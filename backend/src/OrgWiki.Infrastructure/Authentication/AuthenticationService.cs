using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OrgWiki.Application.Authentication;
using OrgWiki.Domain.Authentication;
using OrgWiki.Infrastructure.Persistence;

namespace OrgWiki.Infrastructure.Authentication;

public sealed class AuthenticationService(
    OrgWikiDbContext db,
    IPasswordHasher<User> passwordHasher,
    IAccessTokenService tokens) : IAuthenticationService
{
    public async Task<AuthenticationResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        ValidateRegistration(request);
        var email = NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
            throw new InvalidOperationException("An account with this email already exists.");

        var user = new User(request.FullName.Trim(), email);
        user.SetPasswordHash(passwordHasher.HashPassword(user, request.Password));
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return CreateResponse(user);
    }

    public async Task<AuthenticationResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password)) return null;
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == NormalizeEmail(request.Email), cancellationToken);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed) return null;
        return CreateResponse(user);
    }

    public async Task<AuthenticatedUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await db.Users.Where(x => x.Id == userId)
            .Select(x => new AuthenticatedUser(x.Id, x.FullName, x.Email))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private AuthenticationResponse CreateResponse(User user)
    {
        var authenticatedUser = new AuthenticatedUser(user.Id, user.FullName, user.Email);
        var token = tokens.Create(authenticatedUser);
        return new AuthenticationResponse(token.AccessToken, token.ExpiresAtUtc, authenticatedUser);
    }

    private static void ValidateRegistration(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName)) throw new InvalidOperationException("Full name is required.");
        if (string.IsNullOrWhiteSpace(request.Email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.Email)) throw new InvalidOperationException("A valid email address is required.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8) throw new InvalidOperationException("Password must be at least 8 characters.");
        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal)) throw new InvalidOperationException("Password confirmation does not match.");
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
