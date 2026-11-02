namespace PostulaAI.AI.Clients;

/// <summary>
/// Provider-agnostic contract for calling a large language model.
/// Product logic depends only on this interface so the provider (Claude today) can be replaced
/// without rewriting analysis logic. Implementations live in this folder (ClaudeClient — Phase 3).
/// </summary>
/// <remarks>Preliminary contract (Phase 0). It may be refined in Phase 3 before the first implementation.</remarks>
public interface IAIClient
{
    Task<AICompletionResult> CompleteAsync(AICompletionRequest request, CancellationToken cancellationToken);
}

public sealed record AICompletionRequest(
    string SystemPrompt,
    IReadOnlyList<AIMessage> Messages,
    int MaxOutputTokens);

public sealed record AIMessage(AIMessageRole Role, string Content);

public enum AIMessageRole
{
    User,
    Assistant,
}

public sealed record AICompletionResult(
    string Content,
    string Model,
    int InputTokens,
    int OutputTokens,
    TimeSpan Latency,
    string? StopReason);
