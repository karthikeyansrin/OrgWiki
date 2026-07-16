using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using OrgWiki.Application.Authentication;

namespace OrgWiki.API.Authentication;

public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User ?? throw new InvalidOperationException("No authenticated user is available.");
    public Guid Id => Guid.TryParse(Principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : throw new InvalidOperationException("The authenticated user identifier is invalid.");
    public string FullName => Principal.FindFirstValue(JwtRegisteredClaimNames.Name) ?? throw new InvalidOperationException("The authenticated user name is missing.");
    public string Email => Principal.FindFirstValue(JwtRegisteredClaimNames.Email) ?? throw new InvalidOperationException("The authenticated user email is missing.");
}
