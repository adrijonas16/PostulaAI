using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using PostulaAI.AI.Clients;
using PostulaAI.Api.Modules.ProfessionalProfiles.Import;
using PostulaAI.IntegrationTests.Infrastructure;
using static PostulaAI.IntegrationTests.Infrastructure.ApiTestClient;

namespace PostulaAI.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public sealed class CvImportEndpointsTests : IDisposable
{
    private const string ImportPath = "/api/v1/profiles/me/import";
    private readonly DatabaseApiFactory factory;

    public CvImportEndpointsTests(DatabaseApiFactory factory)
    {
        this.factory = factory;
        factory.AI.Reset();
    }

    public void Dispose() => factory.AI.Reset();

    private static byte[] Docx(string text)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var writer = new StreamWriter(archive.CreateEntry("word/document.xml").Open(), new UTF8Encoding(false));
            writer.Write("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>");
            foreach (var line in text.Split('\n'))
            {
                writer.Write($"<w:p><w:r><w:t>{System.Security.SecurityElement.Escape(line)}</w:t></w:r></w:p>");
            }

            writer.Write("</w:body></w:document>");
        }

        return output.ToArray();
    }

    private static HttpRequestMessage Upload(string accessToken, byte[] content, string fileName = "cv.docx")
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", fileName);
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(ImportPath, UriKind.Relative)) { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private const string CvText = """
        Laura Gómez
        Desarrolladora backend · linkedin.com/in/laura
        Empresa Ficticia S.A. — Desarrolladora backend (marzo 2022 – actual), Bogotá
        - Reduje la latencia un 30 %
        Tecnologías: C#, PostgreSQL
        Universidad Ejemplo — Ingeniería de Sistemas (2015 – 2020)
        Idiomas: Inglés
        """;

    [Fact]
    public async Task Import_ReturnsAGroundedUnsavedDraft()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);

        using var response = await client.SendAsync(Upload(session.Auth.AccessToken, Docx(CvText)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CvImportResponse>(Json);
        Assert.NotNull(body);
        Assert.Equal("Laura Gómez", body.Draft.FullName);
        Assert.Equal(new DateOnly(2022, 3, 1), body.Draft.Experiences[0].StartDate);
        Assert.True(body.Draft.Experiences[0].IsCurrent);
        // Kubernetes and Terraform were "extracted" by the fake model but are not in the CV: they must be dropped.
        Assert.Equal(new[] { "C#" }, body.Draft.Experiences[0].Technologies);
        Assert.Equal(new[] { "C#", "PostgreSQL" }, body.Draft.TechnicalSkills);
        Assert.Null(body.Draft.Languages[0].Proficiency);
        Assert.Equal("Inglés", body.Draft.Languages[0].Language);
        Assert.Null(body.Draft.Links.LinkedInUrl);
        Assert.Contains(body.Warnings, warning => warning.Contains("«Terraform»", StringComparison.Ordinal));
        Assert.Contains(body.Warnings, warning => warning.Contains("«Kubernetes»", StringComparison.Ordinal));

        using var profile = await client.SendAsync(WithBearer(HttpMethod.Get, "/api/v1/profiles/me", session.Auth.AccessToken));
        Assert.Equal(HttpStatusCode.NotFound, profile.StatusCode);
    }

    [Fact]
    public async Task UnsupportedOrEmptyFiles_AreRejected_WithoutCallingTheAI()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);
        var calls = factory.AI.Calls;

        using var text = await client.SendAsync(Upload(session.Auth.AccessToken, Encoding.UTF8.GetBytes(CvText), "cv.pdf"));
        using var tiny = await client.SendAsync(Upload(session.Auth.AccessToken, Docx("Hola")));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, text.StatusCode);
        Assert.Equal("cv_unsupported_format", (await ReadProblemAsync(text)).GetProperty("errorCode").GetString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tiny.StatusCode);
        Assert.Equal("cv_no_text", (await ReadProblemAsync(tiny)).GetProperty("errorCode").GetString());
        Assert.Equal(calls, factory.AI.Calls);
    }

    [Fact]
    public async Task MissingApiKey_ExplainsItClearly()
    {
        using var client = factory.CreateApiClient();
        var session = await RegisterAsync(client);
        factory.AI.EnqueueError(AIProviderErrorKind.NotConfigured);

        using var response = await client.SendAsync(Upload(session.Auth.AccessToken, Docx(CvText)));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("ai_not_configured", (await ReadProblemAsync(response)).GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Import_RequiresAuthentication()
    {
        using var client = factory.CreateApiClient();
        var form = new MultipartFormDataContent { { new ByteArrayContent(Docx(CvText)), "file", "cv.docx" } };

        using var response = await client.PostAsync(new Uri(ImportPath, UriKind.Relative), form);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
