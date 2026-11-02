using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostulaAI.Api.Modules.JobPostings.Contracts;
using PostulaAI.Api.Shared.Api;
using PostulaAI.Api.Shared.Errors;

namespace PostulaAI.Api.Modules.JobPostings;

[Route("api/v1/job-postings")]
[Authorize]
public sealed class JobPostingsController(
    JobPostingService service,
    IValidator<JobPostingRequest> requestValidator,
    IValidator<JobPostingListQuery> queryValidator) : ApiControllerBase
{
    private const int MaxRequestBytes = 128 * 1024;

    [HttpGet]
    [ProducesResponseType<PagedResponse<JobPostingSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new JobPostingListQuery(page, pageSize, search);
        var validation = await queryValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblemFor(validation);
        }

        return Ok(await service.ListAsync(CurrentUserId, query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<JobPostingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(CurrentUserId, id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ProblemFor(result.Error);
    }

    [HttpPost]
    [RequestSizeLimit(MaxRequestBytes)]
    [ProducesResponseType<JobPostingResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(JobPostingRequest request, CancellationToken cancellationToken)
    {
        var validation = await requestValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblemFor(validation);
        }

        var created = await service.CreateAsync(CurrentUserId, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequestSizeLimit(MaxRequestBytes)]
    [ProducesResponseType<JobPostingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, JobPostingRequest request, CancellationToken cancellationToken)
    {
        var validation = await requestValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblemFor(validation);
        }

        var result = await service.UpdateAsync(CurrentUserId, id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ProblemFor(result.Error);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        return await service.DeleteAsync(CurrentUserId, id, cancellationToken)
            ? NoContent()
            : ProblemFor(AppError.NotFound(ErrorCodes.JobPostingNotFound, "No encontramos esa vacante."));
    }
}
