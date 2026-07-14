namespace OrgWiki.Domain.Analysis;

public sealed class GeneratedArticleCitation
{
    private GeneratedArticleCitation() { }
    public GeneratedArticleCitation(Guid articleId, Guid sourceDocumentId, string evidenceSnippet)
    { GeneratedArticleId = articleId; SourceDocumentId = sourceDocumentId; EvidenceSnippet = evidenceSnippet; }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid GeneratedArticleId { get; private set; }
    public Guid SourceDocumentId { get; private set; }
    public string EvidenceSnippet { get; private set; } = string.Empty;
    public GeneratedArticle Article { get; private set; } = null!;
}
