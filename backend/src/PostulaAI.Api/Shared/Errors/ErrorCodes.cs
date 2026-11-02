namespace PostulaAI.Api.Shared.Errors;

/// <summary>
/// Stable, machine-readable error codes returned in ProblemDetails.extensions["errorCode"].
/// The frontend maps these to user-facing messages. Never rename an existing code.
/// </summary>
internal static class ErrorCodes
{
    public const string InternalError = "internal_error";
}
