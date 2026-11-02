using System.Net;
using System.Net.Http.Json;
using PostulaAI.Api.Modules.ProfessionalProfiles.Contracts;
using PostulaAI.Api.Modules.ProfessionalProfiles.Domain;
using PostulaAI.IntegrationTests.Infrastructure;
using static PostulaAI.IntegrationTests.Infrastructure.ApiTestClient;

namespace PostulaAI.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public sealed class ProfileEndpointsTests(DatabaseApiFactory factory)
{
    private const string ProfilePath = "/api/v1/profiles/me";

    internal static ProfileRequest SampleProfile(string fullName = "Laura Gómez") => new(
        FullName: fullName,
        Headline: "Desarrolladora backend .NET",
        Summary: "Cuatro años construyendo APIs REST.",
        Links: new ProfileLinksDto("https://www.linkedin.com/in/ejemplo", null, "https://ejemplo.dev"),
        Experiences:
        [
            new WorkExperienceDto("Empresa Ficticia S.A.", "Desarrolladora backend", "Bogotá", new DateOnly(2022, 3, 1), null, true,
                "APIs para pagos.", ["Reduje la latencia p95 un 30 %"], ["C#", "ASP.NET Core", "PostgreSQL"]),
            new WorkExperienceDto("Startup Inventada", "Desarrolladora junior", null, new DateOnly(2020, 1, 1), new DateOnly(2022, 2, 1), false,
                null, [], ["C#"]),
        ],
        Education: [new EducationDto("Universidad Ejemplo", "Ingeniería de Sistemas", null, new DateOnly(2015, 2, 1), new DateOnly(2020, 12, 1), EducationStatus.Completed)],
        Projects: [new ProjectDto("Gestor de turnos", "Proyecto personal", ["React", "TypeScript"], "https://github.com/ejemplo/turnos")],
        Certifications: [new CertificationDto("Certificación Ficticia de Nube", "Entidad Ejemplo", new DateOnly(2023, 6, 1), null)],
        TechnicalSkills: ["C#", "SQL", "Docker"],
        SoftSkills: ["Comunicación"],
        Languages: [new LanguageDto("Español", LanguageProficiency.Native), new LanguageDto("Inglés", LanguageProficiency.Intermediate)]);

    [Fact]
    public async Task Profile_RequiresAuthentication()
    {
        using var client = factory.CreateApiClient();

        using var get = await client.GetAsync(new Uri(ProfilePath, UriKind.Relative));
        using var put = await client.PutAsJsonAsync(new Uri(ProfilePath, UriKind.Relative), SampleProfile(), Json);

        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, put.StatusCode);
    }

    [Fact]
    public async Task Get_BeforeCreating_Returns404ProfileNotFound()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);

        using var response = await client.SendAsync(WithBearer(HttpMethod.Get, ProfilePath, session.Auth.AccessToken));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("profile_not_found", (await ReadProblemAsync(response)).GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Put_CreatesProfile_AndGetReturnsSameContent()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);
        var request = SampleProfile();

        using var put = await client.SendAsync(WithBearer(HttpMethod.Put, ProfilePath, session.Auth.AccessToken, request));
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        using var get = await client.SendAsync(WithBearer(HttpMethod.Get, ProfilePath, session.Auth.AccessToken));
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var profile = await get.Content.ReadFromJsonAsync<ProfileResponse>(Json);

        Assert.NotNull(profile);
        Assert.Equal(request.FullName, profile.FullName);
        Assert.Equal(request.Links, profile.Links);
        Assert.Equal(new[] { "Empresa Ficticia S.A.", "Startup Inventada" }, profile.Experiences.Select(item => item.Company));
        Assert.Null(profile.Experiences[0].EndDate);
        Assert.Equal(new[] { "C#", "ASP.NET Core", "PostgreSQL" }, profile.Experiences[0].Technologies);
        Assert.Equal(new[] { "C#", "SQL", "Docker" }, profile.TechnicalSkills);
        Assert.Equal(new[] { "Comunicación" }, profile.SoftSkills);
        Assert.Equal(EducationStatus.Completed, Assert.Single(profile.Education).Status);
        Assert.Equal(LanguageProficiency.Native, profile.Languages[0].Proficiency);
    }

    [Fact]
    public async Task Put_SerializesEnumsAsSnakeCase()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);
        var request = SampleProfile() with
        {
            Education = [new EducationDto("Universidad Ejemplo", "Maestría", null, null, null, EducationStatus.InProgress)],
        };

        using var put = await client.SendAsync(WithBearer(HttpMethod.Put, ProfilePath, session.Auth.AccessToken, request));
        var json = await put.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Contains("\"status\":\"in_progress\"", json, StringComparison.Ordinal);
        Assert.Contains("\"proficiency\":\"native\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Put_ReplacesCollections()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);
        using var first = await client.SendAsync(WithBearer(HttpMethod.Put, ProfilePath, session.Auth.AccessToken, SampleProfile()));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var reduced = SampleProfile() with { Experiences = [], Projects = [], TechnicalSkills = ["Go"] };
        using var second = await client.SendAsync(WithBearer(HttpMethod.Put, ProfilePath, session.Auth.AccessToken, reduced));
        var profile = await second.Content.ReadFromJsonAsync<ProfileResponse>(Json);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.NotNull(profile);
        Assert.Empty(profile.Experiences);
        Assert.Empty(profile.Projects);
        Assert.Equal(new[] { "Go" }, profile.TechnicalSkills);
    }

    [Fact]
    public async Task Profiles_AreIsolatedBetweenUsers()
    {
        using var client = factory.CreateApiClient();
        var userA = await RegisterAsync(client);
        var userB = await RegisterAsync(client);

        using var putA = await client.SendAsync(WithBearer(HttpMethod.Put, ProfilePath, userA.Auth.AccessToken, SampleProfile("Usuario A")));
        Assert.Equal(HttpStatusCode.OK, putA.StatusCode);

        // B cannot see A's profile through the only profile route.
        using var getB = await client.SendAsync(WithBearer(HttpMethod.Get, ProfilePath, userB.Auth.AccessToken));
        Assert.Equal(HttpStatusCode.NotFound, getB.StatusCode);

        // B writing its own profile never touches A's.
        using var putB = await client.SendAsync(WithBearer(HttpMethod.Put, ProfilePath, userB.Auth.AccessToken, SampleProfile("Usuario B") with { Experiences = [] }));
        Assert.Equal(HttpStatusCode.OK, putB.StatusCode);

        using var getA = await client.SendAsync(WithBearer(HttpMethod.Get, ProfilePath, userA.Auth.AccessToken));
        var profileA = await getA.Content.ReadFromJsonAsync<ProfileResponse>(Json);
        Assert.NotNull(profileA);
        Assert.Equal("Usuario A", profileA.FullName);
        Assert.Equal(2, profileA.Experiences.Count);
    }

    [Fact]
    public async Task Put_InvalidProfile_ReturnsFieldErrorsWithCamelCasePaths()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);
        var invalid = SampleProfile() with
        {
            FullName = new string('a', 151),
            Links = new ProfileLinksDto("javascript:alert(1)", null, null),
            Experiences =
            [
                new WorkExperienceDto("Empresa", "Cargo", null, new DateOnly(2023, 1, 1), new DateOnly(2022, 1, 1), false, null, [], []),
                new WorkExperienceDto("Empresa", "Cargo", null, new DateOnly(2023, 1, 1), new DateOnly(2024, 1, 1), true, null, [], []),
            ],
            TechnicalSkills = ["C#", "c#"],
        };

        using var response = await client.SendAsync(WithBearer(HttpMethod.Put, ProfilePath, session.Auth.AccessToken, invalid));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadProblemAsync(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("fullName", out _));
        Assert.True(errors.TryGetProperty("links.linkedInUrl", out _));
        Assert.True(errors.TryGetProperty("experiences[0].endDate", out _));
        Assert.True(errors.TryGetProperty("experiences[1].endDate", out _));
        Assert.True(errors.TryGetProperty("technicalSkills", out _));
    }
}
