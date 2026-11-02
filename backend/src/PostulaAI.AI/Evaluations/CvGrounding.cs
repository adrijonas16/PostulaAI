using System.Text.RegularExpressions;
using PostulaAI.AI.Contracts;

namespace PostulaAI.AI.Evaluations;

/// <summary>
/// Removes skills, technologies and languages that do not literally appear in the CV text, so the imported draft
/// never contains something the candidate did not write. Each removal produces a user-facing warning.
/// </summary>
public static class CvGrounding
{
    public static (CvExtraction Extraction, IReadOnlyList<string> Warnings) Apply(CvExtraction extraction, string cvText)
    {
        ArgumentNullException.ThrowIfNull(extraction);
        ArgumentNullException.ThrowIfNull(cvText);

        var source = TechVocabulary.Normalize(cvText);
        var warnings = new List<string>();

        IReadOnlyList<string> Keep(IReadOnlyList<string> items, string kind)
        {
            var kept = new List<string>();
            foreach (var item in items)
            {
                if (AppearsIn(item, source))
                {
                    kept.Add(item);
                }
                else
                {
                    warnings.Add($"Se omitió {kind} «{item}» porque no aparece en el CV.");
                }
            }

            return kept;
        }

        var result = extraction with
        {
            TechnicalSkills = Keep(extraction.TechnicalSkills, "la habilidad técnica"),
            SoftSkills = Keep(extraction.SoftSkills, "la habilidad blanda"),
            Experiences = [.. extraction.Experiences.Select(item => item with { Technologies = Keep(item.Technologies, "la tecnología") })],
            Projects = [.. extraction.Projects.Select(item => item with { Technologies = Keep(item.Technologies, "la tecnología") })],
            Languages = [.. extraction.Languages.Where(language =>
            {
                var present = AppearsIn(language.Language, source);
                if (!present)
                {
                    warnings.Add($"Se omitió el idioma «{language.Language}» porque no aparece en el CV.");
                }

                return present;
            })],
        };

        return (result, warnings.Distinct(StringComparer.Ordinal).ToList());
    }

    /// <summary>True when the whole term appears in the (normalized) source, ignoring case, accents and spacing.</summary>
    public static bool AppearsIn(string term, string normalizedSource)
    {
        ArgumentNullException.ThrowIfNull(term);
        ArgumentNullException.ThrowIfNull(normalizedSource);

        var normalized = Regex.Replace(TechVocabulary.Normalize(term).Trim(), @"\s+", " ");
        if (normalized.Length == 0)
        {
            return false;
        }

        var pattern = string.Join(@"\s+", normalized.Split(' ').Select(Regex.Escape));
        return Regex.IsMatch(normalizedSource, $@"(?<![\p{{L}}\p{{N}}]){pattern}(?![\p{{L}}\p{{N}}#+])", RegexOptions.CultureInvariant);
    }
}
