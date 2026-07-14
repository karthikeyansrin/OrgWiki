namespace OrgWiki.Application.Ingestion;

public sealed record IngestionDocumentResult(Guid Id, string FileName, string OriginalPath, string DocumentType,
    string ProcessingStatus, int CharacterCount, int WordCount, string? ProcessingError);

public sealed record IngestionResult(Guid UploadId, string FileName, string Status, int TotalFiles,
    int SupportedFiles, int ParsedFiles, int FailedFiles, int TotalCharacterCount,
    bool IsEligibleForAnalysis, string? AnalysisEligibilityReason,
    IReadOnlyList<IngestionDocumentResult> Documents);

public interface IIngestionService
{
    Task<IngestionResult> IngestAsync(string fileName, Stream archive, CancellationToken cancellationToken);
    Task<IngestionResult?> GetAsync(Guid uploadId, CancellationToken cancellationToken);
}
