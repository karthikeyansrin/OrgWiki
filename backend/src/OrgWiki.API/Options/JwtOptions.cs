namespace OrgWiki.API.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "OrgWiki";
    public string Audience { get; set; } = "OrgWiki";
    public string SecretKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 480;
}
