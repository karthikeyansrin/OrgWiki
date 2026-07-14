using System.IO.Compression;
using Microsoft.Extensions.Options;
using OrgWiki.Application.Ingestion;

namespace OrgWiki.Infrastructure.Ingestion;

public sealed class SafeZipArchiveExtractor(IOptions<IngestionOptions> options) : IZipArchiveExtractor
{
    private static readonly HashSet<string> SupportedExtensions = [".pdf", ".docx", ".md", ".markdown", ".txt"];

    public async Task<ExtractionResult> ExtractSupportedFilesAsync(
        Stream archive, string extractionDirectory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(extractionDirectory);
        using var zip = new ZipArchive(archive, ZipArchiveMode.Read, leaveOpen: true);
        var supportedEntries = zip.Entries.Where(entry => !string.IsNullOrEmpty(entry.Name)
            && SupportedExtensions.Contains(Path.GetExtension(entry.Name).ToLowerInvariant())).ToList();
        if (supportedEntries.Count > options.Value.MaxSupportedDocuments)
            throw new InvalidDataException($"This archive contains {supportedEntries.Count} supported documents. The current MVP supports up to {options.Value.MaxSupportedDocuments} documents per knowledge archive.");

        long archiveSize = 0;
        foreach (var entry in zip.Entries)
        {
            archiveSize += entry.Length;
            if (archiveSize > options.Value.MaxTotalExtractedBytes)
                throw new InvalidDataException("The archive exceeds the 25 MB MVP extracted content limit.");
        }

        var root = Path.GetFullPath(extractionDirectory);
        var files = new List<ExtractedFile>();
        foreach (var entry in zip.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(entry.Name)) continue;
            var extension = Path.GetExtension(entry.Name).ToLowerInvariant();
            if (!SupportedExtensions.Contains(extension)) continue;
            var relativePath = entry.FullName.Replace('\\', '/');
            var destination = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!destination.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The archive contains an unsafe path.");

            if (entry.Length > options.Value.MaxIndividualFileBytes)
            {
                files.Add(new ExtractedFile(entry.Name, relativePath, string.Empty,
                    "Document exceeds the 2 MB MVP processing limit."));
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            await using var source = entry.Open();
            await using var output = File.Create(destination);
            await source.CopyToAsync(output, cancellationToken);
            files.Add(new ExtractedFile(entry.Name, relativePath, destination));
        }
        return new ExtractionResult(files, zip.Entries.Count(entry => !string.IsNullOrEmpty(entry.Name)));
    }
}
