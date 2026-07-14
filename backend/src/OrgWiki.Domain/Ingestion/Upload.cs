namespace OrgWiki.Domain.Ingestion;

public sealed class Upload
{
    private Upload() { }

    public Upload(string originalFileName, string storageKey)
    {
        OriginalFileName = originalFileName;
        StorageKey = storageKey;
        Status = UploadStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string OriginalFileName { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public UploadStatus Status { get; private set; }
    public int TotalFiles { get; private set; }
    public int SupportedFiles { get; private set; }
    public int FailedFiles { get; private set; }
    public int TotalCharacterCount { get; private set; }
    public bool IsEligibleForAnalysis { get; private set; } = true;
    public string? AnalysisEligibilityReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public ICollection<Document> Documents { get; private set; } = new List<Document>();

    public void MarkProcessing() => Status = UploadStatus.Processing;
    public void SetStorageKey(string storageKey) => StorageKey = storageKey;

    public void Complete(int totalFiles, int supportedFiles, int failedFiles, int totalCharacterCount)
    {
        TotalFiles = totalFiles;
        SupportedFiles = supportedFiles;
        FailedFiles = failedFiles;
        TotalCharacterCount = totalCharacterCount;
        Status = failedFiles > 0 ? UploadStatus.CompletedWithErrors : UploadStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void SetAnalysisEligibility(bool isEligible, string? reason)
    {
        IsEligibleForAnalysis = isEligible;
        AnalysisEligibilityReason = reason;
    }

    public void MarkFailed() { Status = UploadStatus.Failed; CompletedAtUtc = DateTime.UtcNow; }
}
