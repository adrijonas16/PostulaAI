using System.Text.Json;
using PostulaAI.AI.Contracts;
using PostulaAI.AI.Services;

namespace PostulaAI.AI.Evaluations;

/// <summary>
/// Deterministic guard against invented experience: every technology, tool, certification or language that the
/// result attributes to the candidate (strength evidence, adapted summary, CV keywords) must appear in the profile.
/// It cannot prove honesty, but it catches the most common and most damaging fabrications.
/// </summary>
public static class GroundingChecker
{
    public sealed record UnsupportedClaim(string Path, string Term);

    public static IReadOnlyList<UnsupportedClaim> FindUnsupportedClaims(JobAnalysisResult result, CandidateProfile profile)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(profile);

        var profileText = TechVocabulary.Normalize(JsonSerializer.Serialize(profile, JobAnalysisJson.PromptOptions));
        var claims = new List<UnsupportedClaim>();

        void Check(string path, string text)
        {
            foreach (var term in TechVocabulary.Detect(TechVocabulary.Normalize(text)))
            {
                if (!TechVocabulary.IsSupported(term, profileText))
                {
                    claims.Add(new UnsupportedClaim(path, term.Name));
                }
            }
        }

        for (var i = 0; i < result.Strengths.Count; i++)
        {
            Check($"strengths[{i}].evidence", result.Strengths[i].Evidence);
        }

        Check("adaptedProfessionalSummary", result.AdaptedProfessionalSummary);

        for (var i = 0; i < result.KeywordsToInclude.Count; i++)
        {
            Check($"keywordsToInclude[{i}]", result.KeywordsToInclude[i]);
        }

        return claims;
    }
}
