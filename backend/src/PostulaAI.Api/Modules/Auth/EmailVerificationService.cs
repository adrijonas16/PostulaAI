using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PostulaAI.Api.Modules.AuditLogs;
using PostulaAI.Api.Modules.AuditLogs.Domain;
using PostulaAI.Api.Modules.Auth.Domain;
using PostulaAI.Api.Modules.Users.Domain;
using PostulaAI.Api.Shared.Email;
using PostulaAI.Api.Shared.Errors;
using PostulaAI.Api.Shared.Persistence;

namespace PostulaAI.Api.Modules.Auth;

/// <summary>
/// Confirms that a user owns their e-mail address. AI features (which cost money per call) require it, so
/// creating throwaway accounts does not unlock free AI usage. Logs carry identifiers only, never addresses.
/// </summary>
public sealed partial class EmailVerificationService(
    AppDbContext db,
    ITokenService tokens,
    IEmailSender sender,
    IOptions<EmailOptions> options,
    IAuditTrail audit,
    TimeProvider time,
    ILogger<EmailVerificationService> logger)
{
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

    private static readonly AppError InvalidToken = new(ErrorCodes.InvalidVerificationToken, StatusCodes.Status400BadRequest,
        "El enlace de confirmación no es válido o ya venció. Pide uno nuevo desde la aplicación.");

    public bool IsEnforced => options.Value.VerificationEnforced;

    /// <summary>True when verification is enforced and this user has not confirmed their e-mail yet.</summary>
    public async Task<bool> IsPendingAsync(Guid userId, CancellationToken cancellationToken) =>
        IsEnforced && await db.Users.AsNoTracking()
            .AnyAsync(user => user.Id == userId && user.EmailVerifiedAt == null, cancellationToken);

    /// <summary>Creates a token and e-mails the link. A delivery failure is logged and swallowed: the user can resend.</summary>
    public async Task SendAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (!options.Value.IsConfigured || user.EmailVerifiedAt is not null)
        {
            return;
        }

        var now = time.GetUtcNowForStorage();
        var generated = tokens.GenerateRefreshToken();
        db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            UserId = user.Id,
            TokenHash = generated.TokenHash,
            CreatedAt = now,
            ExpiresAt = now + TokenLifetime,
        });
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await sender.SendAsync(BuildMessage(user, generated.Token), cancellationToken);
            audit.Record(AuditActions.VerificationEmailSent, user.Id, nameof(User), user.Id);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogSendFailed(logger, user.Id, exception.GetType().Name);
        }
    }

    /// <returns>null on success (or when there is nothing to send); otherwise the error.</returns>
    public async Task<AppError?> ResendAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null || user.EmailVerifiedAt is not null || !options.Value.IsConfigured)
        {
            return null;
        }

        var cooldownStart = time.GetUtcNow() - ResendCooldown;
        if (await db.EmailVerificationTokens.AnyAsync(token => token.UserId == userId && token.CreatedAt > cooldownStart, cancellationToken))
        {
            return new AppError(ErrorCodes.VerificationEmailCooldown, StatusCodes.Status429TooManyRequests,
                "Ya te enviamos un correo hace un momento. Espera un minuto antes de pedir otro.");
        }

        await SendAsync(user, cancellationToken);
        return null;
    }

    /// <returns>null when the e-mail is now confirmed; otherwise the error.</returns>
    public async Task<AppError?> VerifyAsync(string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return InvalidToken;
        }

        var hash = tokens.HashRefreshToken(token.Trim());
        var stored = await db.EmailVerificationTokens.SingleOrDefaultAsync(candidate => candidate.TokenHash == hash, cancellationToken);
        var now = time.GetUtcNowForStorage();
        if (stored is null || stored.UsedAt is not null || stored.ExpiresAt <= now)
        {
            return InvalidToken;
        }

        var user = await db.Users.SingleAsync(candidate => candidate.Id == stored.UserId, cancellationToken);
        stored.UsedAt = now;
        user.EmailVerifiedAt ??= now;
        user.UpdatedAt = now;
        audit.Record(AuditActions.EmailVerified, user.Id, nameof(User), user.Id);
        await db.SaveChangesAsync(cancellationToken);
        return null;
    }

    private EmailMessage BuildMessage(User user, string token)
    {
        var link = $"{options.Value.AppBaseUrl.TrimEnd('/')}/verificar-correo?token={Uri.EscapeDataString(token)}";
        var name = user.DisplayName;
        var text = $"""
            Hola, {name}:

            Confirma tu correo para empezar a usar el análisis y la importación del CV en PostulaAI:

            {link}

            El enlace vence en 24 horas. Si no creaste una cuenta en PostulaAI, ignora este mensaje.
            """;
        var html = $"""
            <p>Hola, {WebUtility.HtmlEncode(name)}:</p>
            <p>Confirma tu correo para empezar a usar el análisis y la importación del CV en PostulaAI.</p>
            <p><a href="{WebUtility.HtmlEncode(link)}">Confirmar mi correo</a></p>
            <p>El enlace vence en 24 horas. Si no creaste una cuenta en PostulaAI, ignora este mensaje.</p>
            """;
        return new EmailMessage(user.Email, "Confirma tu correo en PostulaAI", text, html);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Verification e-mail could not be sent for user {UserId} ({ErrorType}).")]
    private static partial void LogSendFailed(ILogger logger, Guid userId, string errorType);
}
