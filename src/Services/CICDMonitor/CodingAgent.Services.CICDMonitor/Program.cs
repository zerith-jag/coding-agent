using MassTransit;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RabbitMQ.Client;

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

// MassTransit + RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumers(typeof(Program).Assembly);

    x.UsingRabbitMq((context, cfg) =>
    {
        var cfgHostRaw = builder.Configuration["RabbitMQ:Host"];
        var cfgUserRaw = builder.Configuration["RabbitMQ:Username"];
        var cfgPassRaw = builder.Configuration["RabbitMQ:Password"];
        var isProd = builder.Environment.IsProduction();

        // Default only in non-production
        var host = isProd ? cfgHostRaw : (cfgHostRaw ?? "localhost");
        var username = isProd ? cfgUserRaw : (cfgUserRaw ?? "guest");
        var password = isProd ? cfgPassRaw : (cfgPassRaw ?? "guest");

        if (isProd && (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)))
        {
            throw new InvalidOperationException("RabbitMQ configuration (Host/Username/Password) is required in Production");
        }

        if (!string.IsNullOrWhiteSpace(host))
        {
            cfg.Host(host!, h =>
            {
                if (!string.IsNullOrWhiteSpace(username))
                    h.Username(username!);
                if (!string.IsNullOrWhiteSpace(password))
                    h.Password(password!);
            });
        }

        cfg.ConfigureEndpoints(context);
    });
});

// Add health checks
builder.Services.AddHealthChecks();

// RabbitMQ health check if configured (avoid credentials in URI)
var hcRabbitHost = builder.Configuration["RabbitMQ:Host"];
var hcRabbitUser = builder.Configuration["RabbitMQ:Username"];
var hcRabbitPass = builder.Configuration["RabbitMQ:Password"];
var rabbitConfigured = !string.IsNullOrWhiteSpace(hcRabbitHost) && !string.IsNullOrWhiteSpace(hcRabbitUser) && !string.IsNullOrWhiteSpace(hcRabbitPass);
if (rabbitConfigured)
{
    builder.Services.AddHealthChecks().AddRabbitMQ(_ => new ConnectionFactory
    {
        HostName = hcRabbitHost,
        UserName = hcRabbitUser,
        Password = hcRabbitPass,
        Port = 5672
    }, name: "rabbitmq");
}

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
