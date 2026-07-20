using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrgWiki.Application.TeamSpaces;

namespace OrgWiki.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public/spaces")]
public sealed class PublicTeamSpacesController(IPublicTeamSpaceService teamSpaces) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<PublicTeamSpaceSummary>> GetAll(CancellationToken cancellationToken) => teamSpaces.GetAllAsync(cancellationToken);

    [HttpGet("{slug}")]
    public async Task<ActionResult<PublicTeamSpace>> Get(string slug, CancellationToken cancellationToken)
        => await teamSpaces.GetAsync(slug, cancellationToken) is { } space ? Ok(space) : NotFound();

    [HttpGet("{slug}/articles/{articleKey}")]
    public async Task<ActionResult<PublicTeamSpaceArticle>> Article(string slug, string articleKey, CancellationToken cancellationToken)
        => await teamSpaces.GetArticleAsync(slug, articleKey, cancellationToken) is { } article ? Ok(article) : NotFound();
}
