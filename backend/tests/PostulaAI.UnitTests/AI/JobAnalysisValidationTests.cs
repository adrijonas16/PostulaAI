using PostulaAI.AI.Contracts;
using PostulaAI.AI.Evaluations;
using PostulaAI.AI.Services;

namespace PostulaAI.UnitTests.AI;

public sealed class JobAnalysisValidationTests
{
    [Fact]
    public void StrictParser_AcceptsAValidResult()
    {
        Assert.True(JobAnalysisJson.TryParse(AIFixtures.ValidJson(), out var result, out var error), error);
        Assert.Equal(72, result?.CompatibilityScore);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("[]")]
    public void StrictParser_RejectsNonObjects(string json) =>
        Assert.False(JobAnalysisJson.TryParse(json, out _, out _));

    [Fact]
    public void StrictParser_RejectsUnknownProperties() =>
        Assert.False(JobAnalysisJson.TryParse(AIFixtures.ValidJson().Replace("\"summary\"", "\"extra\":1,\"summary\"", StringComparison.Ordinal), out _, out _));

    [Fact]
    public void StrictParser_RejectsMissingProperties() =>
        Assert.False(JobAnalysisJson.TryParse(AIFixtures.ValidJson().Replace("\"disclaimer\"", "\"notDisclaimer\"", StringComparison.Ordinal), out _, out _));

    [Fact]
    public void StrictParser_RejectsNulls() =>
        Assert.False(JobAnalysisJson.TryParse(System.Text.RegularExpressions.Regex.Replace(AIFixtures.ValidJson(), "\"summary\":\"[^\"]*\"", "\"summary\":null"), out _, out _));

    [Fact]
    public void StrictParser_RejectsUnknownEnumValues() =>
        Assert.False(JobAnalysisJson.TryParse(AIFixtures.ValidJson().Replace("\"missing_skill\"", "\"made_up\"", StringComparison.Ordinal), out _, out _));

    [Fact]
    public void Rules_AcceptAValidResult() =>
        Assert.Empty(JobAnalysisRules.Validate(AIFixtures.ValidResult(), AIFixtures.Profile()));

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Rules_RejectScoresOutOfRange(int score) =>
        Assert.Contains(JobAnalysisRules.Validate(AIFixtures.ValidResult() with { CompatibilityScore = score }, AIFixtures.Profile()),
            issue => issue.StartsWith("compatibilityScore", StringComparison.Ordinal));

    [Fact]
    public void Rules_RejectEmptyTextsAndEmptyAnalyses()
    {
        var issues = JobAnalysisRules.Validate(
            AIFixtures.ValidResult() with { Summary = " ", Disclaimer = "", Strengths = [], Gaps = [], CvRecommendations = [] },
            AIFixtures.Profile());

        Assert.Contains(issues, issue => issue.StartsWith("summary", StringComparison.Ordinal));
        Assert.Contains(issues, issue => issue.StartsWith("disclaimer", StringComparison.Ordinal));
        Assert.Contains(issues, issue => issue.Contains("strengths or gaps", StringComparison.Ordinal));
        Assert.Contains(issues, issue => issue.StartsWith("cvRecommendations", StringComparison.Ordinal));
    }

    [Fact]
    public void Rules_RejectDuplicateKeywords() =>
        Assert.Contains(JobAnalysisRules.Validate(AIFixtures.ValidResult() with { KeywordsToInclude = ["C#", "c#"] }, AIFixtures.Profile()),
            issue => issue.Contains("duplicates", StringComparison.Ordinal));

    [Fact]
    public void Grounding_FlagsInventedTechnologyInEvidenceSummaryAndKeywords()
    {
        var result = AIFixtures.ValidResult() with
        {
            Strengths = [new AnalysisStrength("Orquestación", "Experiencia con Kubernetes en producción.", RequirementImportance.Preferred)],
            AdaptedProfessionalSummary = "Desarrolladora con experiencia en C# y AWS.",
            KeywordsToInclude = ["C#", "Terraform"],
        };

        var claims = GroundingChecker.FindUnsupportedClaims(result, AIFixtures.Profile());

        Assert.Contains(new GroundingChecker.UnsupportedClaim("strengths[0].evidence", "Kubernetes"), claims);
        Assert.Contains(new GroundingChecker.UnsupportedClaim("adaptedProfessionalSummary", "AWS"), claims);
        Assert.Contains(new GroundingChecker.UnsupportedClaim("keywordsToInclude[1]", "Terraform"), claims);
        Assert.Equal(3, claims.Count);
    }

    [Fact]
    public void Grounding_FlagsSimilarButDifferentTechnologies()
    {
        var result = AIFixtures.ValidResult() with
        {
            Strengths = [new AnalysisStrength("Base de datos", "Experiencia con Oracle y MySQL.", RequirementImportance.Required)],
        };

        var terms = GroundingChecker.FindUnsupportedClaims(result, AIFixtures.Profile()).Select(claim => claim.Term).ToList();

        Assert.Contains("Oracle", terms);
        Assert.Contains("MySQL", terms);
    }

    [Theory]
    [InlineData("Consultas SQL sobre PostgreSQL")]
    [InlineData("Nivel de inglés intermedio")]
    [InlineData("Backend en .NET con C#")]
    [InlineData("Frontend con JavaScript en React")]
    public void Grounding_AcceptsTermsImpliedByTheProfile(string evidence) =>
        Assert.Empty(GroundingChecker.FindUnsupportedClaims(
            AIFixtures.ValidResult() with { Strengths = [new AnalysisStrength("x", evidence, RequirementImportance.Required)] },
            AIFixtures.Profile()));

    [Theory]
    [InlineData("javascript", "Java", false)]
    [InlineData("c++ moderno", "C++", true)]
    [InlineData("c++ moderno", "C#", false)]
    [InlineData("asp.net core", ".NET", false)]
    [InlineData("github y gitlab", "Git", false)]
    [InlineData("un equipo excelente", "Excel", false)]
    [InlineData("go to market", "Go", false)]
    [InlineData("experiencia en k8s", "Kubernetes", true)]
    public void Vocabulary_DetectsWholeTermsOnly(string text, string term, bool expected) =>
        Assert.Equal(expected, TechVocabulary.Detect(TechVocabulary.Normalize(text)).Any(found => found.Name == term));
}
