using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PostulaAI.Api.Shared.Errors;

/// <summary>Builds RFC 9457 ProblemDetails with the project's <c>errorCode</c> and <c>traceId</c> extensions.</summary>
public static class ProblemDetailsWriter
{
    public const string ContentType = "application/problem+json";
    public const string ValidationTitle = "Revisa los datos enviados.";

    public static ObjectResult FromError(AppError error, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(httpContext);

        var problem = new ProblemDetails { Status = error.Status, Title = error.Title };
        AddExtensions(problem, error.Code, httpContext);
        return Wrap(problem, error.Status);
    }

    public static ObjectResult FromValidation(ValidationResult validation, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(validation);

        var errors = validation.Errors
            .GroupBy(failure => ToCamelCasePath(failure.PropertyName))
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

        return FromErrors(errors, httpContext);
    }

    public static ObjectResult FromModelState(ModelStateDictionary modelState, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(modelState);

        // Model-binding messages can echo raw input; a fixed message avoids reflecting user data back.
        var errors = new Dictionary<string, string[]>();
        foreach (var entry in modelState)
        {
            if (entry.Value is { Errors.Count: > 0 })
            {
                errors[ToCamelCasePath(entry.Key.TrimStart('$', '.'))] = ["Valor ausente o con formato inválido."];
            }
        }

        return FromErrors(errors, httpContext);
    }

    /// <summary>Converts "Experiences[0].EndDate" to "experiences[0].endDate" so the frontend can map errors to fields.</summary>
    public static string ToCamelCasePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var segments = path.Split('.');
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (segment.Length > 0 && char.IsUpper(segment[0]))
            {
                segments[i] = char.ToLowerInvariant(segment[0]) + segment[1..];
            }
        }

        return string.Join('.', segments);
    }

    private static ObjectResult FromErrors(Dictionary<string, string[]> errors, HttpContext httpContext)
    {
        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = ValidationTitle,
        };
        AddExtensions(problem, ErrorCodes.ValidationFailed, httpContext);
        return Wrap(problem, StatusCodes.Status400BadRequest);
    }

    private static void AddExtensions(ProblemDetails problem, string errorCode, HttpContext httpContext)
    {
        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
    }

    private static ObjectResult Wrap(ProblemDetails problem, int status)
    {
        var result = new ObjectResult(problem) { StatusCode = status };
        result.ContentTypes.Add(ContentType);
        return result;
    }
}
