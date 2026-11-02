using System.Text.Json;
using PostulaAI.AI.Clients;
using PostulaAI.AI.Contracts;
using PostulaAI.AI.Prompts;
using PostulaAI.AI.Schemas;

namespace PostulaAI.AI.Services;

public sealed class JobAnalysisOptions
{
    public const string SectionName = "AI:JobAnalysis";

    public string PromptId { get; set; } = "job-analysis";

    public string PromptVersion { get; set; } = "v1";

    public string RepairPromptId { get; set; } = "job-analysis-repair";

    public string RepairPromptVersion { get; set; } = "v1";

    public string SchemaName { get; set; } = OutputSchemas.JobAnalysisV1;

    /// <summary>Includes room for adaptive thinking; the JSON answer itself is usually 2–4k tokens.</summary>
    public int MaxOutputTokens { get; set; } = 16000;
}

/// <summary>
/// Orchestrates one analysis: versioned prompt → provider (structured output) → strict schema parse →
/// business rules + grounding check → at most ONE repair attempt → valid result or explicit failure.
/// Shared by the API and the evaluation runner so both exercise exactly the same pipeline.
/// </summary>
public sealed class JobAnalysisService(IAIClient client, JobAnalysisOptions options)
{
    public static IReadOnlyDictionary<string, string> SupportedLanguages { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["es"] = "Spanish (neutral Latin American Spanish)",
        ["en"] = "English",
    };

    public async Task<JobAnalysisOutcome> AnalyzeAsync(JobAnalysisRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportedLanguages.TryGetValue(request.Language, out var languageName))
        {
            throw new ArgumentException($"Unsupported language '{request.Language}'.", nameof(request));
        }

        var prompt = PromptTemplate.Load(options.PromptId, options.PromptVersion);
        var schema = OutputSchemas.Load(options.SchemaName);
        var variables = BuildVariables(request, languageName);
        var system = prompt.RenderSystem(variables);
        var conversation = new List<AIMessage> { new(AIMessageRole.User, prompt.RenderUser(variables)) };

        var outcome = new JobAnalysisOutcome
        {
            Succeeded = false,
            PromptId = prompt.Id,
            PromptVersion = prompt.Version,
            Model = client.Model,
            Language = request.Language.ToLowerInvariant(),
        };

        AICompletionResult first;
        try
        {
            first = await client.CompleteAsync(new AICompletionRequest(system, conversation, options.MaxOutputTokens, schema), cancellationToken);
        }
        catch (AIProviderException exception)
        {
            return outcome with { ErrorCode = ErrorCodeFor(exception), ProviderCalled = exception.Kind != AIProviderErrorKind.NotConfigured };
        }

        outcome = Accumulate(outcome, first) with { ProviderCalled = true };
        var (result, issues) = Evaluate(first, request.Profile);
        if (result is not null && issues.Count == 0)
        {
            return Success(outcome, result);
        }

        if (first.StopReason == "refusal")
        {
            return outcome with { ErrorCode = AnalysisErrorCodes.Refused, ValidationIssues = issues };
        }

        // One controlled repair attempt with the concrete validation errors.
        var repairPrompt = PromptTemplate.Load(options.RepairPromptId, options.RepairPromptVersion);
        conversation.Add(new AIMessage(AIMessageRole.Assistant, string.IsNullOrWhiteSpace(first.Content) ? "{}" : first.Content));
        conversation.Add(new AIMessage(AIMessageRole.User, repairPrompt.RenderUser(new Dictionary<string, string>
        {
            ["validation_errors"] = string.Join('\n', issues.Select(issue => $"- {issue}")),
            ["language_name"] = languageName,
        })));

        AICompletionResult second;
        try
        {
            second = await client.CompleteAsync(new AICompletionRequest(system, conversation, options.MaxOutputTokens, schema), cancellationToken);
        }
        catch (AIProviderException exception)
        {
            return outcome with { RepairAttempted = true, ErrorCode = ErrorCodeFor(exception), ValidationIssues = issues };
        }

        outcome = Accumulate(outcome, second) with { RepairAttempted = true };
        (result, issues) = Evaluate(second, request.Profile);
        return result is not null && issues.Count == 0
            ? Success(outcome, result)
            : outcome with { ErrorCode = AnalysisErrorCodes.InvalidOutput, ValidationIssues = issues };
    }

    internal static Dictionary<string, string> BuildVariables(JobAnalysisRequest request, string languageName) => new()
    {
        ["language_name"] = languageName,
        ["profile_json"] = JsonSerializer.Serialize(request.Profile, JobAnalysisJson.PromptOptions),
        ["job_title"] = Sanitize(request.JobPosting.Title),
        ["job_company"] = Sanitize(request.JobPosting.Company ?? "Not specified"),
        ["job_location"] = Sanitize(request.JobPosting.Location ?? "Not specified"),
        ["job_work_mode"] = Sanitize(request.JobPosting.WorkMode),
        ["job_description"] = Sanitize(request.JobPosting.Description),
    };

    /// <summary>Prevents posting text from closing the delimiters that separate data from instructions.</summary>
    internal static string Sanitize(string value) => value
        .Replace("</job_posting>", "</job posting>", StringComparison.OrdinalIgnoreCase)
        .Replace("<job_posting", "<job posting", StringComparison.OrdinalIgnoreCase)
        .Replace("</candidate_profile>", "</candidate profile>", StringComparison.OrdinalIgnoreCase);

    private static (JobAnalysisResult? Result, IReadOnlyList<string> Issues) Evaluate(AICompletionResult completion, CandidateProfile profile)
    {
        if (completion.StopReason == "max_tokens")
        {
            return (null, new[] { "The response was cut off before the JSON object was complete. Be more concise." });
        }

        if (!JobAnalysisJson.TryParse(completion.Content, out var result, out var error) || result is null)
        {
            return (null, new[] { error ?? "The response is not a valid JSON object." });
        }

        return (result, JobAnalysisRules.Validate(result, profile));
    }

    private static JobAnalysisOutcome Accumulate(JobAnalysisOutcome outcome, AICompletionResult completion) => outcome with
    {
        Model = string.IsNullOrEmpty(completion.Model) ? outcome.Model : completion.Model,
        InputTokens = outcome.InputTokens + completion.InputTokens,
        OutputTokens = outcome.OutputTokens + completion.OutputTokens,
        Latency = outcome.Latency + completion.Latency,
    };

    // The stored JSON is re-serialized from the validated object, so it is canonical and contains nothing unvalidated.
    private static JobAnalysisOutcome Success(JobAnalysisOutcome outcome, JobAnalysisResult result) => outcome with
    {
        Succeeded = true,
        Result = result,
        ResultJson = JobAnalysisJson.Serialize(result),
        ErrorCode = null,
        ValidationIssues = [],
    };

    private static string ErrorCodeFor(AIProviderException exception) => exception.Kind switch
    {
        AIProviderErrorKind.NotConfigured => AnalysisErrorCodes.NotConfigured,
        AIProviderErrorKind.RateLimited or AIProviderErrorKind.Overloaded => AnalysisErrorCodes.RateLimited,
        AIProviderErrorKind.InvalidRequest => AnalysisErrorCodes.RequestRejected,
        AIProviderErrorKind.Authentication => AnalysisErrorCodes.NotConfigured,
        _ => AnalysisErrorCodes.Unavailable,
    };
}
