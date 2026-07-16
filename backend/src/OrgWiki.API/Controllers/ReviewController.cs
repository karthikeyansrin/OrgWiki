using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OrgWiki.Application.Review;

namespace OrgWiki.API.Controllers;

[ApiController]
[Authorize]
[Route("api/review")]
public sealed class ReviewController(IReviewService review) : ControllerBase
{
    [HttpGet]
    public Task<ReviewDashboard> Dashboard(CancellationToken cancellationToken) => review.GetDashboardAsync(cancellationToken);
    [HttpGet("articles/{id:guid}")]
    public async Task<ActionResult<ReviewArticleDetails>> Get(Guid id, CancellationToken cancellationToken) => await review.GetArticleAsync(id, cancellationToken) is { } article ? Ok(article) : NotFound();
    [HttpPut("articles/{id:guid}")]
    public async Task<ActionResult<ReviewArticleDetails>> Update(Guid id, UpdateReviewArticleRequest request, CancellationToken cancellationToken)
    { try { return await review.UpdateAsync(id, request, cancellationToken) is { } article ? Ok(article) : NotFound(); } catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); } }
    [HttpPost("articles/{id:guid}/approve")]
    public async Task<ActionResult<ReviewArticleDetails>> Approve(Guid id, [FromBody] ReviewActionRequest? request, CancellationToken cancellationToken)
    { try { return await review.ApproveAsync(id, request?.Notes, cancellationToken) is { } article ? Ok(article) : NotFound(); } catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); } }
    [HttpPost("articles/{id:guid}/reject")]
    public async Task<ActionResult<ReviewArticleDetails>> Reject(Guid id, [FromBody] ReviewActionRequest? request, CancellationToken cancellationToken)
    { try { return await review.RejectAsync(id, request?.Notes, cancellationToken) is { } article ? Ok(article) : NotFound(); } catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); } }
    [HttpPost("articles/{id:guid}/publish")]
    public async Task<ActionResult<ReviewArticleDetails>> Publish(Guid id, CancellationToken cancellationToken)
    { try { return await review.PublishAsync(id, cancellationToken) is { } article ? Ok(article) : NotFound(); } catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); } }
}
public sealed record ReviewActionRequest(string? Notes);
