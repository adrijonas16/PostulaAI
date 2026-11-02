using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using PostulaAI.AI.Clients;
using PostulaAI.AI.Services;
using PostulaAI.Api.Modules.ProfessionalProfiles.Import;
using PostulaAI.Api.Shared.Security;

namespace PostulaAI.Api.Modules.ProfessionalProfiles;

/// <summary>Bound from "RateLimiting:CvImport". Each import calls the AI provider, so it is limited per user.</summary>
public sealed class CvImportRateLimitOptions
{
    public const string SectionName = "RateLimiting:CvImport";
    public const string PolicyName = "cv-import";

    public int PermitLimit { get; set; } = 5;

    public int WindowMinutes { get; set; } = 60;
}

public static class ProfilesModule
{
    public static IServiceCollection AddProfilesModule(this IServiceCollection services)
    {
        services.AddScoped<ProfileService>();

        services.AddOptions<CvExtractionOptions>().BindConfiguration(CvExtractionOptions.SectionName);
        services.AddScoped(provider => new CvExtractionService(
            provider.GetRequiredService<IAIClient>(),
            provider.GetRequiredService<IOptions<CvExtractionOptions>>().Value));
        services.AddScoped<CvImportService>();

        services.AddOptions<CvImportRateLimitOptions>().BindConfiguration(CvImportRateLimitOptions.SectionName);
        services.Configure<RateLimiterOptions>(options =>
            options.AddPolicy(CvImportRateLimitOptions.PolicyName, httpContext =>
            {
                var limits = httpContext.RequestServices.GetRequiredService<IOptions<CvImportRateLimitOptions>>().Value;
                var partitionKey = httpContext.User.FindFirst(ClaimsPrincipalExtensions.SubjectClaim)?.Value
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.PermitLimit,
                    Window = TimeSpan.FromMinutes(limits.WindowMinutes),
                    QueueLimit = 0,
                });
            }));
        return services;
    }
}
