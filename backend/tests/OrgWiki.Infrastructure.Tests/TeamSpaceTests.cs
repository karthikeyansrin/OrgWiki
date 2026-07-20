using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OrgWiki.API.Controllers;
using OrgWiki.Application.Authentication;
using OrgWiki.Application.TeamSpaces;
using OrgWiki.Domain.Analysis;
using OrgWiki.Domain.Authentication;
using OrgWiki.Domain.Ingestion;
using OrgWiki.Infrastructure.Persistence;
using OrgWiki.Infrastructure.TeamSpaces;
using Xunit;

namespace OrgWiki.Infrastructure.Tests;

public sealed class TeamSpaceTests
{
    [Fact]
    public async Task Published_article_can_be_assigned_to_multiple_spaces_and_is_publicly_readable()
    {
        await using var db = CreateDb();
        var user = new User("Workspace Owner", "owner@example.com");
        var article = PublishedArticle(user.Id, "authentication");
        db.AddRange(user, article.Upload, article.Analysis, article.Generation, article.Article);
        await db.SaveChangesAsync();

        var admin = new TeamSpaceService(db, new TestCurrentUser(user.Id, user.FullName, user.Email));
        var technical = await admin.CreateAsync(new CreateTeamSpaceRequest("Technical", "technical", "Engineering standards and technical guides."), default);
        var security = await admin.CreateAsync(new CreateTeamSpaceRequest("Security", "security", "Security policies and practices."), default);
        var assignments = await admin.UpdateArticleAssignmentsAsync(article.Article.Key, new UpdateArticleTeamSpacesRequest([technical.Id, security.Id]), default);

        Assert.NotNull(assignments);
        Assert.Equal(2, assignments.TeamSpaces.Count);

        var publicSpaces = new PublicTeamSpaceService(db);
        var technicalSpace = await publicSpaces.GetAsync("technical", default);
        var securitySpace = await publicSpaces.GetAsync("security", default);
        Assert.Single(technicalSpace!.Articles);
        Assert.Single(securitySpace!.Articles);
        Assert.Equal(article.Article.Key, technicalSpace.Articles[0].Key);
        Assert.NotNull(await publicSpaces.GetArticleAsync("technical", article.Article.Key, default));
    }

    [Fact]
    public async Task Assignment_is_limited_to_the_authenticated_owners_published_article()
    {
        await using var db = CreateDb();
        var owner = new User("Owner", "owner@example.com");
        var other = new User("Other", "other@example.com");
        var article = PublishedArticle(owner.Id, "security-guide");
        db.AddRange(owner, other, article.Upload, article.Analysis, article.Generation, article.Article);
        await db.SaveChangesAsync();

        var ownerService = new TeamSpaceService(db, new TestCurrentUser(owner.Id, owner.FullName, owner.Email));
        var space = await ownerService.CreateAsync(new CreateTeamSpaceRequest("Security", "security", "Security guidance."), default);
        var otherService = new TeamSpaceService(db, new TestCurrentUser(other.Id, other.FullName, other.Email));

        Assert.Null(await otherService.GetArticleAssignmentsAsync(article.Article.Key, default));
        Assert.Null(await otherService.UpdateArticleAssignmentsAsync(article.Article.Key, new UpdateArticleTeamSpacesRequest([space.Id]), default));
    }

    [Fact]
    public async Task Public_space_excludes_unpublished_articles()
    {
        await using var db = CreateDb();
        var user = new User("Owner", "owner@example.com");
        var published = PublishedArticle(user.Id, "published-guide");
        var pending = PendingArticle(user.Id, "pending-guide");
        var space = new OrgWiki.Domain.TeamSpaces.TeamSpace("Technical", "technical", "Technical guidance.");
        db.AddRange(user, published.Upload, published.Analysis, published.Generation, published.Article, pending.Upload, pending.Analysis, pending.Generation, pending.Article, space);
        await db.SaveChangesAsync();
        db.TeamSpaceArticles.AddRange(new OrgWiki.Domain.TeamSpaces.TeamSpaceArticle(space.Id, published.Article.Id), new OrgWiki.Domain.TeamSpaces.TeamSpaceArticle(space.Id, pending.Article.Id));
        await db.SaveChangesAsync();

        var result = await new PublicTeamSpaceService(db).GetAsync(space.Slug, default);

        Assert.Single(result!.Articles);
        Assert.Equal(published.Article.Key, result.Articles[0].Key);
        Assert.Null(await new PublicTeamSpaceService(db).GetArticleAsync(space.Slug, pending.Article.Key, default));
    }

    [Fact]
    public async Task Edited_published_article_is_immediately_reflected_in_its_team_space()
    {
        await using var db = CreateDb();
        var user = new User("Owner", "owner@example.com");
        var article = PublishedArticle(user.Id, "authentication");
        db.AddRange(user, article.Upload, article.Analysis, article.Generation, article.Article);
        await db.SaveChangesAsync();

        var owner = new TeamSpaceService(db, new TestCurrentUser(user.Id, user.FullName, user.Email));
        var space = await owner.CreateAsync(new CreateTeamSpaceRequest("Technical", "technical", "Technical guidance."), default);
        await owner.UpdateArticleAssignmentsAsync(article.Article.Key, new UpdateArticleTeamSpacesRequest([space.Id]), default);

        article.Article.Edit("Authentication Guide", "Updated summary", "# Updated guide", "Advanced", 5, "[\"Security\"]", "[]", "Owner");
        article.Article.Publish("Owner");
        await db.SaveChangesAsync();

        var publicArticle = await new PublicTeamSpaceService(db).GetArticleAsync(space.Slug, article.Article.Key, default);
        Assert.NotNull(publicArticle);
        Assert.Equal("Authentication Guide", publicArticle!.Title);
        Assert.Equal("# Updated guide", publicArticle.MarkdownContent);
    }

    [Fact]
    public async Task Deleting_a_team_space_removes_its_article_assignments()
    {
        await using var db = CreateDb();
        var user = new User("Owner", "owner@example.com");
        var article = PublishedArticle(user.Id, "security-guide");
        db.AddRange(user, article.Upload, article.Analysis, article.Generation, article.Article);
        await db.SaveChangesAsync();

        var owner = new TeamSpaceService(db, new TestCurrentUser(user.Id, user.FullName, user.Email));
        var space = await owner.CreateAsync(new CreateTeamSpaceRequest("Security", "security", "Security guidance."), default);
        await owner.UpdateArticleAssignmentsAsync(article.Article.Key, new UpdateArticleTeamSpacesRequest([space.Id]), default);

        Assert.True(await owner.DeleteAsync(space.Id, default));
        Assert.Empty(await owner.GetAllAsync(default));
        Assert.Empty(await db.TeamSpaceArticles.ToListAsync());
        Assert.Empty(await new PublicTeamSpaceService(db).GetAllAsync(default));
    }

    [Fact]
    public void Team_space_admin_controller_requires_authentication_and_public_controller_allows_anonymous_access()
    {
        Assert.NotNull(typeof(TeamSpacesController).GetCustomAttributes(typeof(AuthorizeAttribute), true).SingleOrDefault());
        Assert.NotNull(typeof(PublicTeamSpacesController).GetCustomAttributes(typeof(AllowAnonymousAttribute), true).SingleOrDefault());
    }

    static OrgWikiDbContext CreateDb() => new(new DbContextOptionsBuilder<OrgWikiDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    static (Upload Upload, KnowledgeAnalysis Analysis, KnowledgeGeneration Generation, GeneratedArticle Article) PublishedArticle(Guid userId, string key)
    {
        var graph = PendingArticle(userId, key);
        graph.Article.Approve("reviewer", null);
        graph.Article.Publish("publisher");
        return graph;
    }

    static (Upload Upload, KnowledgeAnalysis Analysis, KnowledgeGeneration Generation, GeneratedArticle Article) PendingArticle(Guid userId, string key)
    {
        var upload = new Upload($"{key}.zip", "archive", userId);
        var analysis = new KnowledgeAnalysis(upload.Id, AiMode.Replay, "replay");
        analysis.Complete("{\"domains\":[],\"topics\":[],\"relationships\":[],\"duplicateGroups\":[],\"conflicts\":[],\"outdatedCandidates\":[],\"suggestedArticles\":[]}", null, null, null, 1);
        var generation = new KnowledgeGeneration(analysis.Id, AiMode.Replay, "replay");
        generation.Complete("{\"articles\":[]}", null, null, null, 1);
        var article = new GeneratedArticle(generation.Id, key, key, "Summary", "# Article", "Beginner", 1, "[]", "[]", .9);
        return (upload, analysis, generation, article);
    }

    sealed record TestCurrentUser(Guid Id, string FullName, string Email) : ICurrentUser;
}
