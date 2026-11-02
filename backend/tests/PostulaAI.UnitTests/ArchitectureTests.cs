using PostulaAI.AI.Clients;

namespace PostulaAI.UnitTests;

/// <summary>
/// Guards the rule in .claude/rules/architecture.md: the AI library is shared by production and the
/// evaluation runner, so it must stay free of web-host and persistence dependencies.
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly string[] ForbiddenPrefixes =
    [
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "PostulaAI.Api",
    ];

    [Fact]
    public void AiLibrary_DoesNotReference_WebHostOrPersistenceAssemblies()
    {
        var referenced = typeof(IAIClient).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToList();

        var violations = referenced
            .Where(name => ForbiddenPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void AiLibrary_ProjectFile_HasNoForbiddenReferences()
    {
        var projectFile = FindRepoFile(Path.Combine("backend", "src", "PostulaAI.AI", "PostulaAI.AI.csproj"));
        var content = File.ReadAllText(projectFile);

        foreach (var prefix in ForbiddenPrefixes)
        {
            Assert.DoesNotContain(prefix, content, StringComparison.Ordinal);
        }
    }

    private static string FindRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate '{relativePath}' above the test output directory.");
    }
}
