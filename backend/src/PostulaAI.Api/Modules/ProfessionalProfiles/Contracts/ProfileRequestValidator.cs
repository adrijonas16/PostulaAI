using System.Linq.Expressions;
using FluentValidation;
using PostulaAI.Api.Modules.ProfessionalProfiles.Domain;

using PostulaAI.Api.Shared.Persistence;

namespace PostulaAI.Api.Modules.ProfessionalProfiles.Contracts;

public sealed class ProfileRequestValidator : AbstractValidator<ProfileRequest>
{
    public ProfileRequestValidator(TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        var today = DateOnly.FromDateTime(time.GetUtcNowForStorage().UtcDateTime);

        RuleFor(request => request.FullName)
            .Must(NotBlank).WithMessage("Ingresa tu nombre profesional.")
            .MaximumLength(ProfileLimits.FullName).WithMessage(TooLong(ProfileLimits.FullName));
        RuleFor(request => request.Headline).MaximumLength(ProfileLimits.Headline).WithMessage(TooLong(ProfileLimits.Headline));
        RuleFor(request => request.Summary).MaximumLength(ProfileLimits.Summary).WithMessage(TooLong(ProfileLimits.Summary));

        RuleFor(request => request.Links.LinkedInUrl).Must(BeOptionalHttpUrl).WithMessage(InvalidUrl).OverridePropertyName("links.linkedInUrl");
        RuleFor(request => request.Links.GitHubUrl).Must(BeOptionalHttpUrl).WithMessage(InvalidUrl).OverridePropertyName("links.gitHubUrl");
        RuleFor(request => request.Links.PortfolioUrl).Must(BeOptionalHttpUrl).WithMessage(InvalidUrl).OverridePropertyName("links.portfolioUrl");

        RuleFor(request => request.Experiences.Count).LessThanOrEqualTo(ProfileLimits.MaxExperiences)
            .WithMessage($"Puedes registrar hasta {ProfileLimits.MaxExperiences} experiencias.").OverridePropertyName("experiences");
        RuleForEach(request => request.Experiences).ChildRules(experience =>
        {
            experience.RuleFor(item => item.Company).Must(NotBlank).WithMessage("Ingresa la empresa.")
                .MaximumLength(ProfileLimits.ShortText).WithMessage(TooLong(ProfileLimits.ShortText));
            experience.RuleFor(item => item.Role).Must(NotBlank).WithMessage("Ingresa el cargo.")
                .MaximumLength(ProfileLimits.ShortText).WithMessage(TooLong(ProfileLimits.ShortText));
            experience.RuleFor(item => item.Location).MaximumLength(ProfileLimits.ShortText).WithMessage(TooLong(ProfileLimits.ShortText));
            experience.RuleFor(item => item.StartDate).Must(start => start <= today).WithMessage("La fecha de inicio no puede estar en el futuro.");
            experience.RuleFor(item => item.EndDate).Null().When(item => item.IsCurrent)
                .WithMessage("Si es tu empleo actual, no indiques fecha de fin.");
            experience.RuleFor(item => item.EndDate).NotNull().When(item => !item.IsCurrent)
                .WithMessage("Indica la fecha de fin o marca que es tu empleo actual.");
            experience.RuleFor(item => item.EndDate).Must((item, end) => end is null || end >= item.StartDate)
                .WithMessage("La fecha de fin debe ser posterior a la de inicio.");
            experience.RuleFor(item => item.Description).MaximumLength(ProfileLimits.Description).WithMessage(TooLong(ProfileLimits.Description));
            experience.RuleFor(item => item.Achievements.Count).LessThanOrEqualTo(ProfileLimits.MaxAchievements)
                .WithMessage($"Puedes registrar hasta {ProfileLimits.MaxAchievements} logros por experiencia.").OverridePropertyName("achievements");
            experience.RuleForEach(item => item.Achievements).Must(NotBlank).WithMessage("El logro no puede estar vacío.")
                .MaximumLength(ProfileLimits.ListItem).WithMessage(TooLong(ProfileLimits.ListItem));
            AddTagRules(experience, item => item.Technologies, ProfileLimits.MaxTechnologies);
        });

        RuleFor(request => request.Education.Count).LessThanOrEqualTo(ProfileLimits.MaxEducation)
            .WithMessage($"Puedes registrar hasta {ProfileLimits.MaxEducation} estudios.").OverridePropertyName("education");
        RuleForEach(request => request.Education).ChildRules(education =>
        {
            education.RuleFor(item => item.Institution).Must(NotBlank).WithMessage("Ingresa la institución.")
                .MaximumLength(ProfileLimits.MediumText).WithMessage(TooLong(ProfileLimits.MediumText));
            education.RuleFor(item => item.Degree).Must(NotBlank).WithMessage("Ingresa el título o programa.")
                .MaximumLength(ProfileLimits.MediumText).WithMessage(TooLong(ProfileLimits.MediumText));
            education.RuleFor(item => item.FieldOfStudy).MaximumLength(ProfileLimits.MediumText).WithMessage(TooLong(ProfileLimits.MediumText));
            education.RuleFor(item => item.Status).IsInEnum().WithMessage("Estado de estudios no válido.");
            education.RuleFor(item => item.EndDate).Must((item, end) => end is null || item.StartDate is null || end >= item.StartDate)
                .WithMessage("La fecha de fin debe ser posterior a la de inicio.");
        });

        RuleFor(request => request.Projects.Count).LessThanOrEqualTo(ProfileLimits.MaxProjects)
            .WithMessage($"Puedes registrar hasta {ProfileLimits.MaxProjects} proyectos.").OverridePropertyName("projects");
        RuleForEach(request => request.Projects).ChildRules(project =>
        {
            project.RuleFor(item => item.Name).Must(NotBlank).WithMessage("Ingresa el nombre del proyecto.")
                .MaximumLength(ProfileLimits.ShortText).WithMessage(TooLong(ProfileLimits.ShortText));
            project.RuleFor(item => item.Description).MaximumLength(ProfileLimits.Summary).WithMessage(TooLong(ProfileLimits.Summary));
            project.RuleFor(item => item.Url).Must(BeOptionalHttpUrl).WithMessage(InvalidUrl);
            AddTagRules(project, item => item.Technologies, ProfileLimits.MaxTechnologies);
        });

        RuleFor(request => request.Certifications.Count).LessThanOrEqualTo(ProfileLimits.MaxCertifications)
            .WithMessage($"Puedes registrar hasta {ProfileLimits.MaxCertifications} certificaciones.").OverridePropertyName("certifications");
        RuleForEach(request => request.Certifications).ChildRules(certification =>
        {
            certification.RuleFor(item => item.Name).Must(NotBlank).WithMessage("Ingresa el nombre de la certificación.")
                .MaximumLength(ProfileLimits.MediumText).WithMessage(TooLong(ProfileLimits.MediumText));
            certification.RuleFor(item => item.Issuer).Must(NotBlank).WithMessage("Ingresa la entidad emisora.")
                .MaximumLength(ProfileLimits.MediumText).WithMessage(TooLong(ProfileLimits.MediumText));
            certification.RuleFor(item => item.IssuedOn).Must(issuedOn => issuedOn is null || issuedOn <= today)
                .WithMessage("La fecha no puede estar en el futuro.");
            certification.RuleFor(item => item.CredentialUrl).Must(BeOptionalHttpUrl).WithMessage(InvalidUrl);
        });

        AddTagRules(this, request => request.TechnicalSkills, ProfileLimits.MaxTechnicalSkills);
        AddTagRules(this, request => request.SoftSkills, ProfileLimits.MaxSoftSkills);

        RuleFor(request => request.Languages.Count).LessThanOrEqualTo(ProfileLimits.MaxLanguages)
            .WithMessage($"Puedes registrar hasta {ProfileLimits.MaxLanguages} idiomas.").OverridePropertyName("languages");
        RuleFor(request => request.Languages).Must(HaveNoDuplicates<LanguageDto>(language => language.Language))
            .WithMessage("Hay idiomas repetidos.");
        RuleForEach(request => request.Languages).ChildRules(language =>
        {
            language.RuleFor(item => item.Language).Must(NotBlank).WithMessage("Ingresa el idioma.")
                .MaximumLength(ProfileLimits.Tag).WithMessage(TooLong(ProfileLimits.Tag));
            language.RuleFor(item => item.Proficiency).IsInEnum().WithMessage("Nivel de idioma no válido.");
        });
    }

    private const string InvalidUrl = "Ingresa una URL válida que empiece por http:// o https://.";

    private static string TooLong(int max) => $"No puede superar {max} caracteres.";

    private static bool NotBlank(string? value) => !string.IsNullOrWhiteSpace(value);

    private static bool BeOptionalHttpUrl(string? value) =>
        string.IsNullOrEmpty(value)
        || (value.Length <= ProfileLimits.Url
            && Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp));

    private static Func<IEnumerable<T>, bool> HaveNoDuplicates<T>(Func<T, string> key) =>
        items =>
        {
            var keys = items.Select(key).Select(value => value.Trim()).ToList();
            return keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() == keys.Count;
        };

    private static void AddTagRules<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, IEnumerable<string>>> selector,
        int maxItems)
    {
        validator.RuleFor(selector)
            .Must(items => items.Count() <= maxItems).WithMessage($"Puedes registrar hasta {maxItems} elementos.")
            .Must(HaveNoDuplicates<string>(item => item)).WithMessage("Hay elementos repetidos.");
        validator.RuleForEach(selector)
            .Must(NotBlank).WithMessage("No puede haber elementos vacíos.")
            .MaximumLength(ProfileLimits.Tag).WithMessage(TooLong(ProfileLimits.Tag));
    }
}
