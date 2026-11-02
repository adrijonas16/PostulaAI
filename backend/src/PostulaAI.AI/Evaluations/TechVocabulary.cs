using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PostulaAI.AI.Evaluations;

/// <summary>
/// Curated list of technologies, tools, certifications and languages that are easy to "invent".
/// Each term is detected by its aliases and is supported by a profile that mentions any alias or any of its
/// <c>SupportedBy</c> terms (for example, PostgreSQL supports SQL; TypeScript supports JavaScript).
/// </summary>
public static class TechVocabulary
{
    public sealed record Term(string Name, IReadOnlyList<string> Aliases, IReadOnlyList<string> SupportedBy)
    {
        internal IReadOnlyList<Regex> Detectors { get; } = [.. Aliases.Select(BuildRegex)];

        internal IReadOnlyList<Regex> Supporters { get; } = [.. Aliases.Concat(SupportedBy).Select(BuildRegex)];
    }

    private static Term T(string name, string[]? aliases = null, string[]? supportedBy = null) =>
        new(name, aliases ?? [name.ToLowerInvariant()], supportedBy ?? []);

    public static IReadOnlyList<Term> Terms { get; } =
    [
        T("C#", ["c#", "csharp"]),
        T(".NET", [".net", "dotnet", ".net core", ".net framework"], ["c#", "asp.net"]),
        T("ASP.NET", ["asp.net", "asp.net core", "aspnet"]),
        T("Java"),
        T("JavaScript", ["javascript", "js", "ecmascript"], ["typescript", "node.js", "react", "angular", "vue"]),
        T("TypeScript", ["typescript"]),
        T("Python"),
        T("Go", ["golang"]),
        T("Rust"),
        T("Kotlin"),
        T("Swift"),
        T("PHP"),
        T("Ruby"),
        T("Ruby on Rails", ["ruby on rails", "rails"]),
        T("Scala"),
        T("C++", ["c++"]),
        T("React", ["react", "react.js", "reactjs"]),
        T("React Native", ["react native"]),
        T("Angular"),
        T("Vue", ["vue", "vue.js", "vuejs"]),
        T("Next.js", ["next.js", "nextjs"]),
        T("Node.js", ["node.js", "nodejs"]),
        T("Express", ["express.js", "expressjs"]),
        T("Django"),
        T("Flask"),
        T("FastAPI"),
        T("Spring", ["spring boot", "spring framework"]),
        T("Laravel"),
        T("Flutter"),
        T("Android"),
        T("iOS", ["ios"]),
        T("SQL", ["sql"], ["postgresql", "postgres", "mysql", "sql server", "oracle", "sqlite", "mariadb", "t-sql", "pl/sql"]),
        T("PostgreSQL", ["postgresql", "postgres"]),
        T("MySQL"),
        T("SQL Server", ["sql server", "mssql"]),
        T("Oracle"),
        T("SQLite"),
        T("MongoDB", ["mongodb", "mongo"]),
        T("Redis"),
        T("Elasticsearch"),
        T("Entity Framework", ["entity framework", "ef core"]),
        T("GraphQL"),
        T("Docker"),
        T("Kubernetes", ["kubernetes", "k8s"]),
        T("Terraform"),
        T("Ansible"),
        T("AWS", ["aws", "amazon web services"]),
        T("Azure", ["azure", "microsoft azure"]),
        T("Google Cloud", ["gcp", "google cloud"]),
        T("Linux"),
        T("Git"),
        T("GitHub Actions", ["github actions"]),
        T("GitLab CI", ["gitlab ci"]),
        T("Jenkins"),
        T("Kafka"),
        T("RabbitMQ"),
        T("Spark", ["apache spark", "pyspark"]),
        T("Airflow"),
        T("Hadoop"),
        T("Pandas"),
        T("NumPy", ["numpy"]),
        T("scikit-learn", ["scikit-learn", "sklearn"]),
        T("TensorFlow"),
        T("PyTorch"),
        T("Power BI", ["power bi", "powerbi"]),
        T("Tableau"),
        T("Excel"),
        T("Figma"),
        T("Jira"),
        T("Scrum"),
        T("Salesforce"),
        T("SAP"),
        T("HTML", ["html", "html5"]),
        T("CSS", ["css", "css3"], ["sass", "scss", "tailwind"]),
        T("Sass", ["sass", "scss"]),
        T("Tailwind", ["tailwind", "tailwindcss"]),
        T("Jest"),
        T("Cypress"),
        T("Selenium"),
        T("Playwright"),
        T("xUnit", ["xunit"]),
        T("NUnit", ["nunit"]),
        T("Unity"),
        T("PMP"),
        T("ITIL"),
        T("English", ["english", "ingles"]),
        T("Portuguese", ["portuguese", "portugues"]),
        T("French", ["french", "frances"]),
        T("German", ["german", "aleman"]),
    ];

    /// <summary>Lower-case, accent-free text so "Inglés" and "ingles" match.</summary>
    public static string Normalize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>Terms mentioned in <paramref name="normalizedText"/>.</summary>
    public static IEnumerable<Term> Detect(string normalizedText) =>
        Terms.Where(term => term.Detectors.Any(detector => detector.IsMatch(normalizedText)));

    public static bool IsSupported(Term term, string normalizedProfileText)
    {
        ArgumentNullException.ThrowIfNull(term);
        return term.Supporters.Any(supporter => supporter.IsMatch(normalizedProfileText));
    }

    private static Regex BuildRegex(string alias) =>
        new($@"(?<![\p{{L}}\p{{N}}#+.]){Regex.Escape(alias)}(?![\p{{L}}\p{{N}}#+])", RegexOptions.CultureInvariant | RegexOptions.Compiled);
}
