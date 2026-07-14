using System.Text.RegularExpressions;
using OrgWiki.Application.Analysis;

namespace OrgWiki.Infrastructure.Analysis;

public sealed partial class KnowledgeDiscoveryValidator : IKnowledgeDiscoveryValidator
{
    private static readonly HashSet<string> RelationshipTypes = ["DependsOn", "RelatedTo", "Uses", "PartOf", "Supersedes"];

    public void Validate(KnowledgeDiscoveryResult result, IReadOnlyList<CorpusDocument> documents)
    {
        if (result is null || result.Domains is null || result.Topics is null || result.Relationships is null || result.DuplicateGroups is null || result.Conflicts is null || result.OutdatedCandidates is null || result.SuggestedArticles is null)
            throw new InvalidDataException("Knowledge discovery response is missing a required collection.");
        var ids = documents.Select(x => x.Id).ToHashSet();
        var domains = result.Domains.Select(x => x.Key).ToList(); var topics = result.Topics.Select(x => x.Key).ToList(); var articles = result.SuggestedArticles.Select(x => x.Key).ToList();
        Unique(domains, "domain keys"); Unique(topics, "topic keys"); Unique(articles, "suggested article keys");
        foreach (var domain in result.Domains) { Key(domain.Key, "domain key"); Text(domain.Name, "domain name"); Text(domain.Description, "domain description"); Confidence(domain.Confidence); }
        foreach (var topic in result.Topics) { Key(topic.Key, "topic key"); Text(topic.Name, "topic name"); Text(topic.Description, "topic description"); Confidence(topic.Confidence); Reference(topic.DomainKey, domains, "domain"); Sources(topic.SourceDocumentIds, ids, 1); }
        foreach (var relationship in result.Relationships) { Text(relationship.Explanation, "relationship explanation"); Confidence(relationship.Confidence); Reference(relationship.SourceTopicKey, topics, "source topic"); Reference(relationship.TargetTopicKey, topics, "target topic"); if (!RelationshipTypes.Contains(relationship.Type)) throw new InvalidDataException("Knowledge discovery response contains an invalid relationship."); }
        foreach (var group in result.DuplicateGroups) { Text(group.Title, "duplicate title"); Text(group.Explanation, "duplicate explanation"); Confidence(group.Confidence); References(group.TopicKeys, topics, 1, "duplicate topic"); Sources(group.SourceDocumentIds, ids, 2); }
        foreach (var conflict in result.Conflicts)
        {
            Text(conflict.Title, "conflict title"); Text(conflict.Description, "conflict description"); Text(conflict.ClaimA, "claim A"); Text(conflict.ClaimB, "claim B"); Text(conflict.EvidenceSnippetA, "evidence A"); Text(conflict.EvidenceSnippetB, "evidence B"); Text(conflict.Recommendation, "recommendation"); Text(conflict.RecommendationReasoning, "recommendation reasoning"); Confidence(conflict.Confidence); References(conflict.TopicKeys, topics, 1, "conflict topic"); Sources([conflict.SourceDocumentIdA, conflict.SourceDocumentIdB], ids, 2);
            var sourceA = documents.Single(x => x.Id == conflict.SourceDocumentIdA).Content; var sourceB = documents.Single(x => x.Id == conflict.SourceDocumentIdB).Content;
            if (!sourceA.Contains(conflict.EvidenceSnippetA, StringComparison.Ordinal) || !sourceB.Contains(conflict.EvidenceSnippetB, StringComparison.Ordinal)) throw new InvalidDataException("Knowledge discovery response contained evidence that could not be verified against the source corpus.");
        }
        foreach (var outdated in result.OutdatedCandidates) { Text(outdated.Description, "outdated description"); Text(outdated.Reason, "outdated reason"); Confidence(outdated.Confidence); References(outdated.TopicKeys, topics, 1, "outdated topic"); Sources(outdated.SourceDocumentIds, ids, 1); }
        foreach (var article in result.SuggestedArticles) { Key(article.Key, "article key"); Text(article.Title, "article title"); Text(article.Summary, "article summary"); Text(article.Reason, "article reason"); Confidence(article.Confidence); Reference(article.DomainKey, domains, "article domain"); References(article.TopicKeys, topics, 1, "article topic"); Sources(article.SourceDocumentIds, ids, 1); }
    }

    static void Text(string? value, string label) { if (string.IsNullOrWhiteSpace(value)) throw new InvalidDataException($"Knowledge discovery response contains an empty {label}."); }
    static void Key(string? value, string label) { Text(value, label); if (!Kebab().IsMatch(value!)) throw new InvalidDataException($"Knowledge discovery response contains an invalid {label}."); }
    static void Confidence(double value) { if (!double.IsFinite(value) || value is < 0 or > 1) throw new InvalidDataException("Knowledge discovery response contains an invalid confidence value."); }
    static void Unique<T>(IReadOnlyList<T> values, string label) { if (values.Count != values.Distinct().Count()) throw new InvalidDataException($"Knowledge discovery response contains duplicate {label}."); }
    static void Reference(string value, IReadOnlyCollection<string> allowed, string label) { if (!allowed.Contains(value)) throw new InvalidDataException($"Knowledge discovery response references an unknown {label}."); }
    static void References(IReadOnlyList<string> values, IReadOnlyCollection<string> allowed, int minimum, string label) { if (values is null || values.Count < minimum) throw new InvalidDataException($"Knowledge discovery response requires at least one {label}."); Unique(values, $"{label} keys"); foreach (var value in values) Reference(value, allowed, label); }
    static void Sources(IReadOnlyList<Guid> values, HashSet<Guid> allowed, int minimum) { if (values is null || values.Count < minimum) throw new InvalidDataException("Knowledge discovery response contains too few source document references."); Unique(values, "source document references"); if (values.Any(x => !allowed.Contains(x))) throw new InvalidDataException("Knowledge discovery response references an unknown source document."); }
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")] private static partial Regex Kebab();
}
