using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Health checks
builder.Services.AddHealthChecks();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("BrowserService", serviceVersion: "2.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            var endpoint = builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://jaeger:4317";
            options.Endpoint = new Uri(endpoint);
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();

// Health endpoint
app.MapHealthChecks("/health");

// Ping endpoint
app.MapGet("/ping", () => Results.Ok(new
{
    service = "BrowserService",
    status = "healthy",
    version = "2.0.0",
    timestamp = DateTime.UtcNow
}))
.WithName("Ping")
.WithTags("Health")
.Produces(StatusCodes.Status200OK);

// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

app.Run();

// Make Program accessible to test projects
public partial class Program { }
