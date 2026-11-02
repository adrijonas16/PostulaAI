using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using PostulaAI.Api.Shared.Errors;
using PostulaAI.Api.Shared.Security;

namespace PostulaAI.Api.Shared.Api;

/// <summary>Base for thin controllers: maps service results and validation to ProblemDetails.</summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Id of the authenticated user. Only valid on endpoints that require authorization.</summary>
    protected Guid CurrentUserId => User.GetRequiredUserId();

    protected ObjectResult ProblemFor(AppError error) => ProblemDetailsWriter.FromError(error, HttpContext);

    protected ObjectResult ValidationProblemFor(ValidationResult validation) =>
        ProblemDetailsWriter.FromValidation(validation, HttpContext);
}
