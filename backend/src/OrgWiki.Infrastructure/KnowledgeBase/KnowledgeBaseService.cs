using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrgWiki.Application.Analysis;
using OrgWiki.Application.KnowledgeBase;
using OrgWiki.Domain.Analysis;
using OrgWiki.Infrastructure.Persistence;
using OrgWiki.Application.Authentication;

namespace OrgWiki.Infrastructure.KnowledgeBase;

public sealed class KnowledgeBaseService(OrgWikiDbContext db, ICurrentUser currentUser) : IKnowledgeBaseService
{
    public async Task<KnowledgeBaseHome> GetHomeAsync(CancellationToken cancellationToken)
    {
        var articles = await PublishedArticles(cancellationToken);
        var summaries = await Summaries(articles, cancellationToken);
        return new KnowledgeBaseHome(summaries.Count, summaries.Select(x => x.Domain).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList(), summaries.SelectMany(x => x.Tags).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList(), summaries);
    }

    public async Task<IReadOnlyList<PublishedArticleSummary>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var term = query.Trim(); if (term.Length > 200) throw new InvalidOperationException("Search queries must be 200 characters or fewer."); if (term.Length == 0) return [];
        var articles = await PublishedArticles(cancellationToken);
        var summaries = await Summaries(articles, cancellationToken);
        return summaries.Where(summary => Matches(summary, articles.Single(x => x.Key == summary.Key), term)).ToList();
    }

    public async Task<PublishedArticle?> GetArticleAsync(string key, CancellationToken cancellationToken)
    {
        var normalized = key.Trim(); if (string.IsNullOrWhiteSpace(normalized)) return null;
        var article = await OwnedPublishedArticles().Include(x => x.Citations).SingleOrDefaultAsync(x => x.Key == normalized, cancellationToken);
        if (article is null) return null;
        var generation = await db.KnowledgeGenerations.SingleAsync(x => x.Id == article.GenerationId, cancellationToken);
        var analysis = await db.KnowledgeAnalyses.SingleAsync(x => x.Id == generation.AnalysisId, cancellationToken);
        var discovery = Discovery(analysis);
        var published = await PublishedArticles(cancellationToken);
        var byKey = published.ToDictionary(x => x.Key, StringComparer.Ordinal);
        var related = Strings(article.RelatedArticleKeysJson).Where(byKey.ContainsKey).Select(x => byKey[x]).Select(x => new PublishedRelatedArticle(x.Key, x.Title, x.Summary)).ToList();
        var documentIds = article.Citations.Select(x => x.SourceDocumentId).Distinct().ToList();
        var documents = await db.Documents.Where(x => documentIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        var citations = article.Citations.Where(x => documents.ContainsKey(x.SourceDocumentId)).Select(x => new PublishedCitation(documents[x.SourceDocumentId].FileName, documents[x.SourceDocumentId].OriginalPath, x.EvidenceSnippet)).ToList();
        return new PublishedArticle(article.Key, article.Title, article.Summary, article.MarkdownContent, DomainFor(article, discovery), article.Difficulty, article.EstimatedReadingMinutes, Strings(article.TagsJson), article.GeneratedAtUtc, article.PublishedAtUtc ?? article.GeneratedAtUtc, related, citations);
    }

    async Task<List<GeneratedArticle>> PublishedArticles(CancellationToken cancellationToken) => await OwnedPublishedArticles().OrderBy(x => x.Title).ToListAsync(cancellationToken);
    private IQueryable<GeneratedArticle> OwnedPublishedArticles()
        => db.GeneratedArticles.Where(article => article.Status == GeneratedArticleStatus.Published && db.KnowledgeGenerations.Any(generation => generation.Id == article.GenerationId && db.KnowledgeAnalyses.Any(analysis => analysis.Id == generation.AnalysisId && db.Uploads.Any(upload => upload.Id == analysis.UploadId && upload.UserId == currentUser.Id))));
    async Task<List<PublishedArticleSummary>> Summaries(IReadOnlyList<GeneratedArticle> articles, CancellationToken cancellationToken)
    {
        var generations = await db.KnowledgeGenerations.Where(x => articles.Select(a => a.GenerationId).Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        var analyses = await db.KnowledgeAnalyses.Where(x => generations.Values.Select(g => g.AnalysisId).Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        var publishedKeys = articles.Select(x => x.Key).ToHashSet(StringComparer.Ordinal);
        return articles.Select(article => new PublishedArticleSummary(article.Key, article.Title, article.Summary, DomainFor(article, Discovery(analyses[generations[article.GenerationId].AnalysisId])), Strings(article.TagsJson), article.Difficulty, article.EstimatedReadingMinutes, article.PublishedAtUtc ?? article.GeneratedAtUtc, Strings(article.RelatedArticleKeysJson).Count(publishedKeys.Contains))).ToList();
    }
    static bool Matches(PublishedArticleSummary summary, GeneratedArticle article, string term) => new[] { summary.Title, summary.Summary, article.MarkdownContent, summary.Domain, string.Join(' ', summary.Tags) }.Any(value => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    static KnowledgeDiscoveryResult? Discovery(KnowledgeAnalysis analysis) => string.IsNullOrWhiteSpace(analysis.ResultJson) ? null : JsonSerializer.Deserialize<KnowledgeDiscoveryResult>(analysis.ResultJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    static string DomainFor(GeneratedArticle article, KnowledgeDiscoveryResult? discovery) => discovery?.SuggestedArticles.SingleOrDefault(x => x.Key == article.Key)?.DomainKey ?? "Knowledge";
    static IReadOnlyList<string> Strings(string json) => JsonSerializer.Deserialize<List<string>>(json) ?? [];
}
