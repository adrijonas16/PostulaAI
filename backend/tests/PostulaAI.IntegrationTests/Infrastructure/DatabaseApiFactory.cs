using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PostulaAI.AI.Clients;
using Npgsql;
using PostulaAI.Api.Shared.Email;
using PostulaAI.Api.Shared.Persistence;

namespace PostulaAI.IntegrationTests.Infrastructure;

/// <summary>
/// API host backed by a real PostgreSQL database created for this test run and dropped afterwards.
/// Requires the environment variable ConnectionStrings__Postgres pointing at a server where the user can create databases
/// (CI provides it; locally: docker compose up -d postgres).
/// </summary>
public class DatabaseApiFactory : TestApiFactory, IAsyncLifetime
{
    private readonly string connectionString;

    public DatabaseApiFactory()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");
        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            throw new InvalidOperationException(
                "Integration tests need ConnectionStrings__Postgres (e.g. start PostgreSQL with `docker compose up -d postgres`).");
        }

        connectionString = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Database = $"postulaai_test_{Guid.NewGuid():N}",
        }.ConnectionString;
    }

    protected override IDictionary<string, string?> Settings
    {
        get
        {
            var settings = base.Settings;
            settings["ConnectionStrings:Postgres"] = connectionString;
            settings["Database:ApplyMigrationsOnStartup"] = "true";
            settings["UsageLimits:Analyses:MaxPerWindow"] = AnalysisLimit.ToString(System.Globalization.CultureInfo.InvariantCulture);
            // E-mail is "configured" (captured in memory) but not required, so most tests can use AI features right after registering.
            settings["Email:SmtpHost"] = "smtp.test";
            settings["Email:AppBaseUrl"] = "https://app.test";
            settings["Email:RequireVerification"] = "false";
            settings["UsageLimits:AiDaily:MaxRequests"] = "0";
            Customize(settings);
            return settings;
        }
    }

    public const int AnalysisLimit = 3;

    /// <summary>Stands in for Claude in every database-backed test. Never calls the real API.</summary>
    public ScriptedAIClient AI { get; } = new();

    /// <summary>E-mails the API "sent" during the run. Nothing leaves the process.</summary>
    public CapturingEmailSender Emails { get; } = new();

    protected virtual void Customize(IDictionary<string, string?> settings)
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IAIClient>(AI);
            services.AddSingleton<IEmailSender>(Emails);
        });
    }

    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync()
    {
        await using (var scope = Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeletedAsync();
        }

        await DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseApiFactory>
{
    public const string Name = "database";
}

/// <summary>Own database with e-mail verification enforced.</summary>
public sealed class EmailVerificationApiFactory : DatabaseApiFactory
{
    protected override void Customize(IDictionary<string, string?> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings["Email:RequireVerification"] = "true";
    }
}

[CollectionDefinition(Name)]
public sealed class EmailVerificationCollection : ICollectionFixture<EmailVerificationApiFactory>
{
    public const string Name = "email-verification";
}

/// <summary>Own database with a site-wide AI budget of <see cref="DailyBudget"/> requests.</summary>
public sealed class AiBudgetApiFactory : DatabaseApiFactory
{
    public const int DailyBudget = 2;

    protected override void Customize(IDictionary<string, string?> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings["UsageLimits:AiDaily:MaxRequests"] = DailyBudget.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}

[CollectionDefinition(Name)]
public sealed class AiBudgetCollection : ICollectionFixture<AiBudgetApiFactory>
{
    public const string Name = "ai-budget";
}
