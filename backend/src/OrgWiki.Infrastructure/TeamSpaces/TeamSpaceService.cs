using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OrgWiki.Application.Authentication;
using OrgWiki.Application.TeamSpaces;
using OrgWiki.Domain.Analysis;
using OrgWiki.Domain.TeamSpaces;
using OrgWiki.Infrastructure.Persistence;

namespace OrgWiki.Infrastructure.TeamSpaces;

public sealed partial class TeamSpaceService(OrgWikiDbContext db, ICurrentUser currentUser) : ITeamSpaceService
{
    public async Task<IReadOnlyList<TeamSpaceSummary>> GetAllAsync(CancellationToken cancellationToken)
        => await db.TeamSpaces.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new TeamSpaceSummary(x.Id, x.Name, x.Slug, x.Description, x.ArticleAssignments.Count))
            .ToListAsync(cancellationToken);

    public async Task<TeamSpaceSummary> CreateAsync(CreateTeamSpaceRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        var slug = request.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        var description = request.Description?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description)) throw new InvalidOperationException("Team Space name and description are required.");
        if (name.Length > 128 || slug.Length > 128 || description.Length > 1024 || !SlugPattern().IsMatch(slug)) throw new InvalidOperationException("Team Space details are invalid. Slugs must use lowercase letters, numbers, and hyphens.");
        if (await db.TeamSpaces.AnyAsync(x => x.Slug == slug, cancellationToken)) throw new InvalidOperationException("A Team Space with this slug already exists.");

        var space = new TeamSpace(name, slug, description);
        db.TeamSpaces.Add(space);
        await db.SaveChangesAsync(cancellationToken);
        return new TeamSpaceSummary(space.Id, space.Name, space.Slug, space.Description, 0);
    }

    public async Task<bool> DeleteAsync(Guid teamSpaceId, CancellationToken cancellationToken)
    {
        var space = await db.TeamSpaces.SingleOrDefaultAsync(x => x.Id == teamSpaceId, cancellationToken);
        if (space is null) return false;

        var assignments = await db.TeamSpaceArticles.Where(x => x.TeamSpaceId == teamSpaceId).ToListAsync(cancellationToken);
        db.TeamSpaceArticles.RemoveRange(assignments);
        db.TeamSpaces.Remove(space);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ArticleTeamSpaces?> GetArticleAssignmentsAsync(string articleKey, CancellationToken cancellationToken)
    {
        var article = await OwnedPublishedArticle(articleKey, cancellationToken);
        return article is null ? null : new ArticleTeamSpaces(article.Key, await Assignments(article.Id, cancellationToken));
    }

    public async Task<ArticleTeamSpaces?> UpdateArticleAssignmentsAsync(string articleKey, UpdateArticleTeamSpacesRequest request, CancellationToken cancellationToken)
    {
        var article = await OwnedPublishedArticle(articleKey, cancellationToken);
        if (article is null) return null;
        var ids = request.TeamSpaceIds?.Distinct().ToList() ?? [];
        var spaces = ids.Count == 0 ? [] : await db.TeamSpaces.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
        if (spaces.Count != ids.Count) throw new InvalidOperationException("One or more selected Team Spaces do not exist.");

        var duplicateKey = ids.Count > 0 && await db.TeamSpaceArticles
            .Where(x => ids.Contains(x.TeamSpaceId) && x.GeneratedArticleId != article.Id)
            .Join(db.GeneratedArticles, assignment => assignment.GeneratedArticleId, candidate => candidate.Id, (_, candidate) => candidate)
            .AnyAsync(candidate => candidate.Status == GeneratedArticleStatus.Published && candidate.Key == article.Key, cancellationToken);
        if (duplicateKey) throw new InvalidOperationException("A published article with this key already belongs to one of the selected Team Spaces.");

        var current = await db.TeamSpaceArticles.Where(x => x.GeneratedArticleId == article.Id).ToListAsync(cancellationToken);
        db.TeamSpaceArticles.RemoveRange(current);
        db.TeamSpaceArticles.AddRange(ids.Select(id => new TeamSpaceArticle(id, article.Id)));
        await db.SaveChangesAsync(cancellationToken);
        return new ArticleTeamSpaces(article.Key, await Assignments(article.Id, cancellationToken));
    }

    async Task<GeneratedArticle?> OwnedPublishedArticle(string key, CancellationToken cancellationToken)
    {
        var normalized = key.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        return await OwnedPublishedArticles().SingleOrDefaultAsync(x => x.Key == normalized, cancellationToken);
    }

    async Task<IReadOnlyList<TeamSpaceAssignment>> Assignments(Guid articleId, CancellationToken cancellationToken)
        => await db.TeamSpaceArticles.AsNoTracking().Where(x => x.GeneratedArticleId == articleId).OrderBy(x => x.TeamSpace.Name)
            .Select(x => new TeamSpaceAssignment(x.TeamSpace.Id, x.TeamSpace.Name, x.TeamSpace.Slug, x.TeamSpace.Description)).ToListAsync(cancellationToken);

    IQueryable<GeneratedArticle> OwnedPublishedArticles()
        => db.GeneratedArticles.Where(article => article.Status == GeneratedArticleStatus.Published && db.KnowledgeGenerations.Any(generation => generation.Id == article.GenerationId && db.KnowledgeAnalyses.Any(analysis => analysis.Id == generation.AnalysisId && db.Uploads.Any(upload => upload.Id == analysis.UploadId && upload.UserId == currentUser.Id))));

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
