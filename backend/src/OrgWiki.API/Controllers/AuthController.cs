using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OrgWiki.Application.Authentication;

namespace OrgWiki.API.Controllers;

[ApiController]
[Route("api/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(IAuthenticationService authentication, ICurrentUser currentUser) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthenticationResponse>> Register(RegisterRequest? request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(new { error = "Registration details are required." });
        try { return Ok(await authentication.RegisterAsync(request, cancellationToken)); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthenticationResponse>> Login(LoginRequest? request, CancellationToken cancellationToken)
        => request is not null && await authentication.LoginAsync(request, cancellationToken) is { } response ? Ok(response) : Unauthorized(new { error = "Email or password is incorrect." });

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthenticatedUser>> Me(CancellationToken cancellationToken)
        => await authentication.GetUserAsync(currentUser.Id, cancellationToken) is { } user ? Ok(user) : Unauthorized();
}
