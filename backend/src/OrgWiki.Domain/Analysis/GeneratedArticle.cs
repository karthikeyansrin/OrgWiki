namespace OrgWiki.Domain.Analysis;

public sealed class GeneratedArticle
{
    private GeneratedArticle() { }
    public GeneratedArticle(Guid generationId, string key, string title, string summary, string markdownContent, string difficulty, int estimatedReadingMinutes, string tagsJson, string relatedArticleKeysJson, double confidence)
    { GenerationId = generationId; Key = key; Title = title; Summary = summary; MarkdownContent = markdownContent; Difficulty = difficulty; EstimatedReadingMinutes = estimatedReadingMinutes; TagsJson = tagsJson; RelatedArticleKeysJson = relatedArticleKeysJson; Confidence = confidence; Status = GeneratedArticleStatus.PendingReview; GeneratedAtUtc = DateTime.UtcNow; }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid GenerationId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public string MarkdownContent { get; private set; } = string.Empty;
    public string Difficulty { get; private set; } = string.Empty;
    public int EstimatedReadingMinutes { get; private set; }
    public string TagsJson { get; private set; } = "[]";
    public string RelatedArticleKeysJson { get; private set; } = "[]";
    public double Confidence { get; private set; }
    public GeneratedArticleStatus Status { get; private set; }
    public DateTime GeneratedAtUtc { get; private set; }
    public string? ReviewedBy { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public string? ReviewNotes { get; private set; }
    public DateTime? LastEditedAtUtc { get; private set; }
    public string? LastEditedBy { get; private set; }
    public List<GeneratedArticleCitation> Citations { get; private set; } = [];
    public void Edit(string title, string summary, string markdownContent, string difficulty, int estimatedReadingMinutes, string tagsJson, string relatedArticleKeysJson, string reviewer)
    { Title = title; Summary = summary; MarkdownContent = markdownContent; Difficulty = difficulty; EstimatedReadingMinutes = estimatedReadingMinutes; TagsJson = tagsJson; RelatedArticleKeysJson = relatedArticleKeysJson; LastEditedAtUtc = DateTime.UtcNow; LastEditedBy = reviewer; }
    public void Approve(string reviewer, string? notes)
    { Status = GeneratedArticleStatus.Approved; ReviewedBy = reviewer; ReviewedAtUtc = DateTime.UtcNow; ReviewNotes = notes; }
    public void Reject(string reviewer, string? notes)
    { Status = GeneratedArticleStatus.Rejected; ReviewedBy = reviewer; ReviewedAtUtc = DateTime.UtcNow; ReviewNotes = notes; }
}
