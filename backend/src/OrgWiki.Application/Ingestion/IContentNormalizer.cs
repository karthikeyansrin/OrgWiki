namespace OrgWiki.Application.Ingestion;

public sealed record NormalizedContent(string Content, int CharacterCount, int WordCount);

public interface IContentNormalizer
{
    NormalizedContent Normalize(string content);
}
