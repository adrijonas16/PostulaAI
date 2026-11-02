using PostulaAI.Api.Modules.JobPostings.Contracts;
using PostulaAI.Api.Modules.JobPostings.Domain;

namespace PostulaAI.UnitTests.JobPostings;

public sealed class ApplicationRequestValidatorTests
{
    private static readonly DateOnly Today = new(2026, 11, 2);

    private static readonly ApplicationRequestValidator Validator =
        new(new FixedTimeProvider(new DateTimeOffset(2026, 11, 2, 15, 0, 0, TimeSpan.Zero)));

    private static ApplicationRequest Applied() => new(ApplicationStatus.Applied, Today, null, "Resumen enviado.", null);

    private static IEnumerable<string> ErrorPaths(ApplicationRequest request) =>
        Validator.Validate(request).Errors.Select(error => error.PropertyName);

    [Fact]
    public void Applied_WithDate_Passes() => Assert.True(Validator.Validate(Applied()).IsValid);

    [Fact]
    public void Saved_WithoutDate_Passes() =>
        Assert.True(Validator.Validate(new ApplicationRequest(ApplicationStatus.Saved, null, null, null, null)).IsValid);

    [Theory]
    [InlineData(ApplicationStatus.Applied)]
    [InlineData(ApplicationStatus.Interview)]
    [InlineData(ApplicationStatus.Offer)]
    [InlineData(ApplicationStatus.Rejected)]
    public void EveryStatusBeyondSaved_RequiresTheApplicationDate(ApplicationStatus status) =>
        Assert.Contains("AppliedOn", ErrorPaths(Applied() with { Status = status, AppliedOn = null }));

    [Fact]
    public void Tomorrow_IsAllowedForTimeZonesAheadOfUtc() =>
        Assert.True(Validator.Validate(Applied() with { AppliedOn = Today.AddDays(1) }).IsValid);

    [Fact]
    public void FutureDate_IsRejected() =>
        Assert.Contains("AppliedOn", ErrorPaths(Applied() with { AppliedOn = Today.AddDays(2) }));

    [Fact]
    public void UnknownStatus_IsRejected() =>
        Assert.Contains("Status", ErrorPaths(Applied() with { Status = (ApplicationStatus)42 }));

    [Fact]
    public void Texts_HaveUpperBounds()
    {
        var errors = ErrorPaths(Applied() with
        {
            Notes = new string('x', JobPostingLimits.ApplicationNotes + 1),
            SubmittedSummary = new string('x', JobPostingLimits.SubmittedSummary + 1),
            SubmittedCoverLetter = new string('x', JobPostingLimits.SubmittedCoverLetter + 1),
        }).ToList();

        Assert.Contains("Notes", errors);
        Assert.Contains("SubmittedSummary", errors);
        Assert.Contains("SubmittedCoverLetter", errors);
    }
}
