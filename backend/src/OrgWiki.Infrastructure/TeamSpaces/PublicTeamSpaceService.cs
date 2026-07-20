using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrgWiki.Application.TeamSpaces;
using OrgWiki.Domain.Analysis;
using OrgWiki.Infrastructure.Persistence;

namespace OrgWiki.Infrastructure.TeamSpaces;

public sealed class PublicTeamSpaceService(OrgWikiDbContext db) : IPublicTeamSpaceService
{
    public async Task<IReadOnlyList<PublicTeamSpaceSummary>> GetAllAsync(CancellationToken cancellationToken)
        => await db.TeamSpaces.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new PublicTeamSpaceSummary(x.Name, x.Slug, x.Description, db.TeamSpaceArticles.Count(assignment => assignment.TeamSpaceId == x.Id && db.GeneratedArticles.Any(article => article.Id == assignment.GeneratedArticleId && article.Status == GeneratedArticleStatus.Published))))
            .ToListAsync(cancellationToken);

    public async Task<PublicTeamSpace?> GetAsync(string slug, CancellationToken cancellationToken)
    {
        var space = await Space(slug, cancellationToken);
        if (space is null) return null;
        var articles = await SpaceArticles(space.Id).OrderBy(x => x.Title).ToListAsync(cancellationToken);
        return new PublicTeamSpace(space.Name, space.Slug, space.Description, articles.Select(Summary).ToList());
    }

    public async Task<PublicTeamSpaceArticle?> GetArticleAsync(string slug, string articleKey, CancellationToken cancellationToken)
    {
        var space = await Space(slug, cancellationToken);
        if (space is null) return null;
        var normalizedKey = articleKey.Trim();
        if (string.IsNullOrWhiteSpace(normalizedKey)) return null;
        var articles = await SpaceArticles(space.Id).OrderBy(x => x.Title).ToListAsync(cancellationToken);
        var article = articles.SingleOrDefault(x => x.Key == normalizedKey);
        if (article is null) return null;
        var byKey = articles.ToDictionary(x => x.Key, StringComparer.Ordinal);
        var related = Strings(article.RelatedArticleKeysJson).Where(byKey.ContainsKey).Select(key => byKey[key]).Select(x => new PublicRelatedArticle(x.Key, x.Title, x.Summary)).ToList();
        return new PublicTeamSpaceArticle(article.Key, article.Title, article.Summary, article.MarkdownContent, Strings(article.TagsJson), article.Difficulty, article.EstimatedReadingMinutes, article.GeneratedAtUtc, UpdatedAt(article), article.PublishedAtUtc ?? article.GeneratedAtUtc, related);
    }

    async Task<OrgWiki.Domain.TeamSpaces.TeamSpace?> Space(string slug, CancellationToken cancellationToken)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? null : await db.TeamSpaces.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == normalized, cancellationToken);
    }

    IQueryable<GeneratedArticle> SpaceArticles(Guid teamSpaceId)
    {
        var articleIds = db.TeamSpaceArticles.Where(x => x.TeamSpaceId == teamSpaceId).Select(x => x.GeneratedArticleId);
        return db.GeneratedArticles.Where(x => articleIds.Contains(x.Id) && x.Status == GeneratedArticleStatus.Published);
    }

    static PublicTeamSpaceArticleSummary Summary(GeneratedArticle article) => new(article.Key, article.Title, article.Summary, UpdatedAt(article), article.PublishedAtUtc ?? article.GeneratedAtUtc);
    static DateTime UpdatedAt(GeneratedArticle article) => article.LastEditedAtUtc ?? article.PublishedAtUtc ?? article.GeneratedAtUtc;
    static IReadOnlyList<string> Strings(string json) => JsonSerializer.Deserialize<List<string>>(json) ?? [];
}
