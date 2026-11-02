using PostulaAI.Api.Modules.JobPostings;
using PostulaAI.Api.Modules.JobPostings.Contracts;
using PostulaAI.Api.Modules.JobPostings.Domain;

namespace PostulaAI.UnitTests.JobPostings;

public sealed class JobPostingValidatorTests
{
    private const string Description = "Buscamos desarrolladora backend con experiencia en C# y PostgreSQL.";

    private static readonly JobPostingRequestValidator Validator = new();

    private static JobPostingRequest Valid() =>
        new("Desarrolladora backend", "Empresa Ficticia", "Bogotá", WorkMode.Hybrid, Description, null);

    private static IEnumerable<string> ErrorPaths(JobPostingRequest request) =>
        Validator.Validate(request).Errors.Select(error => error.PropertyName);

    [Fact]
    public void ValidRequest_Passes() => Assert.True(Validator.Validate(Valid()).IsValid);

    [Fact]
    public void CompanyAndLocation_AreOptional() =>
        Assert.True(Validator.Validate(Valid() with { Company = null, Location = null, WorkMode = WorkMode.Unspecified }).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Title_IsRequired(string title) => Assert.Contains("Title", ErrorPaths(Valid() with { Title = title }));

    [Fact]
    public void Description_MustLookLikeARealPosting() =>
        Assert.Contains("Description", ErrorPaths(Valid() with { Description = "Backend .NET" }));

    [Fact]
    public void Description_HasAnUpperBound() =>
        Assert.Contains("Description", ErrorPaths(Valid() with { Description = new string('x', JobPostingLimits.DescriptionMax + 1) }));

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://ejemplo.test/vacante")]
    public void SourceUrl_MustBeHttp(string url) => Assert.Contains("SourceUrl", ErrorPaths(Valid() with { SourceUrl = url }));

    [Fact]
    public void UndefinedWorkMode_IsRejected() => Assert.Contains("WorkMode", ErrorPaths(Valid() with { WorkMode = (WorkMode)42 }));

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, JobPostingLimits.MaxPageSize + 1)]
    public void ListQuery_RejectsInvalidPaging(int page, int pageSize) =>
        Assert.False(new JobPostingListQueryValidator().Validate(new JobPostingListQuery(page, pageSize)).IsValid);

    [Theory]
    [InlineData("100%", "100\\%")]
    [InlineData("c_sharp", "c\\_sharp")]
    [InlineData("a\\b", "a\\\\b")]
    [InlineData(".NET", ".NET")]
    public void EscapeLikePattern_MatchesWildcardsLiterally(string input, string expected) =>
        Assert.Equal(expected, JobPostingService.EscapeLikePattern(input));
}
