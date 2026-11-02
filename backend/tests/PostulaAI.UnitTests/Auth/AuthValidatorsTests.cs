using PostulaAI.Api.Modules.Auth.Contracts;

namespace PostulaAI.UnitTests.Auth;

public sealed class AuthValidatorsTests
{
    [Fact]
    public void Register_ValidRequest_Passes() =>
        Assert.True(new RegisterRequestValidator().Validate(new RegisterRequest("ana@example.test", "contraseña-larga", "Ana")).IsValid);

    [Theory]
    [InlineData("sin-arroba", "contraseña-larga", "Ana", "Email")]
    [InlineData("ana@example.test", "corta", "Ana", "Password")]
    [InlineData("ana@example.test", "contraseña-larga", "   ", "DisplayName")]
    public void Register_InvalidField_IsReported(string email, string password, string displayName, string expectedProperty)
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest(email, password, displayName));

        Assert.Contains(result.Errors, error => error.PropertyName == expectedProperty);
    }

    [Fact]
    public void Register_RejectsOverlongPassword()
    {
        var result = new RegisterRequestValidator().Validate(
            new RegisterRequest("ana@example.test", new string('x', PasswordRules.MaxLength + 1), "Ana"));

        Assert.Contains(result.Errors, error => error.PropertyName == "Password");
    }

    [Fact]
    public void Login_DoesNotValidateEmailFormat_SoErrorsStayGeneric() =>
        Assert.True(new LoginRequestValidator().Validate(new LoginRequest("cualquier-cosa", "x")).IsValid);
}
