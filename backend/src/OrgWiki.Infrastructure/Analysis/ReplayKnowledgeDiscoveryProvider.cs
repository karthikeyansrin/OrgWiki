using OrgWiki.Application.Analysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace OrgWiki.Infrastructure.Analysis;

public sealed class ReplayKnowledgeDiscoveryProvider(IOptions<KnowledgeAnalysisOptions> options) : IKnowledgeDiscoveryProvider
{
    public Task<KnowledgeDiscoveryResponse> DiscoverAsync(KnowledgeDiscoveryRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var first = request.Documents[0]; var second = request.Documents.Count > 1 ? request.Documents[1] : first;
        if (request.Documents.Count == 1)
        {
            var single = new KnowledgeDiscoveryResult([new("engineering", "Engineering", "Technical systems and infrastructure.", .9)], [new("documentation", "Documentation", "Organizational guidance.", "engineering", .8, [first.Id])], [], [], [], [new("Potentially superseded guidance", "The source should be reviewed for recency.", ["documentation"], [first.Id], .6)], [new("documentation-overview", "Documentation Overview", "A structured overview of the source.", "engineering", ["documentation"], [first.Id], "The source contains coherent guidance.", .8)]);
            return Task.FromResult(new KnowledgeDiscoveryResponse(single, null));
        }
        var configuredPath = Path.GetFullPath(options.Value.ReplayFixturePath);
        if (File.Exists(configuredPath))
        {
            var node = JsonNode.Parse(File.ReadAllText(configuredPath)) ?? throw new InvalidDataException("Replay fixture is empty.");
            Replace(node, first, second);
            var fixture = node.Deserialize<KnowledgeDiscoveryResult>(new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new InvalidDataException("Replay fixture is invalid.");
            return Task.FromResult(new KnowledgeDiscoveryResponse(fixture, null));
        }
        var result = new KnowledgeDiscoveryResult(
            [new("engineering", "Engineering", "Technical systems, APIs, and infrastructure.", .96), new("operations", "Operations", "Operational procedures and organizational guidance.", .88)],
            [new("authentication", "Authentication", "Identity and access control.", "engineering", .94, [first.Id]), new("api-gateway", "API Gateway", "Entry point for service APIs.", "engineering", .9, [first.Id]), new("security", "Security", "Security controls and practices.", "engineering", .89, [first.Id]), new("onboarding", "Employee Onboarding", "Guidance for new employees.", "operations", .86, [second.Id]), new("leave", "Annual Leave", "Annual leave policy.", "operations", .84, [second.Id]), new("documentation", "Documentation", "Maintained organizational guidance.", "operations", .8, [first.Id])],
            [new("authentication", "security", "RelatedTo", "Authentication is a security control.", .86), new("api-gateway", "authentication", "Uses", "The gateway uses authentication.", .82)],
            [new("Authentication guidance overlap", "Authentication guidance overlaps across source documents.", .8, ["authentication"], [first.Id, second.Id])],
            [new("Authentication policy conflict", "Sources describe different authentication guidance.", ["authentication"], "Authentication settings differ between the supplied sources.", "Authentication settings require review.", first.Id, second.Id, first.Content[..Math.Min(40, first.Content.Length)], second.Content[..Math.Min(40, second.Content.Length)], "Review the source policies.", "The sources provide different guidance and require human review.", .75)],
            [new("Potentially superseded guidance", "The corpus contains overlapping guidance that should be reviewed.", ["authentication"], [second.Id], .7)],
            [new("authentication-overview", "Authentication Overview", "A concise overview of authentication and security guidance.", "engineering", ["authentication", "security"], [first.Id, second.Id], "The topic appears across multiple sources.", .9), new("api-platform-overview", "API Platform Overview", "How the API gateway fits into the platform.", "engineering", ["api-gateway"], [first.Id], "The gateway is a recurring platform concept.", .8), new("onboarding-guide", "Employee Onboarding", "The organization's onboarding guidance.", "operations", ["onboarding"], [second.Id], "Onboarding is a coherent operational topic.", .83)]);
        return Task.FromResult(new KnowledgeDiscoveryResponse(result, null));
    }

    static void Replace(JsonNode node, CorpusDocument first, CorpusDocument second)
    {
        if (node is JsonValue value && value.TryGetValue<string>(out var text))
        {
            var replaced = text
                .Replace("FIRST_SNIPPET", first.Content[..Math.Min(40, first.Content.Length)])
                .Replace("SECOND_SNIPPET", second.Content[..Math.Min(40, second.Content.Length)])
                .Replace("FIRST", first.Id.ToString())
                .Replace("SECOND", second.Id.ToString());
            value.ReplaceWith(JsonValue.Create(replaced));
            return;
        }
        if (node is JsonObject obj) foreach (var child in obj.ToList()) if (child.Value is not null) Replace(child.Value, first, second);
        if (node is JsonArray array) foreach (var child in array.ToList()) if (child is not null) Replace(child, first, second);
    }
}
