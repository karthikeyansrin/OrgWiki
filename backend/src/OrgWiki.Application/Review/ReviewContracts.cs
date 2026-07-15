using OrgWiki.Domain.Analysis;

namespace OrgWiki.Application.Review;

public sealed record ReviewDashboard(int PendingReview, int Approved, int Rejected, int Published, IReadOnlyList<ReviewArticleListItem> Articles);
public sealed record ReviewArticleListItem(Guid Id, string Key, string Title, string Summary, string Status, double Confidence, int CitationCount, DateTime? LastEditedAtUtc, IReadOnlyList<string> Tags, string Difficulty, int EstimatedReadingMinutes, string Domain);
public sealed record ReviewCitation(Guid SourceDocumentId, string FileName, string OriginalPath, string EvidenceSnippet);
public sealed record KnowledgeQuality(int CitationCount, int SourceDocumentCount, double Confidence, int RelatedArticleCount, int LinkedConflictCount, int PotentiallyOutdatedCount);
public sealed record ReviewArticleDetails(Guid Id, string Key, string Title, string Summary, string MarkdownContent, string Difficulty, int EstimatedReadingMinutes, IReadOnlyList<string> Tags, IReadOnlyList<string> RelatedArticleKeys, string Status, double Confidence, string? ReviewNotes, DateTime? ReviewedAtUtc, string? ReviewedBy, DateTime? LastEditedAtUtc, string? LastEditedBy, IReadOnlyList<ReviewCitation> Citations, IReadOnlyList<ReviewArticleListItem> AvailableRelatedArticles, KnowledgeQuality Quality);
public sealed record UpdateReviewArticleRequest(string Title, string Summary, string MarkdownContent, string Difficulty, int EstimatedReadingMinutes, IReadOnlyList<string> Tags, IReadOnlyList<string> RelatedArticleKeys);
public interface IReviewService
{ Task<ReviewDashboard> GetDashboardAsync(CancellationToken cancellationToken); Task<ReviewArticleDetails?> GetArticleAsync(Guid articleId, CancellationToken cancellationToken); Task<ReviewArticleDetails?> UpdateAsync(Guid articleId, UpdateReviewArticleRequest request, CancellationToken cancellationToken); Task<ReviewArticleDetails?> ApproveAsync(Guid articleId, string? notes, CancellationToken cancellationToken); Task<ReviewArticleDetails?> RejectAsync(Guid articleId, string? notes, CancellationToken cancellationToken); Task<ReviewArticleDetails?> PublishAsync(Guid articleId, CancellationToken cancellationToken); }
