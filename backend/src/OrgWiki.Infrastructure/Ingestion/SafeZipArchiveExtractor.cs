using System.IO.Compression;
using System.Buffers;
using Microsoft.Extensions.Options;
using OrgWiki.Application.Ingestion;

namespace OrgWiki.Infrastructure.Ingestion;

public sealed class SafeZipArchiveExtractor(IOptions<IngestionOptions> options) : IZipArchiveExtractor
{
    private static readonly HashSet<string> SupportedExtensions = [".pdf", ".docx", ".md", ".markdown", ".txt"];

    public async Task<ExtractionResult> ExtractSupportedFilesAsync(
        Stream archive, string extractionDirectory, CancellationToken cancellationToken)
    {
        using var zip = new ZipArchive(archive, ZipArchiveMode.Read, leaveOpen: true);
        if (zip.Entries.Count > options.Value.MaxArchiveEntries)
            throw new InvalidDataException($"This archive contains too many entries. The current MVP supports up to {options.Value.MaxArchiveEntries} archive entries.");

        var entries = zip.Entries.Where(entry => !string.IsNullOrEmpty(entry.FullName)).ToList();
        foreach (var entry in entries) GetSafeRelativePath(entry.FullName);

        var actualSizes = await MeasureExtractedSizesAsync(entries, cancellationToken);
        var supportedEntries = entries.Where(entry => !string.IsNullOrEmpty(entry.Name)
            && SupportedExtensions.Contains(Path.GetExtension(entry.Name).ToLowerInvariant())).ToList();
        if (supportedEntries.Count > options.Value.MaxSupportedDocuments)
            throw new InvalidDataException($"This archive contains {supportedEntries.Count} supported documents. The current MVP supports up to {options.Value.MaxSupportedDocuments} documents per knowledge archive.");

        Directory.CreateDirectory(extractionDirectory);
        var root = Path.GetFullPath(extractionDirectory);
        var files = new List<ExtractedFile>();
        foreach (var entry in supportedEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = GetSafeRelativePath(entry.FullName);
            var destination = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!destination.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidDataException("The archive contains an unsafe path.");

            if (actualSizes[entry] > options.Value.MaxIndividualFileBytes)
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
        return new ExtractionResult(files, entries.Count(entry => !string.IsNullOrEmpty(entry.Name)));
    }

    private async Task<IReadOnlyDictionary<ZipArchiveEntry, long>> MeasureExtractedSizesAsync(
        IReadOnlyCollection<ZipArchiveEntry> entries,
        CancellationToken cancellationToken)
    {
        var sizes = new Dictionary<ZipArchiveEntry, long>();
        var buffer = ArrayPool<byte>.Shared.Rent(81_920);
        long total = 0;
        try
        {
            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                long entrySize = 0;
                await using var source = entry.Open();
                int read;
                while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                {
                    if (total > options.Value.MaxTotalExtractedBytes - read)
                        throw new InvalidDataException("The archive exceeds the 25 MB MVP extracted content limit.");
                    total += read;
                    entrySize += read;
                }
                sizes[entry] = entrySize;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return sizes;
    }

    private static string GetSafeRelativePath(string entryPath)
    {
        var relativePath = entryPath.Replace('\\', '/');
        var driveQualified = relativePath.Length >= 3
            && char.IsAsciiLetter(relativePath[0])
            && relativePath[1] == ':'
            && relativePath[2] == '/';
        var containsTraversal = relativePath.Split('/', StringSplitOptions.None)
            .Any(segment => string.Equals(segment, "..", StringComparison.Ordinal));

        if (string.IsNullOrWhiteSpace(relativePath)
            || relativePath.IndexOf('\0') >= 0
            || relativePath.StartsWith("/", StringComparison.Ordinal)
            || driveQualified
            || Path.IsPathRooted(relativePath)
            || containsTraversal)
            throw new InvalidDataException("The archive contains an unsafe path.");

        return relativePath;
    }
}
