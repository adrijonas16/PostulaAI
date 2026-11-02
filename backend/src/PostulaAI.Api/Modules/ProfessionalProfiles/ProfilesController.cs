using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostulaAI.Api.Modules.ProfessionalProfiles.Contracts;
using PostulaAI.Api.Shared.Api;

namespace PostulaAI.Api.Modules.ProfessionalProfiles;

/// <summary>The authenticated user's own profile. There is no route to read another user's profile.</summary>
[Route("api/v1/profiles/me")]
[Authorize]
public sealed class ProfilesController(ProfileService profileService, IValidator<ProfileRequest> validator) : ApiControllerBase
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
}
