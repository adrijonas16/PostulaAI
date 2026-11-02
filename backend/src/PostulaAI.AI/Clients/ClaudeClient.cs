using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PostulaAI.AI.Clients;

public sealed class ClaudeOptions
{
    public const string SectionName = "Anthropic";

    /// <summary>Secret. Comes from environment/user-secrets; never from the repository.</summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "claude-sonnet-5-5";

    public Uri BaseAddress { get; set; } = new("https://api.anthropic.com/");

    public string ApiVersion { get; set; } = "2023-06-01";

    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>Retries for 429, 529, 5xx and network failures (on top of the first attempt).</summary>
    public int MaxRetries { get; set; } = 2;

    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(20);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}

/// <summary>
/// Claude Messages API client (POST /v1/messages) using structured outputs (output_config.format = json_schema).
/// Retries rate-limit (429), overload (529), 5xx and network errors with bounded backoff that honours retry-after.
/// Logs and exceptions never include prompts, responses or the API key.
/// </summary>
public sealed class ClaudeClient(HttpClient httpClient, ClaudeOptions options, TimeProvider time) : IAIClient
{
    public string Model => options.Model;

    public async Task<AICompletionResult> CompleteAsync(AICompletionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!options.IsConfigured)
        {
            throw new AIProviderException(AIProviderErrorKind.NotConfigured, "The Anthropic API key is not configured.");
        }

        var body = BuildBody(request).ToJsonString();
        var started = time.GetTimestamp();

        for (var attempt = 0; ; attempt++)
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(options.BaseAddress, "v1/messages"))
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            message.Headers.Add("x-api-key", options.ApiKey);
            message.Headers.Add("anthropic-version", options.ApiVersion);

            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(message, cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
            {
                if (attempt < options.MaxRetries)
                {
                    await Task.Delay(Backoff(attempt, retryAfter: null), time, cancellationToken);
                    continue;
                }

                throw new AIProviderException(AIProviderErrorKind.Unavailable, "The AI provider could not be reached.", null, exception);
            }

            using (response)
            {
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    return ParseResponse(json, time.GetElapsedTime(started));
                }

                var status = (int)response.StatusCode;
                var retryable = status is 429 or 529 or >= 500;
                if (retryable && attempt < options.MaxRetries)
                {
                    await Task.Delay(Backoff(attempt, RetryAfter(response.Headers)), time, cancellationToken);
                    continue;
                }

                throw new AIProviderException(KindFor(response.StatusCode), $"The AI provider returned HTTP {status}.", status);
            }
        }
    }

    internal static JsonObject BuildBody(AICompletionRequest request, string model)
    {
        var messages = new JsonArray();
        foreach (var item in request.Messages)
        {
            messages.Add(new JsonObject
            {
                ["role"] = item.Role == AIMessageRole.User ? "user" : "assistant",
                ["content"] = item.Content,
            });
        }

        var body = new JsonObject
        {
            ["model"] = model,
            ["max_tokens"] = request.MaxOutputTokens,
            ["messages"] = messages,
        };

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            body["system"] = request.SystemPrompt;
        }

        if (request.OutputJsonSchema is not null)
        {
            body["output_config"] = new JsonObject
            {
                ["format"] = new JsonObject
                {
                    ["type"] = "json_schema",
                    ["schema"] = JsonNode.Parse(request.OutputJsonSchema),
                },
            };
        }

        return body;
    }

    private JsonObject BuildBody(AICompletionRequest request) => BuildBody(request, options.Model);

    internal static AICompletionResult ParseResponse(string json, TimeSpan latency)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var text = new StringBuilder();
            if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in content.EnumerateArray())
                {
                    // Thinking blocks and other block types are ignored; only the final text carries the JSON.
                    if (block.TryGetProperty("type", out var type) && type.GetString() == "text"
                        && block.TryGetProperty("text", out var value))
                    {
                        text.Append(value.GetString());
                    }
                }
            }

            var usage = root.TryGetProperty("usage", out var usageElement) ? usageElement : default;
            return new AICompletionResult(
                text.ToString(),
                root.TryGetProperty("model", out var model) ? model.GetString() ?? string.Empty : string.Empty,
                ReadInt(usage, "input_tokens"),
                ReadInt(usage, "output_tokens"),
                latency,
                root.TryGetProperty("stop_reason", out var stop) ? stop.GetString() : null);
        }
        catch (JsonException exception)
        {
            throw new AIProviderException(AIProviderErrorKind.Unavailable, "The AI provider returned an unreadable response.", null, exception);
        }
    }

    internal TimeSpan Backoff(int attempt, TimeSpan? retryAfter)
    {
        var delay = retryAfter ?? options.BaseRetryDelay * Math.Pow(2, attempt);
        return delay > options.MaxRetryDelay ? options.MaxRetryDelay : delay;
    }

    private static TimeSpan? RetryAfter(HttpResponseHeaders headers)
    {
        if (headers.RetryAfter?.Delta is { } delta)
        {
            return delta;
        }

        return headers.TryGetValues("retry-after", out var values)
            && int.TryParse(values.FirstOrDefault(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var seconds)
            ? TimeSpan.FromSeconds(seconds)
            : null;
    }

    private static AIProviderErrorKind KindFor(HttpStatusCode status) => (int)status switch
    {
        401 or 403 => AIProviderErrorKind.Authentication,
        429 => AIProviderErrorKind.RateLimited,
        529 => AIProviderErrorKind.Overloaded,
        >= 500 => AIProviderErrorKind.Unavailable,
        _ => AIProviderErrorKind.InvalidRequest,
    };

    private static int ReadInt(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.TryGetInt32(out var number)
            ? number
            : 0;
}
