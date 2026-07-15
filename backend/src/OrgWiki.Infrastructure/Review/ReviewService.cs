using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrgWiki.Application.Analysis;
using OrgWiki.Application.Review;
using OrgWiki.Domain.Analysis;
using OrgWiki.Infrastructure.Persistence;

namespace OrgWiki.Infrastructure.Review;

public sealed class ReviewService(OrgWikiDbContext db) : IReviewService
{
    const string Reviewer = "demo-reviewer";
    public async Task<ReviewDashboard> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var articles = await db.GeneratedArticles.Include(x => x.Citations).OrderByDescending(x => x.GeneratedAtUtc).ToListAsync(cancellationToken);
        var generationIds = articles.Select(x => x.GenerationId).Distinct().ToList();
        var generations = await db.KnowledgeGenerations.Where(x => generationIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        var analysisIds = generations.Values.Select(x => x.AnalysisId).Distinct().ToList();
        var analyses = await db.KnowledgeAnalyses.Where(x => analysisIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        var items = articles.Select(article => ListItem(article, Domain(article, generations, analyses))).ToList();
        return new ReviewDashboard(items.Count(x => x.Status == nameof(GeneratedArticleStatus.PendingReview)), items.Count(x => x.Status == nameof(GeneratedArticleStatus.Approved)), items.Count(x => x.Status == nameof(GeneratedArticleStatus.Rejected)), items.Count(x => x.Status == nameof(GeneratedArticleStatus.Published)), items);
    }

    public async Task<ReviewArticleDetails?> GetArticleAsync(Guid articleId, CancellationToken cancellationToken)
    {
        var article = await db.GeneratedArticles.Include(x => x.Citations).SingleOrDefaultAsync(x => x.Id == articleId, cancellationToken);
        return article is null ? null : await Details(article, cancellationToken);
    }

    public async Task<ReviewArticleDetails?> UpdateAsync(Guid articleId, UpdateReviewArticleRequest request, CancellationToken cancellationToken)
    {
        var article = await db.GeneratedArticles.SingleOrDefaultAsync(x => x.Id == articleId, cancellationToken);
        if (article is null) return null;
        Validate(request, await db.GeneratedArticles.Where(x => x.GenerationId == article.GenerationId && x.Id != article.Id).Select(x => x.Key).ToListAsync(cancellationToken));
        article.Edit(request.Title.Trim(), request.Summary.Trim(), request.MarkdownContent.Trim(), request.Difficulty, request.EstimatedReadingMinutes, JsonSerializer.Serialize(request.Tags.Select(x => x.Trim()).ToList()), JsonSerializer.Serialize(request.RelatedArticleKeys), Reviewer);
        await db.SaveChangesAsync(cancellationToken);
        return await GetArticleAsync(articleId, cancellationToken);
    }

    public async Task<ReviewArticleDetails?> ApproveAsync(Guid articleId, string? notes, CancellationToken cancellationToken)
    {
        var article = await db.GeneratedArticles.SingleOrDefaultAsync(x => x.Id == articleId, cancellationToken); if (article is null) return null;
        if (article.Status == GeneratedArticleStatus.Published) throw new InvalidOperationException("Published articles cannot be changed in the review workflow.");
        article.Approve(Reviewer, notes?.Trim()); await db.SaveChangesAsync(cancellationToken); return await GetArticleAsync(articleId, cancellationToken);
    }

    public async Task<ReviewArticleDetails?> RejectAsync(Guid articleId, string? notes, CancellationToken cancellationToken)
    {
        var article = await db.GeneratedArticles.SingleOrDefaultAsync(x => x.Id == articleId, cancellationToken); if (article is null) return null;
        if (article.Status == GeneratedArticleStatus.Published) throw new InvalidOperationException("Published articles cannot be changed in the review workflow.");
        article.Reject(Reviewer, notes?.Trim()); await db.SaveChangesAsync(cancellationToken); return await GetArticleAsync(articleId, cancellationToken);
    }

    public async Task<ReviewArticleDetails?> PublishAsync(Guid articleId, CancellationToken cancellationToken)
    {
        var article = await db.GeneratedArticles.SingleOrDefaultAsync(x => x.Id == articleId, cancellationToken); if (article is null) return null;
        if (article.Status != GeneratedArticleStatus.Published && await db.GeneratedArticles.AnyAsync(x => x.Id != article.Id && x.Key == article.Key && x.Status == GeneratedArticleStatus.Published, cancellationToken)) throw new InvalidOperationException("A published article with this key already exists.");
        article.Publish(Reviewer); await db.SaveChangesAsync(cancellationToken); return await GetArticleAsync(articleId, cancellationToken);
    }

    async Task<ReviewArticleDetails> Details(GeneratedArticle article, CancellationToken cancellationToken)
    {
        if (article.Citations.Count == 0) await db.Entry(article).Collection(x => x.Citations).LoadAsync(cancellationToken);
        var generation = await db.KnowledgeGenerations.SingleAsync(x => x.Id == article.GenerationId, cancellationToken);
        var analysis = await db.KnowledgeAnalyses.SingleAsync(x => x.Id == generation.AnalysisId, cancellationToken);
        var all = await db.GeneratedArticles.Where(x => x.GenerationId == article.GenerationId).ToListAsync(cancellationToken);
        var docIds = article.Citations.Select(x => x.SourceDocumentId).Distinct().ToList();
        var documents = await db.Documents.Where(x => docIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        var discovery = string.IsNullOrWhiteSpace(analysis.ResultJson) ? null : JsonSerializer.Deserialize<KnowledgeDiscoveryResult>(analysis.ResultJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var proposed = discovery?.SuggestedArticles.SingleOrDefault(x => x.Key == article.Key);
        var topicKeys = proposed?.TopicKeys ?? [];
        var conflicts = discovery?.Conflicts.Count(x => x.TopicKeys.Intersect(topicKeys).Any()) ?? 0;
        var outdated = discovery?.OutdatedCandidates.Count(x => x.TopicKeys.Intersect(topicKeys).Any()) ?? 0;
        var citations = article.Citations.Select(x => new ReviewCitation(x.SourceDocumentId, documents[x.SourceDocumentId].FileName, documents[x.SourceDocumentId].OriginalPath, x.EvidenceSnippet)).ToList();
        return new ReviewArticleDetails(article.Id, article.Key, article.Title, article.Summary, article.MarkdownContent, article.Difficulty, article.EstimatedReadingMinutes, Strings(article.TagsJson), Strings(article.RelatedArticleKeysJson), article.Status.ToString(), article.Confidence, article.ReviewNotes, article.ReviewedAtUtc, article.ReviewedBy, article.LastEditedAtUtc, article.LastEditedBy, citations, all.Where(x => x.Id != article.Id).Select(x => ListItem(x, DomainFor(x, discovery))).ToList(), new KnowledgeQuality(citations.Count, citations.Select(x => x.SourceDocumentId).Distinct().Count(), article.Confidence, Strings(article.RelatedArticleKeysJson).Count, conflicts, outdated));
    }

    static void Validate(UpdateReviewArticleRequest request, IReadOnlyCollection<string> otherKeys)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Summary) || string.IsNullOrWhiteSpace(request.MarkdownContent)) throw new InvalidOperationException("Title, summary, and Markdown content are required.");
        if (request.EstimatedReadingMinutes < 1 || !new[] { "Beginner", "Intermediate", "Advanced" }.Contains(request.Difficulty)) throw new InvalidOperationException("Article metadata is invalid.");
        if (request.Tags.Any(string.IsNullOrWhiteSpace) || request.Tags.Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Tags.Count) throw new InvalidOperationException("Tags must be unique and non-empty.");
        if (request.RelatedArticleKeys.Distinct(StringComparer.Ordinal).Count() != request.RelatedArticleKeys.Count || request.RelatedArticleKeys.Any(x => !otherKeys.Contains(x))) throw new InvalidOperationException("Related article references are invalid.");
    }
    static IReadOnlyList<string> Strings(string value) => JsonSerializer.Deserialize<List<string>>(value) ?? [];
    static ReviewArticleListItem ListItem(GeneratedArticle article, string domain) => new(article.Id, article.Key, article.Title, article.Summary, article.Status.ToString(), article.Confidence, article.Citations.Count, article.LastEditedAtUtc, Strings(article.TagsJson), article.Difficulty, article.EstimatedReadingMinutes, domain);
    static string DomainFor(GeneratedArticle article, KnowledgeDiscoveryResult? discovery) => discovery?.SuggestedArticles.SingleOrDefault(x => x.Key == article.Key)?.DomainKey ?? "Knowledge";
    static string Domain(GeneratedArticle article, IReadOnlyDictionary<Guid, KnowledgeGeneration> generations, IReadOnlyDictionary<Guid, KnowledgeAnalysis> analyses)
    {
        var generation = generations[article.GenerationId]; var analysis = analyses[generation.AnalysisId];
        var discovery = string.IsNullOrWhiteSpace(analysis.ResultJson) ? null : JsonSerializer.Deserialize<KnowledgeDiscoveryResult>(analysis.ResultJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return DomainFor(article, discovery);
    }
}
