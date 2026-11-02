using PostulaAI.AI.Contracts;
using PostulaAI.AI.Evaluations;
using PostulaAI.Api.Modules.ProfessionalProfiles.Domain;
using PostulaAI.Api.Modules.ProfessionalProfiles.Import;

namespace PostulaAI.UnitTests.CvImport;

public sealed class CvGroundingAndNormalizerTests
{
    private const string CvText = """
        Laura Gómez — Desarrolladora backend
        Empresa Ficticia S.A. (2022 – actual): APIs con C#, ASP.NET Core y PostgreSQL. Uso de Node.js en scripts.
        Habilidades: trabajo en equipo, Docker.
        Idiomas: Inglés B2, portugués básico.
        """;

    private static CvExtraction Extraction() => new(
        "Laura Gómez", null, null, new CvLinks(null, null, null),
        [new CvExperience("Empresa Ficticia S.A.", "Desarrolladora backend", null, "2022-03-01", "2023-01-01", true, null, [], ["C#", "ASP.NET Core", "Kubernetes"])],
        [], [], [],
        ["C#", "Node.js", "Docker", "Terraform", "AWS"],
        ["Trabajo en equipo", "Liderazgo"],
        [new CvLanguage("Inglés", CvLanguageLevel.Intermediate), new CvLanguage("Francés", CvLanguageLevel.Basic)]);

    [Fact]
    public void Grounding_DropsSkillsTechnologiesAndLanguagesAbsentFromTheCv_WithWarnings()
    {
        var (result, warnings) = CvGrounding.Apply(Extraction(), CvText);

        Assert.Equal(new[] { "C#", "Node.js", "Docker" }, result.TechnicalSkills);
        Assert.Equal(new[] { "Trabajo en equipo" }, result.SoftSkills);
        Assert.Equal(new[] { "C#", "ASP.NET Core" }, result.Experiences[0].Technologies);
        Assert.Equal(new[] { "Inglés" }, result.Languages.Select(language => language.Language));
        Assert.Contains(warnings, warning => warning.Contains("«Terraform»", StringComparison.Ordinal));
        Assert.Contains(warnings, warning => warning.Contains("«Kubernetes»", StringComparison.Ordinal));
        Assert.Contains(warnings, warning => warning.Contains("«Francés»", StringComparison.Ordinal));
        Assert.Contains(warnings, warning => warning.Contains("«Liderazgo»", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("postgresql", true)]
    [InlineData("Ingles", true)]
    [InlineData("Java", false)]
    [InlineData("C", false)]
    [InlineData("trabajo  en   equipo", true)]
    public void AppearsIn_MatchesWholeTermsIgnoringCaseAccentsAndSpacing(string term, bool expected) =>
        Assert.Equal(expected, CvGrounding.AppearsIn(term, TechVocabulary.Normalize(CvText)));

    [Fact]
    public void Normalizer_FixesDatesUrlsAndCurrentJobs_WithoutGuessing()
    {
        var extraction = Extraction() with
        {
            Links = new CvLinks("https://www.linkedin.com/in/laura", "github.com/laura", null),
            Experiences =
            [
                new CvExperience("Empresa A", "Cargo A", null, "2022-03", "2023-01-01", true, null, [], []),
                new CvExperience("Empresa B", "Cargo B", null, "marzo 2019", "2099-01-01", false, null, [], []),
            ],
            TechnicalSkills = ["C#", "c#", " ", "Docker"],
            Languages = [new CvLanguage("Inglés", null)],
        };

        var (draft, warnings) = CvDraftNormalizer.Normalize(extraction, new DateOnly(2026, 11, 2));

        Assert.Equal("https://www.linkedin.com/in/laura", draft.Links.LinkedInUrl);
        Assert.Null(draft.Links.GitHubUrl);
        Assert.Equal(new DateOnly(2022, 3, 1), draft.Experiences[0].StartDate);
        Assert.Null(draft.Experiences[0].EndDate);
        Assert.Null(draft.Experiences[1].StartDate);
        Assert.Null(draft.Experiences[1].EndDate);
        Assert.Equal(new[] { "C#", "Docker" }, draft.TechnicalSkills);
        Assert.Null(draft.Languages[0].Proficiency);
        Assert.Contains(warnings, warning => warning.Contains("GitHub", StringComparison.Ordinal));
        Assert.Contains(warnings, warning => warning.Contains("«marzo 2019»", StringComparison.Ordinal));
        Assert.Contains(warnings, warning => warning.Contains("nivel", StringComparison.Ordinal));
        Assert.Contains(warnings, warning => warning.Contains("fecha de inicio", StringComparison.Ordinal));
    }

    [Fact]
    public void Normalizer_MapsEnumsAndCapsLists()
    {
        var extraction = Extraction() with
        {
            Education = [new CvEducation("U", "Maestría", null, null, null, CvEducationStatus.InProgress)],
            Projects = [.. Enumerable.Range(1, ProfileLimits.MaxProjects + 3).Select(i => new CvProject($"P{i}", null, [], null))],
            Languages = [new CvLanguage("Inglés", CvLanguageLevel.Native)],
        };

        var (draft, warnings) = CvDraftNormalizer.Normalize(extraction, new DateOnly(2026, 11, 2));

        Assert.Equal(EducationStatus.InProgress, draft.Education[0].Status);
        Assert.Equal(LanguageProficiency.Native, draft.Languages[0].Proficiency);
        Assert.Equal(ProfileLimits.MaxProjects, draft.Projects.Count);
        Assert.Contains(warnings, warning => warning.Contains("proyectos", StringComparison.Ordinal));
    }
}
