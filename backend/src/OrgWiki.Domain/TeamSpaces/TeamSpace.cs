namespace OrgWiki.Domain.TeamSpaces;

public sealed class TeamSpace
{
    private TeamSpace() { }

    public TeamSpace(string name, string slug, string description)
    {
        Name = name;
        Slug = slug;
        Description = description;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public List<TeamSpaceArticle> ArticleAssignments { get; private set; } = [];
}
