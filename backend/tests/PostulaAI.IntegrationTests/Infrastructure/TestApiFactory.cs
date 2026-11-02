using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace PostulaAI.IntegrationTests.Infrastructure;

/// <summary>
/// API host for tests that do not need a database. A random JWT key is generated per run (never a repository secret).
/// The connection string points to an unused port: any accidental database access fails loudly.
/// </summary>
public class TestApiFactory : WebApplicationFactory<Program>
{
    protected virtual IDictionary<string, string?> Settings => new Dictionary<string, string?>
    {
        ["Jwt:SigningKey"] = TestSigningKey,
        ["ConnectionStrings:Postgres"] = "Host=127.0.0.1;Port=1;Database=unused;Username=unused",
        ["Database:ApplyMigrationsOnStartup"] = "false",
        ["RateLimiting:Auth:PermitLimit"] = "1000",
    };

    protected static string TestSigningKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = false,
        AllowAutoRedirect = false,
    });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(Settings));
    }
}

/// <summary>Same host with a tiny auth rate limit, to verify throttling.</summary>
public sealed class RateLimitedApiFactory : TestApiFactory
{
    public const int PermitLimit = 2;

    protected override IDictionary<string, string?> Settings
    {
        get
        {
            var settings = base.Settings;
            settings["RateLimiting:Auth:PermitLimit"] = PermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return settings;
        }
    }
}
