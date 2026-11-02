using PostulaAI.AI.Contracts;
using PostulaAI.AI.Evaluations;

namespace PostulaAI.AI.Services;

/// <summary>Business rules applied after schema validation. Messages are in English because they feed the repair prompt.</summary>
public static class JobAnalysisRules
{
    public const int MaxStrengths = 20;
    public const int MaxGaps = 20;
    public const int MaxKeywords = 25;
    public const int MaxCvRecommendations = 12;

    public static IReadOnlyList<string> Validate(JobAnalysisResult result, CandidateProfile profile)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(profile);

        // Structured outputs and strict parsing make null items very unlikely, but the rules below must never throw.
        if (result.Strengths.Any(item => item is null) || result.Gaps.Any(item => item is null)
            || result.KeywordsToInclude.Any(item => item is null) || result.CvRecommendations.Any(item => item is null))
        {
            return ["Arrays must not contain null items."];
        }

        var issues = new List<string>();

        void Text(string path, string value, int max)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                issues.Add($"{path} must not be empty.");
            }
            else if (value.Length > max)
            {
                issues.Add($"{path} is too long ({value.Length} characters, maximum {max}).");
            }
        }

        if (result.CompatibilityScore is < 0 or > 100)
        {
            issues.Add($"compatibilityScore must be an integer between 0 and 100 (got {result.CompatibilityScore}).");
        }

        Text("summary", result.Summary, 2000);
        Text("adaptedProfessionalSummary", result.AdaptedProfessionalSummary, 1500);
        Text("coverLetterDraft", result.CoverLetterDraft, 4000);
        Text("disclaimer", result.Disclaimer, 500);

        if (result.Strengths.Count + result.Gaps.Count == 0)
        {
            issues.Add("At least one requirement must be reported in strengths or gaps.");
        }

        if (result.Strengths.Count > MaxStrengths)
        {
            issues.Add($"strengths has {result.Strengths.Count} items (maximum {MaxStrengths}).");
        }

        for (var i = 0; i < result.Strengths.Count; i++)
        {
            Text($"strengths[{i}].requirement", result.Strengths[i].Requirement, 500);
            Text($"strengths[{i}].evidence", result.Strengths[i].Evidence, 1000);
        }

        if (result.Gaps.Count > MaxGaps)
        {
            issues.Add($"gaps has {result.Gaps.Count} items (maximum {MaxGaps}).");
        }

        for (var i = 0; i < result.Gaps.Count; i++)
        {
            Text($"gaps[{i}].requirement", result.Gaps[i].Requirement, 500);
            Text($"gaps[{i}].recommendation", result.Gaps[i].Recommendation, 1000);
        }

        if (result.KeywordsToInclude.Count > MaxKeywords)
        {
            issues.Add($"keywordsToInclude has {result.KeywordsToInclude.Count} items (maximum {MaxKeywords}).");
        }

        for (var i = 0; i < result.KeywordsToInclude.Count; i++)
        {
            Text($"keywordsToInclude[{i}]", result.KeywordsToInclude[i], 80);
        }

        if (result.KeywordsToInclude.Select(keyword => keyword.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != result.KeywordsToInclude.Count)
        {
            issues.Add("keywordsToInclude contains duplicates.");
        }

        if (result.CvRecommendations.Count is 0 or > MaxCvRecommendations)
        {
            issues.Add($"cvRecommendations must have between 1 and {MaxCvRecommendations} items (got {result.CvRecommendations.Count}).");
        }

        for (var i = 0; i < result.CvRecommendations.Count; i++)
        {
            Text($"cvRecommendations[{i}]", result.CvRecommendations[i], 600);
        }

        foreach (var claim in GroundingChecker.FindUnsupportedClaims(result, profile))
        {
            issues.Add($"{claim.Path} mentions \"{claim.Term}\", which is not supported by the candidate profile.");
        }

        return issues;
    }
}
