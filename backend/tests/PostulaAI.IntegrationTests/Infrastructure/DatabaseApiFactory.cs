using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PostulaAI.AI.Clients;
using Npgsql;
using PostulaAI.Api.Shared.Persistence;

namespace PostulaAI.IntegrationTests.Infrastructure;

/// <summary>
/// API host backed by a real PostgreSQL database created for this test run and dropped afterwards.
/// Requires the environment variable ConnectionStrings__Postgres pointing at a server where the user can create databases
/// (CI provides it; locally: docker compose up -d postgres).
/// </summary>
public sealed class DatabaseApiFactory : TestApiFactory, IAsyncLifetime
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
            return settings;
        }
    }

    public const int AnalysisLimit = 3;

    /// <summary>Stands in for Claude in every database-backed test. Never calls the real API.</summary>
    public ScriptedAIClient AI { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services => services.AddSingleton<IAIClient>(AI));
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
