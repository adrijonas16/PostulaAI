using PostulaAI.AI.Clients;
using PostulaAI.AI.Contracts;
using PostulaAI.AI.Evaluations;
using PostulaAI.AI.Prompts;
using PostulaAI.AI.Schemas;

namespace PostulaAI.AI.Services;

public sealed class CvExtractionOptions
{
    public const string SectionName = "AI:CvExtraction";

    public string PromptId { get; set; } = "cv-extraction";

    public string PromptVersion { get; set; } = "v1";

    public string RepairPromptId { get; set; } = "cv-extraction-repair";

    public string RepairPromptVersion { get; set; } = "v1";

    public string SchemaName { get; set; } = OutputSchemas.CvExtractionV1;

    public int MaxOutputTokens { get; set; } = 16000;

    /// <summary>CVs longer than this are truncated (with a warning) to bound cost and latency.</summary>
    public int MaxCvCharacters { get; set; } = 30_000;
}

/// <summary>
/// Turns CV text into profile fields: versioned prompt → provider (structured output) → strict parse → at most one
/// repair → grounding (drops skills/technologies/languages absent from the CV). Nothing is saved here.
/// </summary>
public sealed class CvExtractionService(IAIClient client, CvExtractionOptions options)
{
    public async Task<CvExtractionOutcome> ExtractAsync(string cvText, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cvText);

        var warnings = new List<string>();
        var text = cvText.Trim();
        if (text.Length > options.MaxCvCharacters)
        {
            text = text[..options.MaxCvCharacters];
            warnings.Add("El CV es muy largo; solo se leyó la primera parte. Revisa que no falte información.");
        }

        var prompt = PromptTemplate.Load(options.PromptId, options.PromptVersion);
        var schema = OutputSchemas.Load(options.SchemaName);
        var variables = new Dictionary<string, string>
        {
            ["cv_text"] = text.Replace("</cv_text>", "</cv text>", StringComparison.OrdinalIgnoreCase),
        };
        var system = prompt.RenderSystem(variables);
        var conversation = new List<AIMessage> { new(AIMessageRole.User, prompt.RenderUser(variables)) };

        var outcome = new CvExtractionOutcome
        {
            Succeeded = false,
            PromptId = prompt.Id,
            PromptVersion = prompt.Version,
            Model = client.Model,
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
        var (extraction, issues) = Parse(first);
        if (extraction is null)
        {
            var repairPrompt = PromptTemplate.Load(options.RepairPromptId, options.RepairPromptVersion);
            conversation.Add(new AIMessage(AIMessageRole.Assistant, string.IsNullOrWhiteSpace(first.Content) ? "{}" : first.Content));
            conversation.Add(new AIMessage(AIMessageRole.User, repairPrompt.RenderUser(new Dictionary<string, string>
            {
                ["validation_errors"] = string.Join('\n', issues.Select(issue => $"- {issue}")),
            })));

            AICompletionResult second;
            try
            {
                second = await client.CompleteAsync(new AICompletionRequest(system, conversation, options.MaxOutputTokens, schema), cancellationToken);
            }
            catch (AIProviderException exception)
            {
                return outcome with { RepairAttempted = true, ErrorCode = ErrorCodeFor(exception) };
            }

            outcome = Accumulate(outcome, second) with { RepairAttempted = true };
            (extraction, _) = Parse(second);
            if (extraction is null)
            {
                return outcome with { ErrorCode = AnalysisErrorCodes.InvalidOutput };
            }
        }

        var (grounded, groundingWarnings) = CvGrounding.Apply(extraction, text);
        warnings.AddRange(groundingWarnings);
        return outcome with { Succeeded = true, Extraction = grounded, Warnings = warnings };
    }

    private static (CvExtraction? Extraction, IReadOnlyList<string> Issues) Parse(AICompletionResult completion)
    {
        if (completion.StopReason == "max_tokens")
        {
            return (null, new[] { "The response was cut off before the JSON object was complete." });
        }

        try
        {
            var extraction = System.Text.Json.JsonSerializer.Deserialize<CvExtraction>(completion.Content, JobAnalysisJson.Options);
            if (extraction is null)
            {
                return (null, new[] { "The response is JSON null instead of an object." });
            }

            return (extraction, Array.Empty<string>());
        }
        catch (System.Text.Json.JsonException exception)
        {
            return (null, new[] { $"The response does not match the schema: {exception.Message}" });
        }
    }

    private static CvExtractionOutcome Accumulate(CvExtractionOutcome outcome, AICompletionResult completion) => outcome with
    {
        Model = string.IsNullOrEmpty(completion.Model) ? outcome.Model : completion.Model,
        InputTokens = outcome.InputTokens + completion.InputTokens,
        OutputTokens = outcome.OutputTokens + completion.OutputTokens,
        Latency = outcome.Latency + completion.Latency,
    };

    private static string ErrorCodeFor(AIProviderException exception) => exception.Kind switch
    {
        AIProviderErrorKind.NotConfigured or AIProviderErrorKind.Authentication => AnalysisErrorCodes.NotConfigured,
        AIProviderErrorKind.RateLimited or AIProviderErrorKind.Overloaded => AnalysisErrorCodes.RateLimited,
        AIProviderErrorKind.InvalidRequest => AnalysisErrorCodes.RequestRejected,
        _ => AnalysisErrorCodes.Unavailable,
    };
}
