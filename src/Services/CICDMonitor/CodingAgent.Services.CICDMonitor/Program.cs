using CodingAgent.SharedKernel.Infrastructure;
using MassTransit;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "CodingAgent.Services.CICDMonitor",
            serviceVersion: "2.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            var otlpEndpoint = builder.Configuration["OpenTelemetry:Endpoint"];
            if (!string.IsNullOrEmpty(otlpEndpoint))
            {
                options.Endpoint = new Uri(otlpEndpoint);
            }
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

// MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // TODO: Add consumers here when needed
    // x.AddConsumer<MyConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureRabbitMQHost(builder.Configuration, builder.Environment);
        cfg.ConfigureEndpoints(context);
    });
});

// Add health checks
var healthChecksBuilder = builder.Services.AddHealthChecks();

// RabbitMQ health check if configured (avoid credentials in URI)
healthChecksBuilder.AddRabbitMQHealthCheckIfConfigured(builder.Configuration);

var app = builder.Build();

// Map health endpoint
app.MapHealthChecks("/health");

// Map ping endpoint
app.MapGet("/ping", () => Results.Ok(new
{
    service = "CodingAgent.Services.CICDMonitor",
    version = "2.0.0",
    status = "healthy",
    timestamp = DateTime.UtcNow
}))
.WithName("Ping")
.WithTags("Health");

// Map Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

await app.RunAsync();
