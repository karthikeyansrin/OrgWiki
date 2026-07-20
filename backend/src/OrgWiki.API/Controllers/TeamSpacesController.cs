using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrgWiki.Application.TeamSpaces;

namespace OrgWiki.API.Controllers;

[ApiController]
[Authorize]
[Route("api/team-spaces")]
public sealed class TeamSpacesController(ITeamSpaceService teamSpaces) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<TeamSpaceSummary>> GetAll(CancellationToken cancellationToken) => teamSpaces.GetAllAsync(cancellationToken);

    [HttpPost]
    public async Task<ActionResult<TeamSpaceSummary>> Create(CreateTeamSpaceRequest request, CancellationToken cancellationToken)
    {
        try { return Created($"/api/team-spaces/{request.Slug}", await teamSpaces.CreateAsync(request, cancellationToken)); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => await teamSpaces.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("articles/{articleKey}")]
    public async Task<ActionResult<ArticleTeamSpaces>> Assignments(string articleKey, CancellationToken cancellationToken)
        => await teamSpaces.GetArticleAssignmentsAsync(articleKey, cancellationToken) is { } assignments ? Ok(assignments) : NotFound();

    [HttpPut("articles/{articleKey}")]
    public async Task<ActionResult<ArticleTeamSpaces>> UpdateAssignments(string articleKey, UpdateArticleTeamSpacesRequest request, CancellationToken cancellationToken)
    {
        try { return await teamSpaces.UpdateArticleAssignmentsAsync(articleKey, request, cancellationToken) is { } assignments ? Ok(assignments) : NotFound(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }
}
