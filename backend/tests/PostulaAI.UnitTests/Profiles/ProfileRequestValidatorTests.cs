using PostulaAI.Api.Modules.ProfessionalProfiles.Contracts;
using PostulaAI.Api.Modules.ProfessionalProfiles.Domain;

namespace PostulaAI.UnitTests.Profiles;

public sealed class ProfileRequestValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly ProfileRequestValidator Validator = new(new FixedTimeProvider(Now));

    private static ProfileRequest Valid() => new(
        "Laura Gómez", null, null,
        new ProfileLinksDto(null, null, null),
        [new WorkExperienceDto("Empresa", "Cargo", null, new DateOnly(2024, 1, 1), null, true, null, [], ["C#"])],
        [], [], [],
        ["C#"], [],
        [new LanguageDto("Español", LanguageProficiency.Native)]);

    private static IEnumerable<string> ErrorPaths(ProfileRequest request) =>
        Validator.Validate(request).Errors.Select(error => error.PropertyName);

    private static WorkExperienceDto Experience(DateOnly start, DateOnly? end, bool isCurrent) =>
        new("Empresa", "Cargo", null, start, end, isCurrent, null, [], []);

    [Fact]
    public void ValidProfile_Passes() => Assert.True(Validator.Validate(Valid()).IsValid);

    [Fact]
    public void MinimalProfile_WithOnlyName_Passes() =>
        Assert.True(Validator.Validate(Valid() with { Experiences = [], TechnicalSkills = [], Languages = [] }).IsValid);

    [Fact]
    public void CurrentJob_WithEndDate_IsRejected() =>
        Assert.Contains("Experiences[0].EndDate", ErrorPaths(Valid() with
        {
            Experiences = [Experience(new DateOnly(2024, 1, 1), new DateOnly(2025, 1, 1), isCurrent: true)],
        }));

    [Fact]
    public void PastJob_WithoutEndDate_IsRejected() =>
        Assert.Contains("Experiences[0].EndDate", ErrorPaths(Valid() with
        {
            Experiences = [Experience(new DateOnly(2024, 1, 1), null, isCurrent: false)],
        }));

    [Fact]
    public void EndDate_BeforeStartDate_IsRejected() =>
        Assert.Contains("Experiences[0].EndDate", ErrorPaths(Valid() with
        {
            Experiences = [Experience(new DateOnly(2024, 1, 1), new DateOnly(2023, 1, 1), isCurrent: false)],
        }));

    [Fact]
    public void StartDate_InTheFuture_IsRejected() =>
        Assert.Contains("Experiences[0].StartDate", ErrorPaths(Valid() with
        {
            Experiences = [Experience(new DateOnly(2027, 1, 1), null, isCurrent: true)],
        }));

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://ejemplo.test")]
    [InlineData("no es una url")]
    public void Links_MustBeHttpOrHttps(string url) =>
        Assert.Contains("links.linkedInUrl", ErrorPaths(Valid() with { Links = new ProfileLinksDto(url, null, null) }));

    [Fact]
    public void DuplicateSkills_IgnoringCase_AreRejected() =>
        Assert.Contains("TechnicalSkills", ErrorPaths(Valid() with { TechnicalSkills = ["C#", " c# "] }));

    [Fact]
    public void BlankSkill_IsRejected() =>
        Assert.Contains(ErrorPaths(Valid() with { TechnicalSkills = ["C#", " "] }), path => path.StartsWith("TechnicalSkills[", StringComparison.Ordinal));

    [Fact]
    public void TooManyExperiences_IsRejected() =>
        Assert.Contains("experiences", ErrorPaths(Valid() with
        {
            Experiences = [.. Enumerable.Repeat(Experience(new DateOnly(2024, 1, 1), null, isCurrent: true), ProfileLimits.MaxExperiences + 1)],
        }));

    [Fact]
    public void UndefinedEnumValue_IsRejected() =>
        Assert.Contains("Languages[0].Proficiency", ErrorPaths(Valid() with
        {
            Languages = [new LanguageDto("Inglés", (LanguageProficiency)99)],
        }));
}
