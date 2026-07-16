namespace OrgWiki.API.Options;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;
    public string Mode { get; set; } = "Replay";
    public int KnowledgeDiscoveryMaxOutputTokens { get; set; } = 5000;
    public string ReplayFixturePath { get; set; } = "replay-knowledge-discovery.json";
    public int TimeoutSeconds { get; set; } = 180;
    public bool VerboseLogging { get; set; }
    public decimal? InputTokenCostPerMillionUsd { get; set; }
    public decimal? OutputTokenCostPerMillionUsd { get; set; }
}
