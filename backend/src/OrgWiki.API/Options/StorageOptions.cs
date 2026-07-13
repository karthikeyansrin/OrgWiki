namespace OrgWiki.API.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Url { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Bucket { get; set; } = string.Empty;
}
