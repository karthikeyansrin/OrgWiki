using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrgWiki.Application.Ingestion;
using OrgWiki.Domain.Ingestion;
using OrgWiki.Infrastructure.Persistence;
using OrgWiki.Application.Authentication;

namespace OrgWiki.Infrastructure.Ingestion;

public sealed class IngestionService(
    OrgWikiDbContext db,
    IFileStorageService storage,
    IZipArchiveExtractor extractor,
    IEnumerable<IDocumentParser> parsers,
    IContentNormalizer normalizer,
    IOptions<IngestionOptions> options,
    ICurrentUser currentUser,
    ILogger<IngestionService> logger) : IIngestionService
{
    public async Task<IngestionResult> IngestAsync(string fileName, Stream archive, CancellationToken cancellationToken)
    {
        var upload = new Upload(fileName, string.Empty, currentUser.Id);
        upload.SetUploadedBy(currentUser.FullName);
        db.Uploads.Add(upload);
        await db.SaveChangesAsync(cancellationToken);
        string? extractionDirectory = null;
        try
        {
            var key = await storage.SaveArchiveAsync(fileName, archive, cancellationToken);
            upload = await db.Uploads.SingleAsync(x => x.Id == upload.Id, cancellationToken);
            upload.SetStorageKey(key);
            upload.MarkProcessing();
            await db.SaveChangesAsync(cancellationToken);
            if (archive.CanSeek) archive.Position = 0;

            extractionDirectory = Path.Combine(Path.GetTempPath(), "orgwiki", upload.Id.ToString("N"));
            var extraction = await extractor.ExtractSupportedFilesAsync(archive, extractionDirectory, cancellationToken);
            if (extraction.Files.Count == 0) throw new InvalidDataException("The archive contains no supported documents.");
            var files = extraction.Files;
            var failed = 0;
            foreach (var file in files)
            {
                var parser = parsers.FirstOrDefault(x => x.Supports(Path.GetExtension(file.FileName)));
                if (parser is null) continue;
                var document = new Document(
                    upload.Id,
                    file.FileName,
                    file.RelativePath,
                    GetDocumentType(file.FileName));
                try
                {
                    if (file.ExtractionError is not null) throw new InvalidDataException(file.ExtractionError);
                    var parsed = await parser.ParseAsync(file.FullPath, cancellationToken);
                    var normalized = normalizer.Normalize(parsed.Content);
                    if (normalized.CharacterCount == 0) throw new InvalidDataException("No extractable text was found in the document.");
                    if (normalized.CharacterCount > options.Value.MaxNormalizedCharactersPerDocument)
                        throw new InvalidDataException("Document contains too much text for the current MVP knowledge processing limit.");
                    document.MarkParsed(normalized.Content, normalized.CharacterCount, normalized.WordCount);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failed++;
                    document.MarkFailed(ex.Message);
                    logger.LogWarning(ex, "Document parsing failed for UploadId {UploadId}, file {FileName}", upload.Id, file.RelativePath);
                }
                db.Documents.Add(document);
            }
            var totalCharacterCount = db.ChangeTracker.Entries<Document>()
                .Where(entry => entry.Entity.UploadId == upload.Id && entry.Entity.ProcessingStatus == DocumentProcessingStatus.Parsed)
                .Sum(entry => entry.Entity.CharacterCount);
            upload.Complete(extraction.TotalFiles, files.Count, failed, totalCharacterCount);
            if (totalCharacterCount > options.Value.MaxTotalNormalizedCharacters)
                upload.SetAnalysisEligibility(false, "The extracted knowledge corpus exceeds the 300,000 character MVP analysis limit.");
            await db.SaveChangesAsync(cancellationToken);
            return await GetAsync(upload.Id, cancellationToken) ?? throw new InvalidOperationException("Upload could not be loaded.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            upload.MarkFailed();
            await db.SaveChangesAsync(cancellationToken);
            logger.LogError(ex, "Upload ingestion failed for UploadId {UploadId}", upload.Id);
            throw;
        }
        finally
        {
            if (extractionDirectory is not null) DeleteTemporaryExtractionDirectory(extractionDirectory);
        }
    }

    public async Task<IngestionResult?> GetAsync(Guid uploadId, CancellationToken cancellationToken)
    {
        var upload = await db.Uploads.Include(x => x.Documents).SingleOrDefaultAsync(x => x.Id == uploadId && x.UserId == currentUser.Id, cancellationToken);
        if (upload is null) return null;
        var analysis = await db.KnowledgeAnalyses.AsNoTracking()
            .Where(x => x.UploadId == upload.Id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var generation = analysis is null
            ? null
            : await db.KnowledgeGenerations.AsNoTracking()
                .Where(x => x.AnalysisId == analysis.Id)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        return new IngestionResult(upload.Id, upload.OriginalFileName, upload.Status.ToString(), upload.TotalFiles,
            upload.SupportedFiles, upload.Documents.Count(x => x.ProcessingStatus == DocumentProcessingStatus.Parsed),
            upload.FailedFiles, upload.TotalCharacterCount, upload.IsEligibleForAnalysis, upload.AnalysisEligibilityReason,
            analysis?.Status.ToString(), generation?.Status.ToString(),
            upload.Documents.Select(x => new IngestionDocumentResult(x.Id, x.FileName, x.OriginalPath,
                x.DocumentType.ToString(), x.ProcessingStatus.ToString(), x.CharacterCount, x.WordCount, x.ProcessingError)).ToList());
    }

    public async Task<IReadOnlyList<UploadHistoryItem>> ListAsync(CancellationToken cancellationToken)
    {
        var uploads = await db.Uploads.AsNoTracking().Where(upload => upload.UserId == currentUser.Id)
            .OrderByDescending(upload => upload.CreatedAtUtc).ToListAsync(cancellationToken);
        if (uploads.Count == 0) return [];

        var uploadIds = uploads.Select(upload => upload.Id).ToList();
        var analyses = await db.KnowledgeAnalyses.AsNoTracking().Where(analysis => uploadIds.Contains(analysis.UploadId))
            .OrderByDescending(analysis => analysis.CreatedAtUtc).ToListAsync(cancellationToken);
        var analysisByUpload = analyses.GroupBy(analysis => analysis.UploadId).ToDictionary(group => group.Key, group => group.First());
        var analysisIds = analysisByUpload.Values.Select(analysis => analysis.Id).ToList();
        var generations = analysisIds.Count == 0 ? [] : await db.KnowledgeGenerations.AsNoTracking()
            .Where(generation => analysisIds.Contains(generation.AnalysisId)).OrderByDescending(generation => generation.CreatedAtUtc).ToListAsync(cancellationToken);
        var generationByAnalysis = generations.GroupBy(generation => generation.AnalysisId).ToDictionary(group => group.Key, group => group.First());

        return uploads.Select(upload =>
        {
            analysisByUpload.TryGetValue(upload.Id, out var analysis);
            var generation = analysis is not null && generationByAnalysis.TryGetValue(analysis.Id, out var value) ? value : null;
            return new UploadHistoryItem(upload.Id, upload.OriginalFileName, upload.CreatedAtUtc, upload.Status.ToString(), upload.SupportedFiles, analysis?.Status.ToString(), generation?.Status.ToString());
        }).ToList();
    }

    private static DocumentType GetDocumentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => DocumentType.Pdf,
            ".docx" => DocumentType.Docx,
            ".md" or ".markdown" => DocumentType.Markdown,
            ".txt" => DocumentType.Text,
            _ => throw new InvalidDataException($"Unsupported document type: {fileName}")
        };
    }

    private void DeleteTemporaryExtractionDirectory(string path)
    {
        try
        {
            var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "orgwiki"));
            var directory = Path.GetFullPath(path);
            if (!directory.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                logger.LogWarning("Skipped cleanup for an unexpected extraction directory.");
                return;
            }

            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("Temporary extraction cleanup did not complete for the current upload.");
        }
    }
}
