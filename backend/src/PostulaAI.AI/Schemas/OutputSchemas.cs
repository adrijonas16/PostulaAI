using System.Text;

namespace PostulaAI.AI.Schemas;

public static class OutputSchemas
{
    public const string JobAnalysisV1 = "job-analysis-v1";

    /// <summary>Returns the JSON Schema text sent to the provider as structured output format.</summary>
    public static string Load(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using var stream = typeof(OutputSchemas).Assembly.GetManifestResourceStream($"Schemas.{name}.schema.json")
            ?? throw new InvalidOperationException($"Schema '{name}' was not found as an embedded resource.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
