using PostulaAI.Api.Shared.Errors;

namespace PostulaAI.UnitTests.Shared;

public sealed class ProblemDetailsWriterTests
{
    [Theory]
    [InlineData("Email", "email")]
    [InlineData("Experiences[0].EndDate", "experiences[0].endDate")]
    [InlineData("links.linkedInUrl", "links.linkedInUrl")]
    [InlineData("", "")]
    public void ToCamelCasePath_MatchesFrontendFieldNames(string input, string expected) =>
        Assert.Equal(expected, ProblemDetailsWriter.ToCamelCasePath(input));
}
