using FluentValidation;
using PostulaAI.AI.Services;

namespace PostulaAI.Api.Modules.Analyses.Contracts;

public sealed class CreateAnalysisRequestValidator : AbstractValidator<CreateAnalysisRequest>
{
    public CreateAnalysisRequestValidator()
    {
        RuleFor(request => request.JobPostingId).NotEmpty().WithMessage("Elige una vacante.");
        RuleFor(request => request.Language)
            .Must(language => language is null || JobAnalysisService.SupportedLanguages.ContainsKey(language))
            .WithMessage("Idioma no soportado. Usa \"es\" o \"en\".");
    }
}
