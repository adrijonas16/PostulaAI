namespace PostulaAI.Api.Modules.ProfessionalProfiles;

public static class ProfilesModule
{
    public static IServiceCollection AddProfilesModule(this IServiceCollection services)
    {
        services.AddScoped<ProfileService>();
        return services;
    }
}
