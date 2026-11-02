using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PostulaAI.Api.Shared.Errors;

/// <summary>
/// Last-resort handler: logs the exception (no request body, no personal data) and returns a generic
/// ProblemDetails response with a stable error code. Expected errors are handled explicitly by each module.
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        LogUnhandledException(logger, exception, httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Ocurrió un error inesperado. Intenta de nuevo más tarde.",
            Type = "https://www.rfc-editor.org/rfc/rfc9110#name-500-internal-server-error",
        };
        problemDetails.Extensions["errorCode"] = ErrorCodes.InternalError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception. TraceId: {TraceId}")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, string traceId);
}
