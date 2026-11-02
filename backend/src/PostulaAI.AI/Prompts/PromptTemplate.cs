using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PostulaAI.AI.Prompts;

/// <summary>
/// A versioned prompt loaded from an embedded "&lt;id&gt;-v&lt;N&gt;.txt" file with optional "=== SYSTEM ===" and
/// "=== USER ===" sections. Placeholders use {{name}} and are replaced in a single pass, so user data that happens to
/// contain "{{...}}" is never interpreted as a placeholder.
/// </summary>
public sealed partial class PromptTemplate
{
    private const string SystemMarker = "=== SYSTEM ===";
    private const string UserMarker = "=== USER ===";

    private PromptTemplate(string id, string version, string system, string user, string sha256)
    {
        Id = id;
        Version = version;
        System = system;
        User = user;
        Sha256 = sha256;
    }

    public string Id { get; }

    public string Version { get; }

    public string System { get; }

    public string User { get; }

    /// <summary>Hash of the file content, useful to detect accidental edits of a published prompt.</summary>
    public string Sha256 { get; }

    public IReadOnlySet<string> Placeholders =>
        PlaceholderRegex().Matches(System + User).Select(match => match.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

    public static PromptTemplate Load(string id, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        var resourceName = $"Prompts.{id}-{version}.txt";
        using var stream = typeof(PromptTemplate).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Prompt '{id}-{version}' was not found as an embedded resource.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);

        var sha = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        var (system, user) = Split(content);
        return new PromptTemplate(id, version, system, user, sha);
    }

    public static IReadOnlyList<string> AvailableResources() =>
        [.. Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(name => name.StartsWith("Prompts.", StringComparison.Ordinal))];

    public string RenderSystem(IReadOnlyDictionary<string, string> variables) => Render(System, variables);

    public string RenderUser(IReadOnlyDictionary<string, string> variables) => Render(User, variables);

    private static string Render(string text, IReadOnlyDictionary<string, string> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        var missing = PlaceholderRegex().Matches(text)
            .Select(match => match.Groups[1].Value)
            .Where(name => !variables.ContainsKey(name))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException($"Missing prompt variables: {string.Join(", ", missing)}.");
        }

        return PlaceholderRegex().Replace(text, match => variables[match.Groups[1].Value]).Trim();
    }

    private static (string System, string User) Split(string content)
    {
        var systemIndex = content.IndexOf(SystemMarker, StringComparison.Ordinal);
        var userIndex = content.IndexOf(UserMarker, StringComparison.Ordinal);
        if (userIndex < 0)
        {
            throw new InvalidOperationException($"Prompt files must contain a '{UserMarker}' section.");
        }

        var system = systemIndex >= 0 && systemIndex < userIndex
            ? content[(systemIndex + SystemMarker.Length)..userIndex].Trim()
            : string.Empty;
        var user = content[(userIndex + UserMarker.Length)..].Trim();
        return (system, user);
    }

    [GeneratedRegex(@"\{\{([a-z_]+)\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegex();
}
