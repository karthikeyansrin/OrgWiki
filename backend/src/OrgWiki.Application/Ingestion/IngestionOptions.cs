namespace OrgWiki.Application.Ingestion;

public sealed class IngestionOptions
{
    public const string SectionName = "Ingestion";
    public long MaxArchiveBytes { get; set; } = 10 * 1024 * 1024;
    public int MaxSupportedDocuments { get; set; } = 10;
    public long MaxIndividualFileBytes { get; set; } = 2 * 1024 * 1024;
    public long MaxTotalExtractedBytes { get; set; } = 25 * 1024 * 1024;
    public int MaxNormalizedCharactersPerDocument { get; set; } = 75_000;
    public int MaxTotalNormalizedCharacters { get; set; } = 300_000;
    public int MaxPdfPages { get; set; } = 50;
    public string LocalStoragePath { get; set; } = "storage";
}
