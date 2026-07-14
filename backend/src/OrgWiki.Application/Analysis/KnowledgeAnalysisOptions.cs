namespace OrgWiki.Application.Analysis;

public sealed class KnowledgeAnalysisOptions
{
    public const string SectionName = "AI";
    public string Mode { get; set; } = "Replay";
    public string Model { get; set; } = "gpt-5.6";
    public int KnowledgeDiscoveryMaxOutputTokens { get; set; } = 5000;
    public string ReplayFixturePath { get; set; } = "replay-knowledge-discovery.json";
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 180;
}
