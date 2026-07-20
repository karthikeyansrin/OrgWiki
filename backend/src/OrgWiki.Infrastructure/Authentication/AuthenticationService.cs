using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            {
                SqlState: "23505",
                ConstraintName: "IX_users_Email"
            })
        {
            throw new InvalidOperationException("An account with this email already exists.");
        }
        return CreateResponse(user);
    }

    public async Task<AuthenticationResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password)) return null;
        if (request.Email.Length > 320 || request.Password.Length > 1024) return null;
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
        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length > 256) throw new InvalidOperationException("Full name is required and must be 256 characters or fewer.");
        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Trim().Length > 320 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.Email)) throw new InvalidOperationException("A valid email address is required.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8 || request.Password.Length > 1024) throw new InvalidOperationException("Password must be between 8 and 1024 characters.");
        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal)) throw new InvalidOperationException("Password confirmation does not match.");
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
