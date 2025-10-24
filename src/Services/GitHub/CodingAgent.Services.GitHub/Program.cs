using Octokit;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Configure Octokit GitHubClient
var githubToken = builder.Configuration["GitHub:Token"];
var productHeaderValue = new ProductHeaderValue("CodingAgent", "2.0.0");

builder.Services.AddSingleton<IGitHubClient>(sp =>
{
    var client = new GitHubClient(productHeaderValue);
    if (!string.IsNullOrEmpty(githubToken))
    {
        client.Credentials = new Credentials(githubToken);
    }
    return client;
});

// Health checks
builder.Services.AddHealthChecks();

// OpenTelemetry configuration
var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "CodingAgent.Services.GitHub";
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName, serviceVersion: "2.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();

// Map health endpoint
app.MapHealthChecks("/health");

// Map ping endpoint
app.MapGet("/ping", () => Results.Ok(new
{
    service = "GitHub Service",
    version = "2.0.0",
    status = "healthy",
    timestamp = DateTime.UtcNow
}))
.WithName("Ping")
.WithTags("Health");

// Map Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

app.Run();
