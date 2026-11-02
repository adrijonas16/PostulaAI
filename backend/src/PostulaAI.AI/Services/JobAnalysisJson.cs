using System.Text.Json;
using System.Text.Json.Serialization;
using PostulaAI.AI.Contracts;

namespace PostulaAI.AI.Services;

/// <summary>
/// Strict (de)serialization of <see cref="JobAnalysisResult"/>: unknown properties, missing properties, nulls and
/// unknown enum values are all rejected. This is the schema validation step of the pipeline.
/// </summary>
public static class JobAnalysisJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false) },
    };

    /// <summary>Compact camelCase JSON used to give the profile to the model (nulls omitted).</summary>
    public static JsonSerializerOptions PromptOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static bool TryParse(string json, out JobAnalysisResult? result, out string? error)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            result = JsonSerializer.Deserialize<JobAnalysisResult>(json, Options);
            error = result is null ? "The response is JSON null instead of an object." : null;
            return result is not null;
        }
        catch (JsonException exception)
        {
            result = null;
            error = $"The response does not match the schema: {exception.Message}";
            return false;
        }
    }

    public static string Serialize(JobAnalysisResult result) => JsonSerializer.Serialize(result, Options);
}
