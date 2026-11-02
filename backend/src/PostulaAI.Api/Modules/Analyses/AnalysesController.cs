using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostulaAI.Api.Modules.Analyses.Contracts;
using PostulaAI.Api.Modules.JobPostings.Contracts;
using PostulaAI.Api.Modules.JobPostings.Domain;
using PostulaAI.Api.Shared.Api;
using PostulaAI.Api.Shared.Errors;

namespace PostulaAI.Api.Modules.Analyses;

[Route("api/v1/analyses")]
[Authorize]
public sealed class AnalysesController(AnalysisService service, IValidator<CreateAnalysisRequest> validator) : ApiControllerBase
{
    /// <summary>Runs an analysis synchronously (typically 10–60 s). Failed runs are stored without a result.</summary>
    [HttpPost]
    [ProducesResponseType<AnalysisResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Create(CreateAnalysisRequest request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblemFor(validation);
        }

        var result = await service.CreateAsync(CurrentUserId, request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : ProblemFor(result.Error);
    }

    [HttpGet]
    [ProducesResponseType<PagedResponse<AnalysisSummary>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > JobPostingLimits.MaxPageSize)
        {
            return ProblemFor(new AppError(ErrorCodes.ValidationFailed, StatusCodes.Status400BadRequest,
                $"Usa page ≥ 1 y pageSize entre 1 y {JobPostingLimits.MaxPageSize}."));
        }

        return Ok(await service.ListAsync(CurrentUserId, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AnalysisResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(CurrentUserId, id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ProblemFor(result.Error);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await service.DeleteAsync(CurrentUserId, id, cancellationToken)
            ? NoContent()
            : ProblemFor(AppError.NotFound(ErrorCodes.AnalysisNotFound, "No encontramos ese análisis."));
}
