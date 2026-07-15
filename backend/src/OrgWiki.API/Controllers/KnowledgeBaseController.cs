using Microsoft.AspNetCore.Mvc;
using OrgWiki.Application.KnowledgeBase;

namespace OrgWiki.API.Controllers;

[ApiController]
[Route("api/knowledge")]
public sealed class KnowledgeBaseController(IKnowledgeBaseService knowledgeBase) : ControllerBase
{
    [HttpGet]
    public Task<KnowledgeBaseHome> Home(CancellationToken cancellationToken) => knowledgeBase.GetHomeAsync(cancellationToken);
    [HttpGet("articles/{key}")]
    public async Task<ActionResult<PublishedArticle>> Article(string key, CancellationToken cancellationToken) => await knowledgeBase.GetArticleAsync(key, cancellationToken) is { } article ? Ok(article) : NotFound();
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<PublishedArticleSummary>>> Search([FromQuery] string? q, CancellationToken cancellationToken)
    { try { return Ok(await knowledgeBase.SearchAsync(q ?? string.Empty, cancellationToken)); } catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); } }
}
