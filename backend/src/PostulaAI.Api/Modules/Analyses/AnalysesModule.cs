using Microsoft.Extensions.Options;
using PostulaAI.AI.Clients;
using PostulaAI.AI.Services;
using PostulaAI.Api.Modules.UsageLimits;

namespace PostulaAI.Api.Modules.Analyses;

public static class AnalysesModule
{
    public static IServiceCollection AddAnalysesModule(this IServiceCollection services)
    {
        services.AddOptions<ClaudeOptions>()
            .BindConfiguration(ClaudeOptions.SectionName)
            .PostConfigure(options =>
            {
                // docker-compose passes empty strings for unset variables; fall back to the defaults.
                if (string.IsNullOrWhiteSpace(options.Model))
                {
                    options.Model = new ClaudeOptions().Model;
                }
            });
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<ClaudeOptions>>().Value);
        services.AddHttpClient<ClaudeClient>((provider, http) =>
            http.Timeout = TimeSpan.FromSeconds(provider.GetRequiredService<ClaudeOptions>().TimeoutSeconds));
        services.AddTransient<IAIClient>(provider => provider.GetRequiredService<ClaudeClient>());

        services.AddOptions<JobAnalysisOptions>().BindConfiguration(JobAnalysisOptions.SectionName);
        services.AddScoped(provider => new JobAnalysisService(
            provider.GetRequiredService<IAIClient>(),
            provider.GetRequiredService<IOptions<JobAnalysisOptions>>().Value));

        services.AddOptions<AnalysisUsageOptions>().BindConfiguration(AnalysisUsageOptions.SectionName);
        services.AddScoped<UsageLimitService>();
        services.AddScoped<AiBudgetService>();
        services.AddOptions<AiDailyBudgetOptions>().BindConfiguration(AiDailyBudgetOptions.SectionName);
        services.AddScoped<AnalysisService>();
        return services;
    }
}
