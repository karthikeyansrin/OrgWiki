namespace OrgWiki.Domain.Ingestion;

public sealed class Document
{
    private Document() { }

    public Document(Guid uploadId, string fileName, string originalPath, DocumentType documentType)
    {
        UploadId = uploadId;
        FileName = fileName;
        OriginalPath = originalPath;
        FileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        DocumentType = documentType;
        ProcessingStatus = DocumentProcessingStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UploadId { get; private set; }
    public Upload Upload { get; private set; } = null!;
    public string FileName { get; private set; } = string.Empty;
    public string OriginalPath { get; private set; } = string.Empty;
    public string FileExtension { get; private set; } = string.Empty;
    public DocumentType DocumentType { get; private set; }
    public string? Content { get; private set; }
    public int CharacterCount { get; private set; }
    public int WordCount { get; private set; }
    public DocumentProcessingStatus ProcessingStatus { get; private set; }
    public string? ProcessingError { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public void MarkParsed(string content, int characterCount, int wordCount)
    {
        Content = content;
        CharacterCount = characterCount;
        WordCount = wordCount;
        ProcessingStatus = DocumentProcessingStatus.Parsed;
        ProcessingError = null;
    }

    public void MarkFailed(string error)
    {
        ProcessingStatus = DocumentProcessingStatus.Failed;
        ProcessingError = error;
    }
}
