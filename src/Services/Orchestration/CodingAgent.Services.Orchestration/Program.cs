using CodingAgent.Services.Orchestration.Api.Endpoints;
using CodingAgent.Services.Orchestration.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RabbitMQ.Client;

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

// RabbitMQ health check if configured (avoid embedding credentials in URI)
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
app.MapEventTestEndpoints();

// Root endpoint
app.MapGet("/", () => Results.Ok(new
{
    service = "Orchestration Service",
    version = "2.0.0",
    documentation = "/swagger"
}))
.WithName("Root")
.ExcludeFromDescription();

// Apply migrations when using a relational provider
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrchestrationDbContext>();
    try
    {
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync();
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to apply OrchestrationDb migrations on startup");
        if (app.Environment.IsProduction())
        {
            throw;
        }
    }
}

await app.RunAsync();
