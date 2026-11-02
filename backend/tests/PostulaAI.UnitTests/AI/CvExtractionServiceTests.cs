using System.Text.Json;
using PostulaAI.AI.Clients;
using PostulaAI.AI.Contracts;
using PostulaAI.AI.Prompts;
using PostulaAI.AI.Services;

namespace PostulaAI.UnitTests.AI;

public sealed class CvExtractionServiceTests
{
    private const string CvText = "Laura Gómez. Desarrolladora backend en Empresa Ficticia desde 2022 con C# y PostgreSQL. Inglés B2.";

    private static string Json(CvExtraction extraction) => JsonSerializer.Serialize(extraction, JobAnalysisJson.Options);

    private static CvExtraction Valid() => new(
        "Laura Gómez", "Desarrolladora backend", null, new CvLinks(null, null, null),
        [new CvExperience("Empresa Ficticia", "Desarrolladora backend", null, "2022-01-01", null, true, null, [], ["C#", "PostgreSQL"])],
        [], [], [], ["C#", "PostgreSQL", "Kubernetes"], [], [new CvLanguage("Inglés", CvLanguageLevel.Intermediate)]);

    private static CvExtractionService Service(FakeAIClient client, int maxCharacters = 30_000) =>
        new(client, new CvExtractionOptions { MaxCvCharacters = maxCharacters });

    [Fact]
    public void Prompts_Load_WithTheirPlaceholders()
    {
        Assert.Equal(new[] { "cv_text" }, PromptTemplate.Load("cv-extraction", "v1").Placeholders);
        Assert.Equal(new[] { "validation_errors" }, PromptTemplate.Load("cv-extraction-repair", "v1").Placeholders);
    }

    [Fact]
    public async Task ValidExtraction_IsGrounded_AndUsesTheCvSchema()
    {
        var client = new FakeAIClient().Returns(AIFixtures.Completion(Json(Valid())));

        var outcome = await Service(client).ExtractAsync(CvText, CancellationToken.None);

        Assert.True(outcome.Succeeded);
        Assert.Equal(new[] { "C#", "PostgreSQL" }, outcome.Extraction?.TechnicalSkills);
        Assert.Contains(outcome.Warnings, warning => warning.Contains("«Kubernetes»", StringComparison.Ordinal));
        Assert.Equal(("cv-extraction", "v1"), (outcome.PromptId, outcome.PromptVersion));
        var request = Assert.Single(client.Requests);
        Assert.Contains("\"technicalSkills\"", request.OutputJsonSchema ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains(CvText, request.Messages[0].Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidJson_IsRepairedOnce()
    {
        var client = new FakeAIClient()
            .Returns(AIFixtures.Completion("{\"fullName\": 1}"))
            .Returns(AIFixtures.Completion(Json(Valid())));

        var outcome = await Service(client).ExtractAsync(CvText, CancellationToken.None);

        Assert.True(outcome.Succeeded);
        Assert.True(outcome.RepairAttempted);
        Assert.Equal(2, client.Requests.Count);
    }

    [Fact]
    public async Task InvalidTwice_Fails()
    {
        var client = new FakeAIClient().Returns(AIFixtures.Completion("nope")).Returns(AIFixtures.Completion("still nope"));

        var outcome = await Service(client).ExtractAsync(CvText, CancellationToken.None);

        Assert.False(outcome.Succeeded);
        Assert.Equal(AnalysisErrorCodes.InvalidOutput, outcome.ErrorCode);
        Assert.Null(outcome.Extraction);
    }

    [Fact]
    public async Task MissingApiKey_MapsToNotConfigured()
    {
        var outcome = await Service(new FakeAIClient().Throws(AIProviderErrorKind.NotConfigured)).ExtractAsync(CvText, CancellationToken.None);

        Assert.Equal(AnalysisErrorCodes.NotConfigured, outcome.ErrorCode);
        Assert.False(outcome.ProviderCalled);
    }

    [Fact]
    public async Task VeryLongCv_IsTruncated_WithAWarning()
    {
        var client = new FakeAIClient().Returns(AIFixtures.Completion(Json(Valid())));

        var outcome = await Service(client, maxCharacters: 40).ExtractAsync(CvText, CancellationToken.None);

        Assert.Contains(outcome.Warnings, warning => warning.Contains("muy largo", StringComparison.Ordinal));
        Assert.DoesNotContain("Inglés B2", client.Requests[0].Messages[0].Content, StringComparison.Ordinal);
    }
}
