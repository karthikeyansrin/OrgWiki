using System.Text.RegularExpressions;
using OrgWiki.Application.Analysis;

namespace OrgWiki.Infrastructure.Analysis;

public sealed partial class KnowledgeGenerationValidator : IKnowledgeGenerationValidator
{
    public void Validate(KnowledgeGenerationResult result, KnowledgeGenerationRequest request)
    {
        if (result is null || result.Articles is null) throw new InvalidDataException("Knowledge generation response is missing articles.");
        var expected = request.Articles.Select(x => x.Article.Key).ToHashSet(StringComparer.Ordinal);
        var keys = result.Articles.Select(x => x.Key).ToList();
        if (keys.Count != expected.Count || !keys.All(expected.Contains)) throw new InvalidDataException("Knowledge generation response does not contain exactly the proposed articles.");
        if (keys.Count != keys.Distinct(StringComparer.Ordinal).Count()) throw new InvalidDataException("Knowledge generation response contains duplicate article keys.");
        var titles = new HashSet<string>(StringComparer.Ordinal);
        var sourceMap = request.Articles.SelectMany(x => x.Sources).GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.First().Content);
        foreach (var article in result.Articles)
        {
            Key(article.Key); Text(article.Title); Text(article.Summary); Text(article.MarkdownContent); Text(article.Difficulty);
            if (!new[] { "Beginner", "Intermediate", "Advanced" }.Contains(article.Difficulty, StringComparer.Ordinal)) throw new InvalidDataException("Knowledge generation response contains an invalid difficulty.");
            if (article.EstimatedReadingMinutes < 1 || !double.IsFinite(article.Confidence) || article.Confidence is < 0 or > 1) throw new InvalidDataException("Knowledge generation response contains invalid metadata.");
            if (!titles.Add(article.Title)) throw new InvalidDataException("Knowledge generation response contains duplicate article titles.");
            if (article.RelatedArticleKeys.Any(x => x == article.Key) || article.RelatedArticleKeys.Any(x => !expected.Contains(x)) || article.RelatedArticleKeys.Count != article.RelatedArticleKeys.Distinct(StringComparer.Ordinal).Count()) throw new InvalidDataException("Knowledge generation response contains invalid related article references.");
            if (article.Citations is null || article.Citations.Count == 0) throw new InvalidDataException("Generated article must contain at least one citation.");
            foreach (var citation in article.Citations)
            {
                Text(citation.EvidenceSnippet);
                if (!sourceMap.TryGetValue(citation.SourceDocumentId, out var content) || !content.Contains(citation.EvidenceSnippet, StringComparison.Ordinal)) throw new InvalidDataException("Knowledge generation response contained evidence that could not be verified against the source corpus.");
            }
        }
    }
    static void Text(string? value) { if (string.IsNullOrWhiteSpace(value)) throw new InvalidDataException("Knowledge generation response contains an empty required value."); }
    static void Key(string value) { Text(value); if (!Kebab().IsMatch(value)) throw new InvalidDataException("Knowledge generation response contains an invalid article key."); }
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")] private static partial Regex Kebab();
}
