namespace OrgWiki.Domain.Ingestion;

public enum UploadStatus
{
    Pending,
    Processing,
    Completed,
    CompletedWithErrors,
    Failed
}
