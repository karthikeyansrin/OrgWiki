using System.Text.Json;
using Microsoft.Extensions.Options;
using OrgWiki.Application.Analysis;

namespace OrgWiki.Infrastructure.Analysis;

public sealed class ReplayKnowledgeGenerationProvider(IOptions<KnowledgeAnalysisOptions> options) : IKnowledgeGenerationProvider
{
    public Task<KnowledgeGenerationResponse> GenerateAsync(KnowledgeGenerationRequest request, CancellationToken cancellationToken)
    {
        var path = Path.Combine(AppContext.BaseDirectory, options.Value.ReplayGenerationFixturePath);
        KnowledgeGenerationResult? fixture = File.Exists(path) ? JsonSerializer.Deserialize<KnowledgeGenerationResult>(File.ReadAllText(path), new JsonSerializerOptions(JsonSerializerDefaults.Web)) : null;
        var articles = request.Articles.Select((context, index) =>
        {
            var source = context.Sources.First();
            var template = fixture?.Articles.ElementAtOrDefault(index);
            return new GeneratedArticleDraft(context.Article.Key, template?.Title ?? context.Article.Title, template?.Summary ?? context.Article.Summary, template?.MarkdownContent ?? $"# {context.Article.Title}\n\n{context.Article.Summary}", template?.Difficulty ?? "Intermediate", template?.EstimatedReadingMinutes > 0 ? template.EstimatedReadingMinutes : 5, template?.Tags ?? context.Topics.Select(x => x.Name).Take(5).ToArray(), template?.RelatedArticleKeys ?? [], .9, [new GeneratedCitation(source.Id, source.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries).First())]);
        }).ToList();
        return Task.FromResult(new KnowledgeGenerationResponse(new KnowledgeGenerationResult(articles), null));
    }
}
