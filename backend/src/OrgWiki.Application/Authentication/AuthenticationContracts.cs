namespace OrgWiki.Application.Authentication;

public sealed record RegisterRequest(string FullName, string Email, string Password, string ConfirmPassword);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthenticatedUser(Guid Id, string FullName, string Email);
public sealed record AuthenticationResponse(string AccessToken, DateTime ExpiresAtUtc, AuthenticatedUser User);
public sealed record IssuedAccessToken(string AccessToken, DateTime ExpiresAtUtc);

public interface IAuthenticationService
{
    Task<AuthenticationResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthenticationResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthenticatedUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken);
}

public interface IAccessTokenService
{
    IssuedAccessToken Create(AuthenticatedUser user);
}

public interface ICurrentUser
{
    Guid Id { get; }
    string FullName { get; }
    string Email { get; }
}
