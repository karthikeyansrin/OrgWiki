using System.Text.Json.Nodes;

namespace OrgWiki.Infrastructure.Analysis;

public static class KnowledgeDiscoverySchema
{
    public static JsonObject CreateResponseFormat() => new()
    {
        ["type"] = "json_schema",
        ["json_schema"] = new JsonObject
        {
            ["name"] = "orgwiki_knowledge_discovery",
            ["strict"] = true,
            ["schema"] = CreateSchema()
        }
    };

    static JsonObject CreateSchema()
    {
        var confidence = new JsonObject { ["type"] = "number", ["minimum"] = 0, ["maximum"] = 1 };
        var guid = new JsonObject { ["type"] = "string", ["format"] = "uuid" };
        var stringArray = StringArray();
        var guidArray = GuidArray();
        var domain = Object(["key", "name", "description", "confidence"], new JsonObject { ["key"] = String(), ["name"] = String(), ["description"] = String(), ["confidence"] = confidence.DeepClone() });
        var topic = Object(["key", "name", "description", "domainKey", "confidence", "sourceDocumentIds"], new JsonObject { ["key"] = String(), ["name"] = String(), ["description"] = String(), ["domainKey"] = String(), ["confidence"] = confidence.DeepClone(), ["sourceDocumentIds"] = GuidArray(1) });
        var relationship = Object(["sourceTopicKey", "targetTopicKey", "type", "explanation", "confidence"], new JsonObject { ["sourceTopicKey"] = String(), ["targetTopicKey"] = String(), ["type"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("DependsOn", "RelatedTo", "Uses", "PartOf", "Supersedes") }, ["explanation"] = String(), ["confidence"] = confidence.DeepClone() });
        var duplicate = Object(["title", "explanation", "confidence", "topicKeys", "sourceDocumentIds"], new JsonObject { ["title"] = String(), ["explanation"] = String(), ["confidence"] = confidence.DeepClone(), ["topicKeys"] = StringArray(1), ["sourceDocumentIds"] = GuidArray(2) });
        var conflict = Object(["title", "description", "topicKeys", "claimA", "claimB", "sourceDocumentIdA", "sourceDocumentIdB", "evidenceSnippetA", "evidenceSnippetB", "recommendation", "recommendationReasoning", "confidence"], new JsonObject { ["title"] = String(), ["description"] = String(), ["topicKeys"] = StringArray(1), ["claimA"] = String(), ["claimB"] = String(), ["sourceDocumentIdA"] = guid.DeepClone(), ["sourceDocumentIdB"] = guid.DeepClone(), ["evidenceSnippetA"] = String(), ["evidenceSnippetB"] = String(), ["recommendation"] = String(), ["recommendationReasoning"] = String(), ["confidence"] = confidence.DeepClone() });
        var outdated = Object(["description", "reason", "topicKeys", "sourceDocumentIds", "confidence"], new JsonObject { ["description"] = String(), ["reason"] = String(), ["topicKeys"] = StringArray(1), ["sourceDocumentIds"] = GuidArray(1), ["confidence"] = confidence.DeepClone() });
        var article = Object(["key", "title", "summary", "domainKey", "topicKeys", "sourceDocumentIds", "reason", "confidence"], new JsonObject { ["key"] = String(), ["title"] = String(), ["summary"] = String(), ["domainKey"] = String(), ["topicKeys"] = StringArray(1), ["sourceDocumentIds"] = GuidArray(1), ["reason"] = String(), ["confidence"] = confidence.DeepClone() });
        return Object(["domains", "topics", "relationships", "duplicateGroups", "conflicts", "outdatedCandidates", "suggestedArticles"], new JsonObject { ["domains"] = Array(domain), ["topics"] = Array(topic), ["relationships"] = Array(relationship), ["duplicateGroups"] = Array(duplicate), ["conflicts"] = Array(conflict), ["outdatedCandidates"] = Array(outdated), ["suggestedArticles"] = Array(article) });
    }
    static JsonObject String() => new() { ["type"] = "string", ["minLength"] = 1 };
    static JsonObject StringArray(int minItems = 0) => new() { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string", ["minLength"] = 1 }, ["minItems"] = minItems };
    static JsonObject GuidArray(int minItems = 0) => new() { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string", ["format"] = "uuid" }, ["minItems"] = minItems };
    static JsonObject Array(JsonObject item) => new() { ["type"] = "array", ["items"] = item };
    static JsonObject Object(string[] required, JsonObject properties) => new() { ["type"] = "object", ["properties"] = properties, ["required"] = new JsonArray(required.Select(x => (JsonNode)JsonValue.Create(x)!).ToArray()), ["additionalProperties"] = false };
}
