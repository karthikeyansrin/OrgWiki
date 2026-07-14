using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrgWiki.Application.Analysis;
using OrgWiki.Domain.Analysis;
using OrgWiki.Domain.Ingestion;
using OrgWiki.Infrastructure.Persistence;

namespace OrgWiki.Infrastructure.Analysis;

public sealed class KnowledgeAnalysisService(OrgWikiDbContext db, DeterministicCorpusBuilder corpusBuilder, IEnumerable<IKnowledgeDiscoveryProvider> providers, IKnowledgeDiscoveryValidator validator, IOptions<KnowledgeAnalysisOptions> options, ILogger<KnowledgeAnalysisService> logger) : IKnowledgeAnalysisService
{
    public async Task<KnowledgeAnalysisResult?> StartAsync(Guid uploadId, bool explicitRetry, CancellationToken cancellationToken)
    {
        var upload = await db.Uploads.Include(x => x.Documents).SingleOrDefaultAsync(x => x.Id == uploadId, cancellationToken);
        if (upload is null) return null;
        if (!upload.IsEligibleForAnalysis) throw new InvalidOperationException(upload.AnalysisEligibilityReason ?? "This upload is not eligible for analysis.");
        var latest = await db.KnowledgeAnalyses.Where(x => x.UploadId == uploadId).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        var existing = latest is { IsCurrent: true } ? latest : null;
        if (existing is { Status: KnowledgeAnalysisStatus.Completed } || existing is { Status: KnowledgeAnalysisStatus.Processing }) return ToResult(existing, upload.Documents.Count(x => x.ProcessingStatus == DocumentProcessingStatus.Parsed), upload.TotalCharacterCount);
        if (latest is { Status: KnowledgeAnalysisStatus.Failed } && !explicitRetry) return ToResult(latest, 0, upload.TotalCharacterCount);
        var mode = Enum.TryParse<AiMode>(options.Value.Mode, true, out var parsedMode) ? parsedMode : AiMode.Replay;
        var analysis = new KnowledgeAnalysis(uploadId, mode, options.Value.Model); analysis.Start(); db.KnowledgeAnalyses.Add(analysis);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            var concurrent = await db.KnowledgeAnalyses.Where(x => x.UploadId == uploadId && x.IsCurrent).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
            if (concurrent is not null) return ToResult(concurrent, upload.Documents.Count(x => x.ProcessingStatus == DocumentProcessingStatus.Parsed), upload.TotalCharacterCount);
            throw;
        }
        var watch = Stopwatch.StartNew();
        try
        {
            var request = corpusBuilder.Build(uploadId, upload.Documents);
            if (request.Documents.Count == 0) throw new InvalidOperationException("This upload has no successfully parsed documents.");
            var provider = providers.Single(x => mode == AiMode.Replay ? x is ReplayKnowledgeDiscoveryProvider : x is OpenAiKnowledgeDiscoveryProvider);
            var response = await provider.DiscoverAsync(request, cancellationToken); // exactly one provider invocation
            analysis.RecordUsage(response.Usage?.InputTokens, response.Usage?.OutputTokens, response.Usage?.TotalTokens);
            validator.Validate(response.Result, request.Documents);
            var json = JsonSerializer.Serialize(response.Result);
            watch.Stop();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            analysis.Complete(json, response.Usage?.InputTokens, response.Usage?.OutputTokens, response.Usage?.TotalTokens, watch.ElapsedMilliseconds);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ToResult(analysis, request.Documents.Count, request.Documents.Sum(x => x.CharacterCount));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            watch.Stop(); analysis.Fail(ex is InvalidDataException ? ex.Message : "OrgWiki couldn't complete knowledge analysis for this archive.", watch.ElapsedMilliseconds); await db.SaveChangesAsync(cancellationToken);
            logger.LogError(ex, "Knowledge discovery failed for UploadId {UploadId}, AnalysisId {AnalysisId}, mode {AiMode}", uploadId, analysis.Id, mode);
            throw;
        }
    }

    public async Task<KnowledgeAnalysisResult?> GetAsync(Guid analysisId, CancellationToken cancellationToken)
    {
        var analysis = await db.KnowledgeAnalyses.SingleOrDefaultAsync(x => x.Id == analysisId, cancellationToken); if (analysis is null) return null;
        var upload = await db.Uploads.Include(x => x.Documents).SingleAsync(x => x.Id == analysis.UploadId, cancellationToken); return ToResult(analysis, upload.Documents.Count(x => x.ProcessingStatus == DocumentProcessingStatus.Parsed), upload.TotalCharacterCount);
    }

    static KnowledgeAnalysisResult ToResult(KnowledgeAnalysis analysis, int documents, int characters)
    { KnowledgeDiscoveryResult? discovery = null; if (!string.IsNullOrWhiteSpace(analysis.ResultJson)) discovery = JsonSerializer.Deserialize<KnowledgeDiscoveryResult>(analysis.ResultJson); return new(analysis.Id, analysis.UploadId, analysis.Status.ToString(), analysis.AiMode.ToString(), analysis.Model, documents, characters, analysis.InputTokens, analysis.OutputTokens, analysis.TotalTokens, analysis.DurationMilliseconds, analysis.ErrorMessage, discovery); }
}
