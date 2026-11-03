using PostulaAI.AI.Clients;
using PostulaAI.AI.Contracts;
using PostulaAI.AI.Services;

namespace PostulaAI.UnitTests.AI;

internal static class AIFixtures
{
    public static CandidateProfile Profile() => new(
        FullName: "Laura Gómez",
        Headline: "Desarrolladora backend",
        Summary: "Construyo APIs REST.",
        Experiences:
        [
            new CandidateExperience("Empresa Ficticia", "Desarrolladora backend", "Bogotá", "2022-03-01", null, true,
                "APIs de pagos con ASP.NET Core.", ["Reduje la latencia un 30 %"], ["C#", "ASP.NET Core", "PostgreSQL", "Docker"]),
        ],
        Education: [new CandidateEducation("Universidad Ejemplo", "Ingeniería de Sistemas", null, "2015-02-01", "2020-12-01", "completed")],
        Projects: [new CandidateProject("Gestor de turnos", "Proyecto personal", ["React", "TypeScript"], null)],
        Certifications: [],
        TechnicalSkills: ["C#", "SQL", "Git"],
        SoftSkills: ["Comunicación"],
        Languages: [new CandidateLanguage("Inglés", "intermediate")]);

    public static JobPostingInput Posting() => new(
        "Backend .NET", "Empresa X", "Remoto", "remote",
        "Requisitos: C#, ASP.NET Core, PostgreSQL. Deseable: Kubernetes y AWS.");

    public static JobAnalysisRequest Request(string language = "es") => new(Profile(), Posting(), language);

    public static JobAnalysisResult ValidResult() => new(
        CompatibilityScore: 72,
        Summary: "Cumples los requisitos principales de backend; faltan Kubernetes y AWS, que son deseables.",
        Strengths:
        [
            new AnalysisStrength("Experiencia con C# y ASP.NET Core", "Desarrolladora backend en Empresa Ficticia usando C# y ASP.NET Core.", RequirementImportance.Required),
            new AnalysisStrength("PostgreSQL", "Uso de PostgreSQL en Empresa Ficticia.", RequirementImportance.Required),
        ],
        Gaps:
        [
            new AnalysisGap("Kubernetes", GapType.MissingSkill, RequirementImportance.Preferred, "Menciona tu experiencia real con Docker como base y considera un curso de Kubernetes."),
        ],
        KeywordsToInclude: ["C#", "ASP.NET Core", "PostgreSQL", "APIs REST"],
        CvRecommendations: ["Cuantifica el impacto de las APIs de pagos."],
        AdaptedProfessionalSummary: "Desarrolladora backend con experiencia en C#, ASP.NET Core y PostgreSQL construyendo APIs de pagos.",
        CoverLetterDraft: "Estimado equipo de Empresa X: ...",
        Disclaimer: "Este análisis es una estimación automática y no garantiza contratación.");

    public static string ValidJson() => JobAnalysisJson.Serialize(ValidResult());

    public static AICompletionResult Completion(string content, string? stopReason = "end_turn") =>
        new(content, "claude-test", 1000, 500, TimeSpan.FromSeconds(2), stopReason);
}

/// <summary>Scripted IAIClient: returns queued results or throws queued exceptions, and records every request.</summary>
internal sealed class FakeAIClient : IAIClient
{
    private readonly Queue<Func<AICompletionResult>> responses = new();

    public List<AICompletionRequest> Requests { get; } = [];

    public string Model => "claude-test";

    public bool IsConfigured => true;

    public FakeAIClient Returns(AICompletionResult result)
    {
        responses.Enqueue(() => result);
        return this;
    }

    public FakeAIClient Throws(AIProviderErrorKind kind)
    {
        responses.Enqueue(() => throw new AIProviderException(kind, $"fake {kind}"));
        return this;
    }

    public Task<AICompletionResult> CompleteAsync(AICompletionRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(responses.Dequeue()());
    }
}
