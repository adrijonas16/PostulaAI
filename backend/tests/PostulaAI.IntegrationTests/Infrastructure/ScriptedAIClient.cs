using System.Collections.Concurrent;
using PostulaAI.AI.Clients;
using PostulaAI.AI.Contracts;
using PostulaAI.AI.Services;

namespace PostulaAI.IntegrationTests.Infrastructure;

/// <summary>
/// Replaces the Claude client in integration tests. Returns queued responses/errors, or a valid analysis when the
/// queue is empty. Tests in the database collection run sequentially, so a shared instance is safe.
/// </summary>
public sealed class ScriptedAIClient : IAIClient
{
    private readonly ConcurrentQueue<Func<AICompletionResult>> queue = new();

    public string Model => "claude-integration-fake";

    public int Calls { get; private set; }

    public static JobAnalysisResult ValidResult() => new(
        78,
        "Cumples los requisitos principales de backend .NET; falta experiencia con Azure, que es deseable.",
        [
            new AnalysisStrength("C# y ASP.NET Core", "Desarrolladora backend en Empresa Ficticia S.A. con C# y ASP.NET Core.", RequirementImportance.Required),
            new AnalysisStrength("PostgreSQL", "Usa PostgreSQL en Empresa Ficticia S.A.", RequirementImportance.Required),
        ],
        [new AnalysisGap("Azure", GapType.MissingSkill, RequirementImportance.Preferred, "Considera una certificación introductoria de Azure.")],
        ["C#", "ASP.NET Core", "PostgreSQL"],
        ["Cuantifica el impacto de tus APIs de pagos."],
        "Desarrolladora backend con experiencia en C#, ASP.NET Core y PostgreSQL.",
        "Estimado equipo: me interesa la vacante...",
        "Este análisis es una estimación automática y no garantiza contratación.");

    public void Enqueue(JobAnalysisResult result) => Enqueue(JobAnalysisJson.Serialize(result));

    public void Enqueue(string content) =>
        queue.Enqueue(() => new AICompletionResult(content, Model, 1200, 800, TimeSpan.FromMilliseconds(1500), "end_turn"));

    public void EnqueueError(AIProviderErrorKind kind) =>
        queue.Enqueue(() => throw new AIProviderException(kind, $"scripted {kind}"));

    public void Reset() => queue.Clear();

    public Task<AICompletionResult> CompleteAsync(AICompletionRequest request, CancellationToken cancellationToken)
    {
        Calls++;
        var next = queue.TryDequeue(out var scripted)
            ? scripted
            : () => new AICompletionResult(JobAnalysisJson.Serialize(ValidResult()), Model, 1200, 800, TimeSpan.FromMilliseconds(1500), "end_turn");
        return Task.FromResult(next());
    }
}
