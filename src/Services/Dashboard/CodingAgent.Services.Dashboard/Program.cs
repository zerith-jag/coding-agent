using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Health checks
builder.Services.AddHealthChecks();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("CodingAgent.Services.Dashboard", serviceVersion: "2.0.0"))
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

// Map health endpoint
app.MapHealthChecks("/health");

// Map metrics endpoint
app.MapPrometheusScrapingEndpoint();

// Ping endpoint
app.MapGet("/ping", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }))
    .WithName("Ping")
    .WithOpenApi();

app.Run();
