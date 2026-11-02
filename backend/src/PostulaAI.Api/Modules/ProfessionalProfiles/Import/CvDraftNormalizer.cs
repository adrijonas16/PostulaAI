using System.Globalization;
using PostulaAI.AI.Contracts;
using PostulaAI.Api.Modules.ProfessionalProfiles.Contracts;
using PostulaAI.Api.Modules.ProfessionalProfiles.Domain;

namespace PostulaAI.Api.Modules.ProfessionalProfiles.Import;

/// <summary>
/// Converts the model's extraction into a draft that respects the profile rules (lengths, list sizes, http(s) URLs,
/// past dates, no duplicates). Invalid values become empty fields for the user to complete, never guesses.
/// </summary>
public static class CvDraftNormalizer
{
    public static (ProfileDraft Draft, IReadOnlyList<string> Warnings) Normalize(CvExtraction extraction, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(extraction);

        var warnings = new List<string>();
        var truncated = false;

        string Text(string? value, int max)
        {
            var clean = (value ?? string.Empty).Trim();
            if (clean.Length > max)
            {
                truncated = true;
                return clean[..max];
            }

            return clean;
        }

        string? Optional(string? value, int max)
        {
            var clean = Text(value, max);
            return clean.Length == 0 ? null : clean;
        }

        DateOnly? Date(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateOnly.TryParseExact(value.Trim(), new[] { "yyyy-MM-dd", "yyyy-MM" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date > today ? null : date;
            }

            warnings.Add($"No se pudo interpretar la fecha «{value}»; complétala manualmente.");
            return null;
        }

        string? Url(string? value, string label)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var candidate = value.Trim();
            if (candidate.Length <= ProfileLimits.Url
                && Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            {
                return candidate;
            }

            warnings.Add($"Se omitió el enlace de {label} porque no es una URL válida.");
            return null;
        }

        List<string> Tags(IEnumerable<string> values, int maxItems, int maxLength) =>
            [.. values.Select(value => value.Trim())
                .Where(value => value.Length > 0 && value.Length <= maxLength)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(maxItems)];

        List<T> Cap<T>(IEnumerable<T> items, int max, string label)
        {
            var list = items.ToList();
            if (list.Count > max)
            {
                warnings.Add($"Solo se importaron {max} {label}; revisa si falta alguno importante.");
                return list.Take(max).ToList();
            }

            return list;
        }

        var experiences = Cap(extraction.Experiences, ProfileLimits.MaxExperiences, "experiencias").Select(item => new ExperienceDraft(
            Text(item.Company, ProfileLimits.ShortText),
            Text(item.Role, ProfileLimits.ShortText),
            Optional(item.Location, ProfileLimits.ShortText),
            Date(item.StartDate),
            item.IsCurrent ? null : Date(item.EndDate),
            item.IsCurrent,
            Optional(item.Description, ProfileLimits.Description),
            [.. item.Achievements.Select(achievement => Text(achievement, ProfileLimits.ListItem)).Where(achievement => achievement.Length > 0).Take(ProfileLimits.MaxAchievements)],
            Tags(item.Technologies, ProfileLimits.MaxTechnologies, ProfileLimits.Tag))).ToList();

        var education = Cap(extraction.Education, ProfileLimits.MaxEducation, "estudios").Select(item => new EducationDraft(
            Text(item.Institution, ProfileLimits.MediumText),
            Text(item.Degree, ProfileLimits.MediumText),
            Optional(item.FieldOfStudy, ProfileLimits.MediumText),
            Date(item.StartDate),
            Date(item.EndDate),
            item.Status switch
            {
                CvEducationStatus.InProgress => EducationStatus.InProgress,
                CvEducationStatus.Incomplete => EducationStatus.Incomplete,
                _ => EducationStatus.Completed,
            })).ToList();

        var projects = Cap(extraction.Projects, ProfileLimits.MaxProjects, "proyectos").Select(item => new ProjectDto(
            Text(item.Name, ProfileLimits.ShortText),
            Optional(item.Description, ProfileLimits.Summary),
            Tags(item.Technologies, ProfileLimits.MaxTechnologies, ProfileLimits.Tag),
            Url(item.Url, $"el proyecto «{item.Name}»"))).ToList();

        var certifications = Cap(extraction.Certifications, ProfileLimits.MaxCertifications, "certificaciones").Select(item => new CertificationDto(
            Text(item.Name, ProfileLimits.MediumText),
            Text(item.Issuer, ProfileLimits.MediumText),
            Date(item.IssuedOn),
            Url(item.CredentialUrl, $"la certificación «{item.Name}»"))).ToList();

        var languages = extraction.Languages
            .Where(item => !string.IsNullOrWhiteSpace(item.Language))
            .DistinctBy(item => item.Language.Trim(), StringComparer.OrdinalIgnoreCase)
            .Take(ProfileLimits.MaxLanguages)
            .Select(item => new LanguageDraft(
                Text(item.Language, ProfileLimits.Tag),
                item.Proficiency switch
                {
                    CvLanguageLevel.Basic => LanguageProficiency.Basic,
                    CvLanguageLevel.Intermediate => LanguageProficiency.Intermediate,
                    CvLanguageLevel.Advanced => LanguageProficiency.Advanced,
                    CvLanguageLevel.Native => LanguageProficiency.Native,
                    _ => null,
                }))
            .ToList();

        var draft = new ProfileDraft(
            Text(extraction.FullName, ProfileLimits.FullName),
            Optional(extraction.Headline, ProfileLimits.Headline),
            Optional(extraction.Summary, ProfileLimits.Summary),
            new ProfileLinksDto(
                Url(extraction.Links.LinkedInUrl, "LinkedIn"),
                Url(extraction.Links.GitHubUrl, "GitHub"),
                Url(extraction.Links.PortfolioUrl, "portafolio")),
            experiences,
            education,
            projects,
            certifications,
            Tags(extraction.TechnicalSkills, ProfileLimits.MaxTechnicalSkills, ProfileLimits.Tag),
            Tags(extraction.SoftSkills, ProfileLimits.MaxSoftSkills, ProfileLimits.Tag),
            languages);

        if (truncated)
        {
            warnings.Add("Algunos textos eran demasiado largos y se recortaron.");
        }

        if (languages.Any(language => language.Proficiency is null))
        {
            warnings.Add("Algunos idiomas no indican nivel en el CV; elígelo antes de guardar.");
        }

        if (experiences.Any(item => item.StartDate is null))
        {
            warnings.Add("Algunas experiencias no tienen fecha de inicio clara; complétala antes de guardar.");
        }

        return (draft, warnings);
    }
}
