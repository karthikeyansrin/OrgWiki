namespace OrgWiki.Application.Ingestion;

public sealed record ExtractedFile(string FileName, string RelativePath, string FullPath, string? ExtractionError = null);
public sealed record ExtractionResult(IReadOnlyList<ExtractedFile> Files, int TotalFiles);

public interface IZipArchiveExtractor
{
    Task<ExtractionResult> ExtractSupportedFilesAsync(
        Stream archive, string extractionDirectory, CancellationToken cancellationToken);
}
