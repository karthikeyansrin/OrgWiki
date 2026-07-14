using System.Text.Json.Nodes;

namespace OrgWiki.Infrastructure.Analysis;

public static class KnowledgeGenerationSchema
{
    public static JsonObject CreateResponseFormat() => new()
    {
        ["type"] = "json_schema",
        ["json_schema"] = new JsonObject { ["name"] = "orgwiki_knowledge_generation", ["strict"] = true, ["schema"] = Schema() }
    };
    static JsonObject Schema()
    {
        var article = Object(["key", "title", "summary", "markdownContent", "difficulty", "estimatedReadingMinutes", "tags", "relatedArticleKeys", "confidence", "citations"], new JsonObject
        {
            ["key"] = String(), ["title"] = String(), ["summary"] = String(), ["markdownContent"] = String(),
            ["difficulty"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("Beginner", "Intermediate", "Advanced") },
            ["estimatedReadingMinutes"] = new JsonObject { ["type"] = "integer", ["minimum"] = 1 }, ["tags"] = StringArray(), ["relatedArticleKeys"] = StringArray(), ["confidence"] = new JsonObject { ["type"] = "number", ["minimum"] = 0, ["maximum"] = 1 },
            ["citations"] = new JsonObject { ["type"] = "array", ["minItems"] = 1, ["items"] = Object(["sourceDocumentId", "evidenceSnippet"], new JsonObject { ["sourceDocumentId"] = new JsonObject { ["type"] = "string", ["format"] = "uuid" }, ["evidenceSnippet"] = String() }) }
        });
        return Object(["articles"], new JsonObject { ["articles"] = new JsonObject { ["type"] = "array", ["items"] = article, ["uniqueItems"] = true } });
    }
    static JsonObject String() => new() { ["type"] = "string", ["minLength"] = 1 };
    static JsonObject StringArray() => new() { ["type"] = "array", ["items"] = String(), ["uniqueItems"] = true };
    static JsonObject Object(string[] required, JsonObject properties) => new() { ["type"] = "object", ["properties"] = properties, ["required"] = new JsonArray(required.Select(x => (JsonNode)JsonValue.Create(x)!).ToArray()), ["additionalProperties"] = false };
}
