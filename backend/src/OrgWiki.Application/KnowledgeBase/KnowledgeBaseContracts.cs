namespace OrgWiki.Application.KnowledgeBase;

public sealed record PublishedArticleSummary(string Key, string Title, string Summary, string Domain, IReadOnlyList<string> Tags, string Difficulty, int EstimatedReadingMinutes, DateTime PublishedAtUtc, int RelatedArticleCount);
public sealed record KnowledgeBaseHome(int PublishedArticleCount, IReadOnlyList<string> Domains, IReadOnlyList<string> Tags, IReadOnlyList<PublishedArticleSummary> Articles);
public sealed record PublishedCitation(string SourceFileName, string SourcePath, string EvidenceSnippet);
public sealed record PublishedRelatedArticle(string Key, string Title, string Summary);
public sealed record PublishedArticle(string Key, string Title, string Summary, string MarkdownContent, string Domain, string Difficulty, int EstimatedReadingMinutes, IReadOnlyList<string> Tags, DateTime PublishedAtUtc, IReadOnlyList<PublishedRelatedArticle> RelatedArticles, IReadOnlyList<PublishedCitation> Citations);
public interface IKnowledgeBaseService
{ Task<KnowledgeBaseHome> GetHomeAsync(CancellationToken cancellationToken); Task<PublishedArticle?> GetArticleAsync(string key, CancellationToken cancellationToken); Task<IReadOnlyList<PublishedArticleSummary>> SearchAsync(string query, CancellationToken cancellationToken); }
