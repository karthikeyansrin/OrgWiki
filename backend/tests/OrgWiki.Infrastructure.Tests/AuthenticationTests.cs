using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging.Abstractions;
using OrgWiki.API.Authentication;
using OrgWiki.API.Controllers;
using OrgWiki.API.Options;
using OrgWiki.Application.Authentication;
using OrgWiki.Domain.Authentication;
using OrgWiki.Infrastructure.Authentication;
using OrgWiki.Infrastructure.Persistence;
using OrgWiki.Infrastructure.Ingestion;
using OrgWiki.Infrastructure.Analysis;
using OrgWiki.Infrastructure.KnowledgeBase;
using OrgWiki.Infrastructure.Review;
using OrgWiki.Application.Ingestion;
using OrgWiki.Application.Analysis;
using OrgWiki.Domain.Analysis;
using OrgWiki.Domain.Ingestion;
using Xunit;

namespace OrgWiki.Infrastructure.Tests;

public sealed class AuthenticationTests
{
    [Fact]
    public void Password_hasher_does_not_store_or_accept_plaintext()
    {
        var user = new User("Test User", "test@example.com");
        var hasher = new PasswordHasher<User>();
        var hash = hasher.HashPassword(user, "correct-password");

        Assert.NotEqual("correct-password", hash);
        Assert.NotEqual(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, hash, "correct-password"));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, hash, "incorrect-password"));
    }

    [Fact]
    public async Task Register_creates_a_hashed_user_and_duplicate_email_is_rejected()
    {
        await using var db = CreateDb();
        var service = CreateAuthenticationService(db);

        var response = await service.RegisterAsync(new RegisterRequest("Test User", "test@example.com", "correct-password", "correct-password"), default);

        Assert.Equal("Test User", response.User.FullName);
        var stored = await db.Users.SingleAsync();
        Assert.NotEqual("correct-password", stored.PasswordHash);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(new RegisterRequest("Another User", "TEST@example.com", "correct-password", "correct-password"), default));
    }

    [Fact]
    public async Task Registration_validation_and_login_success_or_failure_are_enforced()
    {
        await using var db = CreateDb();
        var service = CreateAuthenticationService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(new RegisterRequest("", "invalid", "short", "different"), default));
        await service.RegisterAsync(new RegisterRequest("Test User", "test@example.com", "correct-password", "correct-password"), default);

        Assert.NotNull(await service.LoginAsync(new LoginRequest("test@example.com", "correct-password"), default));
        Assert.Null(await service.LoginAsync(new LoginRequest("test@example.com", "wrong-password"), default));
    }

    [Fact]
    public async Task Publishing_a_second_article_with_the_same_key_in_the_same_workspace_is_rejected()
    {
        await using var db = CreateDb();
        var user = new User("Test User", "test@example.com");
        var published = CreatePublishedGraph(user.Id, "published.zip", "authentication");
        var candidate = CreateApprovedGraph(user.Id, "candidate.zip", "authentication");
        db.AddRange(user, published.Upload, published.Analysis, published.Generation, published.Article, candidate.Upload, candidate.Analysis, candidate.Generation, candidate.Article);
        await db.SaveChangesAsync();

        var review = new ReviewService(db, new TestCurrentUser(user.Id, user.FullName, user.Email));
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => review.PublishAsync(candidate.Article.Id, default));

        Assert.Equal("An article with the same knowledge identifier is already published in your workspace.", exception.Message);
        Assert.Equal(GeneratedArticleStatus.Approved, candidate.Article.Status);
    }

    [Fact]
    public async Task Republishing_the_same_published_article_remains_idempotent()
    {
        await using var db = CreateDb();
        var user = new User("Test User", "test@example.com");
        var published = CreatePublishedGraph(user.Id, "published.zip", "authentication");
        db.AddRange(user, published.Upload, published.Analysis, published.Generation, published.Article);
        await db.SaveChangesAsync();

        var review = new ReviewService(db, new TestCurrentUser(user.Id, user.FullName, user.Email));
        var result = await review.PublishAsync(published.Article.Id, default);

        Assert.NotNull(result);
        Assert.Equal("Published", result.Status);
    }

    [Fact]
    public async Task Authentication_rejects_oversized_registration_and_login_inputs()
    {
        await using var db = CreateDb();
        var service = CreateAuthenticationService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(new RegisterRequest(new string('a', 257), "test@example.com", "correct-password", "correct-password"), default));
        Assert.Null(await service.LoginAsync(new LoginRequest(new string('a', 321), "correct-password"), default));
    }

    [Fact]
    public void Jwt_contains_expected_claims_and_validates()
    {
        var signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var service = new JwtAccessTokenService(Options.Create(new JwtOptions { Issuer = "OrgWiki", Audience = "OrgWiki", SigningKey = signingKey, ExpirationMinutes = 30 }));
        var user = new AuthenticatedUser(Guid.NewGuid(), "Test User", "test@example.com");

        var issued = service.Create(user);
        var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(issued.AccessToken, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "OrgWiki",
            ValidateAudience = true,
            ValidAudience = "OrgWiki",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        }, out _);

        Assert.Equal(user.Id.ToString(), principal.FindFirstValue(JwtRegisteredClaimNames.Sub));
        Assert.Equal(user.Email, principal.FindFirstValue(JwtRegisteredClaimNames.Email));
        Assert.Equal(user.FullName, principal.FindFirstValue(JwtRegisteredClaimNames.Name));
        Assert.Equal(user.Id.ToString(), principal.FindFirstValue("userId"));
    }

    [Theory]
    [InlineData(typeof(UploadsController))]
    [InlineData(typeof(AnalysesController))]
    [InlineData(typeof(GenerationsController))]
    [InlineData(typeof(ReviewController))]
    public void Workflow_controllers_require_authorization(Type controller)
        => Assert.NotNull(controller.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());

    [Fact]
    public void Sensitive_endpoints_have_explicit_rate_limit_policies()
    {
        Assert.Contains(typeof(UploadsController).GetMethod(nameof(UploadsController.Upload))!.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true), attribute => ((EnableRateLimitingAttribute)attribute).PolicyName == "upload");
        Assert.Contains(typeof(AnalysesController).GetMethod(nameof(AnalysesController.Start))!.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true), attribute => ((EnableRateLimitingAttribute)attribute).PolicyName == "ai");
        Assert.Contains(typeof(GenerationsController).GetMethod(nameof(GenerationsController.Start))!.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true), attribute => ((EnableRateLimitingAttribute)attribute).PolicyName == "ai");
    }

    [Fact]
    public async Task User_owned_workspace_services_do_not_expose_another_users_data()
    {
        await using var db = CreateDb();
        var userA = new User("User A", "a@example.com");
        var userB = new User("User B", "b@example.com");
        var graphA = CreatePublishedGraph(userA.Id, "alpha-upload.zip", "alpha-article");
        var graphB = CreatePublishedGraph(userB.Id, "bravo-upload.zip", "bravo-article");
        db.AddRange(userA, userB, graphA.Upload, graphB.Upload, graphA.Analysis, graphB.Analysis, graphA.Generation, graphB.Generation, graphA.Article, graphB.Article);
        await db.SaveChangesAsync();

        var currentUserB = new TestCurrentUser(userB.Id, userB.FullName, userB.Email);
        var ingestion = new IngestionService(db, null!, null!, [], null!, Options.Create(new IngestionOptions()), currentUserB, NullLogger<IngestionService>.Instance);
        var analysis = new KnowledgeAnalysisService(db, currentUserB, new DeterministicCorpusBuilder(), [], new KnowledgeDiscoveryValidator(), Options.Create(new KnowledgeAnalysisOptions()), NullLogger<KnowledgeAnalysisService>.Instance);
        var generation = new KnowledgeGenerationService(db, currentUserB, new KnowledgeGenerationContextBuilder(), [], new KnowledgeGenerationValidator(), Options.Create(new KnowledgeAnalysisOptions()), NullLogger<KnowledgeGenerationService>.Instance);
        var review = new ReviewService(db, currentUserB);
        var knowledge = new KnowledgeBaseService(db, currentUserB);

        Assert.Null(await ingestion.GetAsync(graphA.Upload.Id, default));
        Assert.Null(await analysis.GetAsync(graphA.Analysis.Id, default));
        Assert.Null(await generation.GetAsync(graphA.Generation.Id, default));
        Assert.Null(await review.GetArticleAsync(graphA.Article.Id, default));
        Assert.Null(await knowledge.GetArticleAsync(graphA.Article.Key, default));
        Assert.Empty(await knowledge.SearchAsync("alpha", default));

        var dashboard = await review.GetDashboardAsync(default);
        Assert.Single(dashboard.Articles);
        Assert.Equal(graphB.Article.Id, dashboard.Articles[0].Id);
        var home = await knowledge.GetHomeAsync(default);
        Assert.Single(home.Articles);
        Assert.Equal(graphB.Article.Key, home.Articles[0].Key);
    }

    private static OrgWikiDbContext CreateDb()
        => new(new DbContextOptionsBuilder<OrgWikiDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static AuthenticationService CreateAuthenticationService(OrgWikiDbContext db)
        => new(db, new PasswordHasher<User>(), new TestAccessTokenService());

    private static (Upload Upload, KnowledgeAnalysis Analysis, KnowledgeGeneration Generation, GeneratedArticle Article) CreatePublishedGraph(Guid userId, string uploadName, string articleKey)
    {
        const string discovery = "{\"domains\":[],\"topics\":[],\"relationships\":[],\"duplicateGroups\":[],\"conflicts\":[],\"outdatedCandidates\":[],\"suggestedArticles\":[]}";
        var upload = new Upload(uploadName, "archive", userId);
        var analysis = new KnowledgeAnalysis(upload.Id, AiMode.Replay, "replay");
        analysis.Complete(discovery, null, null, null, 1);
        var generation = new KnowledgeGeneration(analysis.Id, AiMode.Replay, "replay");
        generation.Complete("{\"articles\":[]}", null, null, null, 1);
        var article = new GeneratedArticle(generation.Id, articleKey, articleKey, "summary", "# Article", "Beginner", 1, "[]", "[]", .9);
        article.Approve("reviewer", null);
        article.Publish("publisher");
        return (upload, analysis, generation, article);
    }

    private static (Upload Upload, KnowledgeAnalysis Analysis, KnowledgeGeneration Generation, GeneratedArticle Article) CreateApprovedGraph(Guid userId, string uploadName, string articleKey)
    {
        const string discovery = "{\"domains\":[],\"topics\":[],\"relationships\":[],\"duplicateGroups\":[],\"conflicts\":[],\"outdatedCandidates\":[],\"suggestedArticles\":[]}";
        var upload = new Upload(uploadName, "archive", userId);
        var analysis = new KnowledgeAnalysis(upload.Id, AiMode.Replay, "replay");
        analysis.Complete(discovery, null, null, null, 1);
        var generation = new KnowledgeGeneration(analysis.Id, AiMode.Replay, "replay");
        generation.Complete("{\"articles\":[]}", null, null, null, 1);
        var article = new GeneratedArticle(generation.Id, articleKey, articleKey, "summary", "# Article", "Beginner", 1, "[]", "[]", .9);
        article.Approve("reviewer", null);
        return (upload, analysis, generation, article);
    }

    private sealed class TestAccessTokenService : IAccessTokenService
    {
        public IssuedAccessToken Create(AuthenticatedUser user) => new($"test-token-{user.Id}", DateTime.UtcNow.AddHours(1));
    }

    private sealed record TestCurrentUser(Guid Id, string FullName, string Email) : ICurrentUser;
}
