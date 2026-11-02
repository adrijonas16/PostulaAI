namespace PostulaAI.AI.Clients;

/// <summary>
/// Provider-agnostic contract for calling a large language model.
/// Product logic depends only on this interface so the provider (Claude today) can be replaced
/// without rewriting analysis logic.
/// </summary>
public interface IAIClient
{
    /// <summary>Model identifier that will be used, for traceability.</summary>
    string Model { get; }

    /// <exception cref="AIProviderException">The provider could not produce a response.</exception>
    Task<AICompletionResult> CompleteAsync(AICompletionRequest request, CancellationToken cancellationToken);
}

/// <param name="OutputJsonSchema">When set, the provider must return JSON that matches this JSON Schema.</param>
public sealed record AICompletionRequest(
    string SystemPrompt,
    IReadOnlyList<AIMessage> Messages,
    int MaxOutputTokens,
    string? OutputJsonSchema = null);

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

public enum AIProviderErrorKind
{
    NotConfigured,
    Authentication,
    RateLimited,
    Overloaded,
    Unavailable,
    InvalidRequest,
}

/// <summary>Provider failure. The message never contains prompts, responses or credentials.</summary>
public sealed class AIProviderException : Exception
{
    public AIProviderException(AIProviderErrorKind kind, string message, int? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
        StatusCode = statusCode;
    }

    public AIProviderException()
        : this(AIProviderErrorKind.Unavailable, "The AI provider failed.")
    {
    }

    public AIProviderException(string message)
        : this(AIProviderErrorKind.Unavailable, message)
    {
    }

    public AIProviderException(string message, Exception innerException)
        : this(AIProviderErrorKind.Unavailable, message, null, innerException)
    {
    }

    public AIProviderErrorKind Kind { get; }

    public int? StatusCode { get; }
}
