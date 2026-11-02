namespace PostulaAI.Api.Shared.Errors;

/// <summary>
/// Stable, machine-readable error codes returned in ProblemDetails.extensions["errorCode"].
/// The frontend maps these to user-facing messages. Never rename an existing code.
/// </summary>
public static class ErrorCodes
{
    public const string InternalError = "internal_error";
    public const string ValidationFailed = "validation_failed";
    public const string Unauthorized = "unauthorized";
    public const string NotFound = "not_found";
    public const string RateLimited = "rate_limited";
    public const string RequestFailed = "request_failed";

    public const string InvalidCredentials = "invalid_credentials";
    public const string EmailAlreadyRegistered = "email_already_registered";
    public const string InvalidRefreshToken = "invalid_refresh_token";

    public const string ProfileNotFound = "profile_not_found";
}
