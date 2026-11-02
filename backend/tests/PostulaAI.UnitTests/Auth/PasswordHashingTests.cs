using Microsoft.AspNetCore.Identity;
using PostulaAI.Api.Modules.Users.Domain;

namespace PostulaAI.UnitTests.Auth;

/// <summary>Guards the hashing configuration used for user passwords (ASP.NET Core Identity v3 / PBKDF2).</summary>
public sealed class PasswordHashingTests
{
    private readonly PasswordHasher<User> hasher = new();
    private readonly User user = new() { Email = "persona@example.test", PasswordHash = string.Empty, DisplayName = "Persona" };

    [Fact]
    public void Hash_IsSaltedAndNeverThePlainPassword()
    {
        const string password = "una-contraseña-segura";

        var first = hasher.HashPassword(user, password);
        var second = hasher.HashPassword(user, password);

        Assert.NotEqual(password, first);
        Assert.NotEqual(first, second);
        Assert.Equal(0x01, Convert.FromBase64String(first)[0]);
    }

    [Fact]
    public void Verify_AcceptsCorrectPassword_AndRejectsWrongOne()
    {
        var hash = hasher.HashPassword(user, "una-contraseña-segura");

        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, hash, "una-contraseña-segura"));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, hash, "otra-contraseña"));
    }

    [Theory]
    [InlineData("  Persona@Example.TEST ", "persona@example.test")]
    [InlineData("ya@normalizado.test", "ya@normalizado.test")]
    public void NormalizeEmail_TrimsAndLowercases(string input, string expected) =>
        Assert.Equal(expected, User.NormalizeEmail(input));
}
