using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PostulaAI.Api.Modules.Auth.Contracts;
using PostulaAI.Api.Shared.Api;

namespace PostulaAI.Api.Modules.Auth;

[Route("api/v1/auth")]
public sealed class AuthController(
    AuthService authService,
    EmailVerificationService emailVerification,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator) : ApiControllerBase
{
    /// <summary>Creates an account and starts a session.</summary>
    [HttpPost("register")]
    [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var validation = await registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblemFor(validation);
        }

        var result = await authService.RegisterAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return ProblemFor(result.Error);
        }

        SetRefreshCookie(result.Value);
        return StatusCode(StatusCodes.Status201Created, result.Value.Response);
    }

    /// <summary>Starts a session with e-mail and password.</summary>
    [HttpPost("login")]
    [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var validation = await loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblemFor(validation);
        }

        var result = await authService.LoginAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return ProblemFor(result.Error);
        }

        SetRefreshCookie(result.Value);
        return Ok(result.Value.Response);
    }

    /// <summary>Rotates the refresh token (sent as HttpOnly cookie) and returns a new access token.</summary>
    [HttpPost("refresh")]
    [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var result = await authService.RefreshAsync(Request.Cookies[RefreshTokenCookie.Name], cancellationToken);
        if (!result.IsSuccess)
        {
            DeleteRefreshCookie();
            return ProblemFor(result.Error);
        }

        SetRefreshCookie(result.Value);
        return Ok(result.Value.Response);
    }

    /// <summary>Ends the session: revokes the refresh token family and clears the cookie.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(Request.Cookies[RefreshTokenCookie.Name], cancellationToken);
        DeleteRefreshCookie();
        return NoContent();
    }

    /// <summary>Returns the authenticated user.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await authService.GetCurrentUserAsync(CurrentUserId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ProblemFor(result.Error);
    }

    /// <summary>Confirms the e-mail address with the token from the link sent by e-mail.</summary>
    [HttpPost("verify-email")]
    [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var error = await emailVerification.VerifyAsync(request?.Token, cancellationToken);
        return error is null ? NoContent() : ProblemFor(error);
    }

    /// <summary>Sends a new confirmation e-mail to the authenticated user (at most once a minute).</summary>
    [HttpPost("resend-verification")]
    [Authorize]
    [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResendVerification(CancellationToken cancellationToken)
    {
        var error = await emailVerification.ResendAsync(CurrentUserId, cancellationToken);
        return error is null ? NoContent() : ProblemFor(error);
    }

    private void SetRefreshCookie(AuthSession session) =>
        Response.Cookies.Append(RefreshTokenCookie.Name, session.RefreshToken, BuildCookieOptions(session.RefreshTokenExpiresAt));

    private void DeleteRefreshCookie() =>
        Response.Cookies.Delete(RefreshTokenCookie.Name, BuildCookieOptions(expires: null));

    private static CookieOptions BuildCookieOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = RefreshTokenCookie.Path,
        Expires = expires,
        IsEssential = true,
    };
}
