using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PostulaAI.Api.Modules.ProfessionalProfiles.Contracts;
using PostulaAI.Api.Modules.ProfessionalProfiles.Import;
using PostulaAI.Api.Shared.Api;
using PostulaAI.Api.Shared.Errors;

namespace PostulaAI.Api.Modules.ProfessionalProfiles;

/// <summary>The authenticated user's own profile. There is no route to read another user's profile.</summary>
[Route("api/v1/profiles/me")]
[Authorize]
public sealed class ProfilesController(ProfileService profileService, CvImportService cvImport, IValidator<ProfileRequest> validator) : ApiControllerBase
{
    private const int MaxRequestBytes = 512 * 1024;

    [HttpGet]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await profileService.GetAsync(CurrentUserId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ProblemFor(result.Error);
    }

    [HttpPut]
    [RequestSizeLimit(MaxRequestBytes)]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Put(ProfileRequest request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblemFor(validation);
        }

        return Ok(await profileService.UpsertAsync(CurrentUserId, request, cancellationToken));
    }

    /// <summary>
    /// Reads a CV (PDF or DOCX, max 5 MB) and returns an unsaved profile draft for the user to review.
    /// The file is processed in memory and never stored.
    /// </summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(CvTextExtractor.MaxFileBytes + (512 * 1024))]
    [RequestFormLimits(MultipartBodyLengthLimit = CvTextExtractor.MaxFileBytes + (512 * 1024))]
    [EnableRateLimiting(CvImportRateLimitOptions.PolicyName)]
    [ProducesResponseType<CvImportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Import(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return ProblemFor(new AppError(ErrorCodes.CvFileRequired, StatusCodes.Status400BadRequest, "Elige un archivo PDF o Word (.docx)."));
        }

        if (file.Length > CvTextExtractor.MaxFileBytes)
        {
            return ProblemFor(new AppError(ErrorCodes.CvTooLarge, StatusCodes.Status413PayloadTooLarge, "El archivo supera el máximo de 5 MB."));
        }

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, cancellationToken);
        var result = await cvImport.ImportAsync(CurrentUserId, buffer.ToArray(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ProblemFor(result.Error);
    }
}
