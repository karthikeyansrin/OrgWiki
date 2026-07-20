using OrgWiki.Domain.Analysis;

namespace OrgWiki.Domain.TeamSpaces;

public sealed class TeamSpaceArticle
{
    private TeamSpaceArticle() { }

    public TeamSpaceArticle(Guid teamSpaceId, Guid generatedArticleId)
    {
        TeamSpaceId = teamSpaceId;
        GeneratedArticleId = generatedArticleId;
    }

    public Guid TeamSpaceId { get; private set; }
    public Guid GeneratedArticleId { get; private set; }
    public TeamSpace TeamSpace { get; private set; } = null!;
    public GeneratedArticle Article { get; private set; } = null!;
}
