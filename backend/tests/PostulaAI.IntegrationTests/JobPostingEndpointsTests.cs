using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PostulaAI.Api.Modules.AuditLogs.Domain;
using PostulaAI.Api.Modules.JobPostings.Contracts;
using PostulaAI.Api.Modules.JobPostings.Domain;
using PostulaAI.IntegrationTests.Infrastructure;
using static PostulaAI.IntegrationTests.Infrastructure.ApiTestClient;

namespace PostulaAI.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public sealed class JobPostingEndpointsTests(DatabaseApiFactory factory)
{
    private const string BasePath = "/api/v1/job-postings";

    private static JobPostingRequest Sample(string title = "Desarrolladora backend .NET", string? company = "Empresa Ficticia") => new(
        title,
        company,
        "Bogotá",
        WorkMode.Hybrid,
        "Requisitos: 3 años con C# y ASP.NET Core, PostgreSQL. Deseable: Docker y Azure. Inglés intermedio.",
        "https://empleos.example.test/vacante/123");

    private static async Task<JobPostingResponse> CreateAsync(HttpClient client, TestSession session, JobPostingRequest request)
    {
        using var response = await client.SendAsync(WithBearer(HttpMethod.Post, BasePath, session.Auth.AccessToken, request));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JobPostingResponse>(Json);
        return created ?? throw new InvalidOperationException("Empty create response.");
    }

    private static async Task<PagedResponse<JobPostingSummary>> ListAsync(HttpClient client, TestSession session, string query = "")
    {
        using var response = await client.SendAsync(WithBearer(HttpMethod.Get, BasePath + query, session.Auth.AccessToken));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<JobPostingSummary>>(Json);
        return page ?? throw new InvalidOperationException("Empty list response.");
    }

    [Fact]
    public async Task Endpoints_RequireAuthentication()
    {
        using var client = factory.CreateApiClient();

        using var list = await client.GetAsync(new Uri(BasePath, UriKind.Relative));
        using var create = await client.PostAsJsonAsync(new Uri(BasePath, UriKind.Relative), Sample(), Json);

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsLocationAndFullPosting_ThenGetReturnsIt()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);

        using var response = await client.SendAsync(WithBearer(HttpMethod.Post, BasePath, session.Auth.AccessToken, Sample()));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JobPostingResponse>(Json);
        Assert.NotNull(created);
        Assert.EndsWith($"{BasePath}/{created.Id}", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"workMode\":\"hybrid\"", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        using var get = await client.SendAsync(WithBearer(HttpMethod.Get, $"{BasePath}/{created.Id}", session.Auth.AccessToken));
        var fetched = await get.Content.ReadFromJsonAsync<JobPostingResponse>(Json);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task Update_ChangesFields_AndDelete_RemovesPosting()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);
        var created = await CreateAsync(client, session, Sample());

        var changes = Sample("Ingeniera de software", company: null) with { WorkMode = WorkMode.Remote, SourceUrl = null };
        using var put = await client.SendAsync(WithBearer(HttpMethod.Put, $"{BasePath}/{created.Id}", session.Auth.AccessToken, changes));
        var updated = await put.Content.ReadFromJsonAsync<JobPostingResponse>(Json);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("Ingeniera de software", updated.Title);
        Assert.Null(updated.Company);
        Assert.Equal(WorkMode.Remote, updated.WorkMode);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);

        using var delete = await client.SendAsync(WithBearer(HttpMethod.Delete, $"{BasePath}/{created.Id}", session.Auth.AccessToken));
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        using var getAfter = await client.SendAsync(WithBearer(HttpMethod.Get, $"{BasePath}/{created.Id}", session.Auth.AccessToken));
        Assert.Equal(HttpStatusCode.NotFound, getAfter.StatusCode);
        Assert.Equal("job_posting_not_found", (await ReadProblemAsync(getAfter)).GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task OtherUsersPosting_IsInvisible_AndCannotBeModifiedOrDeleted()
    {
        using var client = factory.CreateApiClient();
        var owner = await RegisterAsync(client);
        var intruder = await RegisterAsync(client);
        var posting = await CreateAsync(client, owner, Sample());
        var path = $"{BasePath}/{posting.Id}";

        using var get = await client.SendAsync(WithBearer(HttpMethod.Get, path, intruder.Auth.AccessToken));
        using var put = await client.SendAsync(WithBearer(HttpMethod.Put, path, intruder.Auth.AccessToken, Sample("Modificada")));
        using var delete = await client.SendAsync(WithBearer(HttpMethod.Delete, path, intruder.Auth.AccessToken));

        // 404, not 403: the intruder cannot even learn that the id exists.
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Empty((await ListAsync(client, intruder)).Items);

        using var ownerGet = await client.SendAsync(WithBearer(HttpMethod.Get, path, owner.Auth.AccessToken));
        var unchanged = await ownerGet.Content.ReadFromJsonAsync<JobPostingResponse>(Json);
        Assert.Equal(HttpStatusCode.OK, ownerGet.StatusCode);
        Assert.Equal(posting.Title, unchanged?.Title);
    }

    [Fact]
    public async Task List_IsNewestFirst_Paginated_AndExcludesDescription()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);
        for (var i = 1; i <= 3; i++)
        {
            await CreateAsync(client, session, Sample($"Vacante {i}"));
        }

        var firstPage = await ListAsync(client, session, "?page=1&pageSize=2");
        var secondPage = await ListAsync(client, session, "?page=2&pageSize=2");

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(new[] { "Vacante 3", "Vacante 2" }, firstPage.Items.Select(item => item.Title));
        Assert.Equal(new[] { "Vacante 1" }, secondPage.Items.Select(item => item.Title));

        using var raw = await client.SendAsync(WithBearer(HttpMethod.Get, BasePath, session.Auth.AccessToken));
        Assert.DoesNotContain("\"description\"", await raw.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task List_SearchesTitleAndCompany_CaseInsensitive_TreatingWildcardsLiterally()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);
        await CreateAsync(client, session, Sample("Analista de Datos", company: "Consultora Norte"));
        await CreateAsync(client, session, Sample("Desarrolladora Backend", company: "Banco Sur"));
        await CreateAsync(client, session, Sample("Soporte 100% remoto", company: null));

        Assert.Equal(new[] { "Analista de Datos" }, (await ListAsync(client, session, "?search=datos")).Items.Select(item => item.Title));
        Assert.Equal(new[] { "Desarrolladora Backend" }, (await ListAsync(client, session, "?search=BANCO")).Items.Select(item => item.Title));
        Assert.Equal(new[] { "Soporte 100% remoto" }, (await ListAsync(client, session, "?search=100%25")).Items.Select(item => item.Title));
        Assert.Empty((await ListAsync(client, session, "?search=%25%25")).Items);
    }

    [Fact]
    public async Task InvalidInput_ReturnsFieldErrors()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);
        var invalid = Sample() with { Title = " ", Description = "Muy corta", SourceUrl = "javascript:alert(1)" };

        using var create = await client.SendAsync(WithBearer(HttpMethod.Post, BasePath, session.Auth.AccessToken, invalid));
        using var badPaging = await client.SendAsync(WithBearer(HttpMethod.Get, $"{BasePath}?pageSize=500", session.Auth.AccessToken));

        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        var errors = (await ReadProblemAsync(create)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("description", out _));
        Assert.True(errors.TryGetProperty("sourceUrl", out _));
        Assert.Equal(HttpStatusCode.BadRequest, badPaging.StatusCode);
    }

    [Fact]
    public async Task Mutations_AreAudited_WithoutPostingContent()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);
        var posting = await CreateAsync(client, session, Sample());
        using var delete = await client.SendAsync(WithBearer(HttpMethod.Delete, $"{BasePath}/{posting.Id}", session.Auth.AccessToken));

        var actions = await factory.WithDbAsync(db => db.AuditLogs
            .Where(log => log.EntityId == posting.Id)
            .Select(log => log.Action)
            .ToListAsync());

        Assert.Contains(AuditActions.JobPostingCreated, actions);
        Assert.Contains(AuditActions.JobPostingDeleted, actions);
    }

    [Fact]
    public async Task NewPosting_StartsAsSaved()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);

        var posting = await CreateAsync(client, session, Sample());

        Assert.Equal(ApplicationStatus.Saved, posting.Application.Status);
        Assert.Null(posting.Application.AppliedOn);
    }

    [Fact]
    public async Task UpdateApplication_StoresWhatWasSent_IsAudited_AndFiltersTheList()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);
        var applied = await CreateAsync(client, session, Sample("Aplicada"));
        await CreateAsync(client, session, Sample("Solo guardada"));
        var request = new ApplicationRequest(
            ApplicationStatus.Interview, new DateOnly(2026, 10, 30), "  Entrevista el lunes  ", "Resumen que envié.", "Carta que envié.");

        using var put = await client.SendAsync(
            WithBearer(HttpMethod.Put, $"{BasePath}/{applied.Id}/application", session.Auth.AccessToken, request));
        var updated = await put.Content.ReadFromJsonAsync<JobPostingResponse>(Json);

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal(ApplicationStatus.Interview, updated.Application.Status);
        Assert.Equal(new DateOnly(2026, 10, 30), updated.Application.AppliedOn);
        Assert.Equal("Entrevista el lunes", updated.Application.Notes);
        Assert.Equal("Carta que envié.", updated.Application.SubmittedCoverLetter);
        Assert.NotNull(updated.Application.UpdatedAt);

        var interviews = await ListAsync(client, session, "?status=interview");
        var saved = await ListAsync(client, session, "?status=saved");
        Assert.Equal(new[] { "Aplicada" }, interviews.Items.Select(item => item.Title));
        Assert.Equal(new DateOnly(2026, 10, 30), interviews.Items[0].AppliedOn);
        Assert.Equal(new[] { "Solo guardada" }, saved.Items.Select(item => item.Title));

        var auditCount = await factory.WithDbAsync(db => db.AuditLogs.AsNoTracking()
            .CountAsync(log => log.EntityId == applied.Id && log.Action == AuditActions.JobPostingApplicationUpdated));
        Assert.Equal(1, auditCount);
    }

    [Fact]
    public async Task UpdateApplication_RequiresADate_WhenTheUserApplied()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);
        var posting = await CreateAsync(client, session, Sample());

        using var put = await client.SendAsync(WithBearer(
            HttpMethod.Put,
            $"{BasePath}/{posting.Id}/application",
            session.Auth.AccessToken,
            new ApplicationRequest(ApplicationStatus.Applied, null, null, null, null)));

        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
        Assert.Contains("appliedOn", await put.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateApplication_OnAnotherUsersPosting_Returns404AndChangesNothing()
    {
        using var client = factory.CreateApiClient();
        var owner = await RegisterAsync(client);
        var intruder = await RegisterAsync(client);
        var posting = await CreateAsync(client, owner, Sample());

        using var put = await client.SendAsync(WithBearer(
            HttpMethod.Put,
            $"{BasePath}/{posting.Id}/application",
            intruder.Auth.AccessToken,
            new ApplicationRequest(ApplicationStatus.Rejected, new DateOnly(2026, 10, 1), null, null, null)));

        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        using var ownerGet = await client.SendAsync(WithBearer(HttpMethod.Get, $"{BasePath}/{posting.Id}", owner.Auth.AccessToken));
        var unchanged = await ownerGet.Content.ReadFromJsonAsync<JobPostingResponse>(Json);
        Assert.Equal(ApplicationStatus.Saved, unchanged?.Application.Status);
    }

    [Fact]
    public async Task Model_HasNoChangesMissingFromMigrations()
    {
        var pending = await factory.WithDbAsync(db => Task.FromResult(db.Database.HasPendingModelChanges()));

        Assert.False(pending, "Run `dotnet ef migrations add` for the model changes.");
    }
}
