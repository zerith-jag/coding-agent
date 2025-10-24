using CodingAgent.Services.Orchestration.Api.Endpoints;
using CodingAgent.Services.Orchestration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("OrchestrationDb")
    ?? throw new InvalidOperationException("OrchestrationDb connection string is required");

builder.Services.AddDbContext<OrchestrationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrchestrationDbContext>()
    .AddNpgSql(
    connectionString,
        name: "postgresql",
        tags: new[] { "db", "ready" });

// OpenTelemetry configuration
var serviceName = "CodingAgent.Services.Orchestration";
var serviceVersion = "2.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(options =>
        {
            // Default OTLP endpoint (will be configured via appsettings for production)
            options.Endpoint = new Uri(
                builder.Configuration["OpenTelemetry:OtlpEndpoint"]
                ?? "http://localhost:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

// API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "orchestration";
    config.Title = "Orchestration Service API";
    config.Version = "v2.0";
});

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "Orchestration Service API";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
    });
}

// Map endpoints
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");
app.MapTaskEndpoints();

// Root endpoint
app.MapGet("/", () => Results.Ok(new
{
    service = "Orchestration Service",
    version = "2.0.0",
    documentation = "/swagger"
}))
.WithName("Root")
.ExcludeFromDescription();

// Ensure database schema exists (dev/test convenience)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrchestrationDbContext>();
    try
    {
        await db.Database.EnsureCreatedAsync();
    }
    catch
    {
        // Ignore schema creation failures in scenarios where DB isn't reachable
    }
}

await app.RunAsync();
