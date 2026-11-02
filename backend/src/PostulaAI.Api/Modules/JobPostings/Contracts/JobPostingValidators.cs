using FluentValidation;
using PostulaAI.Api.Modules.JobPostings.Domain;

namespace PostulaAI.Api.Modules.JobPostings.Contracts;

public sealed class JobPostingRequestValidator : AbstractValidator<JobPostingRequest>
{
    public JobPostingRequestValidator()
    {
        RuleFor(request => request.Title)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Ingresa el título de la vacante.")
            .MaximumLength(JobPostingLimits.Title).WithMessage($"No puede superar {JobPostingLimits.Title} caracteres.");
        RuleFor(request => request.Company)
            .MaximumLength(JobPostingLimits.Company).WithMessage($"No puede superar {JobPostingLimits.Company} caracteres.");
        RuleFor(request => request.Location)
            .MaximumLength(JobPostingLimits.Location).WithMessage($"No puede superar {JobPostingLimits.Location} caracteres.");
        RuleFor(request => request.WorkMode).IsInEnum().WithMessage("Modalidad no válida.");
        RuleFor(request => request.Description)
            .Must(value => value is not null && value.Trim().Length >= JobPostingLimits.DescriptionMin)
            .WithMessage($"Pega la descripción completa de la vacante (al menos {JobPostingLimits.DescriptionMin} caracteres).")
            .MaximumLength(JobPostingLimits.DescriptionMax)
            .WithMessage($"La descripción no puede superar {JobPostingLimits.DescriptionMax} caracteres.");
        RuleFor(request => request.SourceUrl)
            .Must(BeOptionalHttpUrl).WithMessage("Ingresa una URL válida que empiece por http:// o https://.");
    }

    private static bool BeOptionalHttpUrl(string? value) =>
        string.IsNullOrEmpty(value)
        || (value.Length <= JobPostingLimits.Url
            && Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp));
}

public sealed class JobPostingListQueryValidator : AbstractValidator<JobPostingListQuery>
{
    public JobPostingListQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");
        RuleFor(query => query.PageSize).InclusiveBetween(1, JobPostingLimits.MaxPageSize)
            .WithMessage($"El tamaño de página debe estar entre 1 y {JobPostingLimits.MaxPageSize}.");
        RuleFor(query => query.Search).MaximumLength(JobPostingLimits.Search)
            .WithMessage($"La búsqueda no puede superar {JobPostingLimits.Search} caracteres.");
    }
}
