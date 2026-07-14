using System.Text.RegularExpressions;
using OrgWiki.Application.Ingestion;

namespace OrgWiki.Infrastructure.Ingestion;

public sealed partial class ContentNormalizer : IContentNormalizer
{
    public NormalizedContent Normalize(string content)
    {
        var normalized = content.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\0", string.Empty);
        normalized = BlankLinesRegex().Replace(normalized, "\n\n").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return new NormalizedContent(string.Empty, 0, 0);

        var words = Regex.Matches(normalized, @"\S+").Count;
        return new NormalizedContent(normalized, normalized.Length, words);
    }

    [GeneratedRegex(@"[ \t]*\n(?:[ \t]*\n){2,}")]
    private static partial Regex BlankLinesRegex();
}
