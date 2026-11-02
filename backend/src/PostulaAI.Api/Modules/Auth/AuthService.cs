using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PostulaAI.Api.Modules.AuditLogs;
using PostulaAI.Api.Modules.AuditLogs.Domain;
using PostulaAI.Api.Modules.Auth.Contracts;
using PostulaAI.Api.Modules.Auth.Domain;
using PostulaAI.Api.Modules.Users.Domain;
using PostulaAI.Api.Shared.Errors;
using PostulaAI.Api.Shared.Persistence;

namespace PostulaAI.Api.Modules.Auth;

public sealed class AuthService(
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokens,
    IAuditTrail audit,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider time)
{
    private static readonly AppError InvalidCredentials = AppError.Unauthorized(
        ErrorCodes.InvalidCredentials, "Correo o contraseña incorrectos.");

    private static readonly AppError InvalidRefreshToken = AppError.Unauthorized(
        ErrorCodes.InvalidRefreshToken, "Tu sesión expiró. Inicia sesión de nuevo.");

    // Used to spend the same hashing time when the e-mail does not exist (mitigates user enumeration by timing).
    private static readonly User TimingDummyUser = new() { Email = "timing@invalid", PasswordHash = string.Empty, DisplayName = "-" };
    private static readonly Lazy<string> TimingDummyHash = new(() =>
        new PasswordHasher<User>().HashPassword(TimingDummyUser, Guid.NewGuid().ToString()));

    public async Task<Result<AuthSession>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = User.NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            return AppError.Conflict(ErrorCodes.EmailAlreadyRegistered, "Ya existe una cuenta con ese correo.");
        }

        var now = time.GetUtcNowForStorage();
        var newUser = new User
        {
            Email = email,
            PasswordHash = string.Empty,
            DisplayName = request.DisplayName.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        newUser.PasswordHash = passwordHasher.HashPassword(newUser, request.Password);

        db.Users.Add(newUser);
        audit.Record(AuditActions.UserRegistered, newUser.Id, nameof(User), newUser.Id);

        var session = IssueSession(newUser, familyId: Guid.CreateVersion7(), now);
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<Result<AuthSession>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = User.NormalizeEmail(request.Email);
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        if (user is null)
        {
            passwordHasher.VerifyHashedPassword(TimingDummyUser, TimingDummyHash.Value, request.Password);
            audit.Record(AuditActions.LoginFailed, userId: null);
            await db.SaveChangesAsync(cancellationToken);
            return InvalidCredentials;
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            audit.Record(AuditActions.LoginFailed, user.Id, nameof(User), user.Id);
            await db.SaveChangesAsync(cancellationToken);
            return InvalidCredentials;
        }

        var now = time.GetUtcNowForStorage();
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            user.UpdatedAt = now;
        }

        audit.Record(AuditActions.LoginSucceeded, user.Id, nameof(User), user.Id);
        var session = IssueSession(user, familyId: Guid.CreateVersion7(), now);
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<Result<AuthSession>> RefreshAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return InvalidRefreshToken;
        }

        var hash = tokens.HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);
        if (stored is null)
        {
            return InvalidRefreshToken;
        }

        var now = time.GetUtcNowForStorage();
        switch (RefreshTokenPolicy.Evaluate(stored, now))
        {
            case RefreshTokenState.Reused:
                await RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
                audit.Record(AuditActions.RefreshTokenReuseDetected, stored.UserId, nameof(RefreshToken), stored.Id);
                await db.SaveChangesAsync(cancellationToken);
                return InvalidRefreshToken;
            case RefreshTokenState.Revoked:
            case RefreshTokenState.Expired:
                return InvalidRefreshToken;
            case RefreshTokenState.Active:
            default:
                break;
        }

        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Id == stored.UserId, cancellationToken);
        if (user is null)
        {
            return InvalidRefreshToken;
        }

        var session = IssueSession(user, stored.FamilyId, now, out var replacement);
        stored.RevokedAt = now;
        stored.ReplacedByTokenId = replacement.Id;
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }

    /// <summary>Revokes the whole token family of the presented refresh token. Idempotent.</summary>
    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var hash = tokens.HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);
        if (stored is null)
        {
            return;
        }

        await RevokeFamilyAsync(stored.FamilyId, time.GetUtcNowForStorage(), cancellationToken);
        audit.Record(AuditActions.LoggedOut, stored.UserId, nameof(User), stored.UserId);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result<UserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            return AppError.Unauthorized(ErrorCodes.Unauthorized, "Tu sesión no es válida. Inicia sesión de nuevo.");
        }

        return ToResponse(user);
    }

    private AuthSession IssueSession(User user, Guid familyId, DateTimeOffset now) =>
        IssueSession(user, familyId, now, out _);

    private AuthSession IssueSession(User user, Guid familyId, DateTimeOffset now, out RefreshToken refreshToken)
    {
        var accessToken = tokens.CreateAccessToken(user);
        var generated = tokens.GenerateRefreshToken();

        refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = generated.TokenHash,
            FamilyId = familyId,
            CreatedAt = now,
            ExpiresAt = now.AddDays(jwtOptions.Value.RefreshTokenDays),
        };
        db.RefreshTokens.Add(refreshToken);

        return new AuthSession(
            new AuthResponse(accessToken.Token, accessToken.ExpiresAt, ToResponse(user)),
            generated.Token,
            refreshToken.ExpiresAt);
    }

    private async Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var active = await db.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
        {
            token.RevokedAt = now;
        }
    }

    private static UserResponse ToResponse(User user) => new(user.Id, user.Email, user.DisplayName);
}
