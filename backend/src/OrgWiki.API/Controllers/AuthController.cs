using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrgWiki.Application.Authentication;

namespace OrgWiki.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthenticationService authentication, ICurrentUser currentUser) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthenticationResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await authentication.RegisterAsync(request, cancellationToken)); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
        => await authentication.LoginAsync(request, cancellationToken) is { } response ? Ok(response) : Unauthorized(new { error = "Email or password is incorrect." });

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthenticatedUser>> Me(CancellationToken cancellationToken)
        => await authentication.GetUserAsync(currentUser.Id, cancellationToken) is { } user ? Ok(user) : Unauthorized();
}
