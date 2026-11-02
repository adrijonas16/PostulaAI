using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using PostulaAI.Api.Shared.Errors;
using PostulaAI.Api.Shared.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Errors: RFC 9457 ProblemDetails with a trace id for correlation. No exception details leak to clients.
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// The connection string is resolved lazily so the app can start (and /health/live can answer)
// even when the database is not configured; /health/ready reports the database state.
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:Postgres' is not configured.");
    options.UseNpgsql(connectionString);
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(name: "postgres", tags: ["ready"]);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "PostulaAI API v1"));
}

app.UseCors();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.MapControllers();

await app.RunAsync();

/// <summary>Entry point; declared public so integration tests can use WebApplicationFactory&lt;Program&gt;.</summary>
public partial class Program;
