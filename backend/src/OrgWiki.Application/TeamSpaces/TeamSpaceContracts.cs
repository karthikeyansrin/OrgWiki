namespace OrgWiki.Application.TeamSpaces;

public sealed record TeamSpaceSummary(Guid Id, string Name, string Slug, string Description, int ArticleCount);
public sealed record TeamSpaceAssignment(Guid Id, string Name, string Slug, string Description);
public sealed record ArticleTeamSpaces(string ArticleKey, IReadOnlyList<TeamSpaceAssignment> TeamSpaces);
public sealed record CreateTeamSpaceRequest(string Name, string Slug, string Description);
public sealed record UpdateArticleTeamSpacesRequest(IReadOnlyList<Guid> TeamSpaceIds);

public interface ITeamSpaceService
{
    Task<IReadOnlyList<TeamSpaceSummary>> GetAllAsync(CancellationToken cancellationToken);
    Task<TeamSpaceSummary> CreateAsync(CreateTeamSpaceRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid teamSpaceId, CancellationToken cancellationToken);
    Task<ArticleTeamSpaces?> GetArticleAssignmentsAsync(string articleKey, CancellationToken cancellationToken);
    Task<ArticleTeamSpaces?> UpdateArticleAssignmentsAsync(string articleKey, UpdateArticleTeamSpacesRequest request, CancellationToken cancellationToken);
}

public sealed record PublicTeamSpaceSummary(string Name, string Slug, string Description, int ArticleCount);
public sealed record PublicTeamSpaceArticleSummary(string Key, string Title, string Summary, DateTime LastUpdatedAtUtc, DateTime PublishedAtUtc);
public sealed record PublicTeamSpace(string Name, string Slug, string Description, IReadOnlyList<PublicTeamSpaceArticleSummary> Articles);
public sealed record PublicRelatedArticle(string Key, string Title, string Summary);
public sealed record PublicTeamSpaceArticle(string Key, string Title, string Summary, string MarkdownContent, IReadOnlyList<string> Tags, string Difficulty, int EstimatedReadingMinutes, DateTime GeneratedAtUtc, DateTime LastUpdatedAtUtc, DateTime PublishedAtUtc, IReadOnlyList<PublicRelatedArticle> RelatedArticles);

public interface IPublicTeamSpaceService
{
    Task<IReadOnlyList<PublicTeamSpaceSummary>> GetAllAsync(CancellationToken cancellationToken);
    Task<PublicTeamSpace?> GetAsync(string slug, CancellationToken cancellationToken);
    Task<PublicTeamSpaceArticle?> GetArticleAsync(string slug, string articleKey, CancellationToken cancellationToken);
}
