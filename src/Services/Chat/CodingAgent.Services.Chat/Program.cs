using CodingAgent.Services.Chat.Api.Endpoints;
using CodingAgent.Services.Chat.Api.Hubs;
using CodingAgent.Services.Chat.Infrastructure.Persistence;
using CodingAgent.SharedKernel.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Database configuration
builder.Services.AddDbContext<ChatDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ChatDb");
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        // Use PostgreSQL when a connection string is explicitly provided
        options.UseNpgsql(connectionString);
    }
    else if (builder.Environment.IsProduction())
    {
        // In Production, require explicit configuration
        throw new InvalidOperationException("ChatDb connection string is required in Production");
    }
    else
    {
        // Default to in-memory for development/testing when not configured
        options.UseInMemoryDatabase("ChatDb");
    }
});

// Redis Cache (optional - skip if not configured)
var redisConnection = builder.Configuration["Redis:Connection"];
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
    });
}

// SignalR
builder.Services.AddSignalR();

// MassTransit + RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumers(typeof(Program).Assembly);

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureRabbitMQHost(builder.Configuration, builder.Environment);
        cfg.ConfigureEndpoints(context);
    });
});

// Health Checks
var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddDbContextCheck<ChatDbContext>();

// Add Redis health check if configured
if (!string.IsNullOrEmpty(redisConnection))
{
    healthChecksBuilder.AddRedis(redisConnection, name: "redis");
}

// RabbitMQ health check if configured (only in Production to avoid dev package mismatches)
if (builder.Environment.IsProduction())
{
    healthChecksBuilder.AddRabbitMQHealthCheckIfConfigured(builder.Configuration);
}

// OpenTelemetry - Tracing and Metrics
var serviceName = "chat-service";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource(serviceName)
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

// CORS (for development)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Middleware
app.UseCors();

// Map endpoints
app.MapGet("/ping", () => Results.Ok(new { status = "healthy", service = "chat-service", timestamp = DateTime.UtcNow }))
    .WithName("Ping")
    .WithTags("Health")
    .Produces(StatusCodes.Status200OK);

app.MapConversationEndpoints();
app.MapEventTestEndpoints();

// SignalR hub
app.MapHub<ChatHub>("/hubs/chat");

// Health checks
app.MapHealthChecks("/health");

// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

// Apply EF Core migrations on startup when using a relational provider
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    await db.MigrateDatabaseIfRelationalAsync(
        app.Logger,
        isProduction: app.Environment.IsProduction());
}

await app.RunAsync();

// Expose Program class for WebApplicationFactory in tests
public partial class Program
{
    // Prevent instantiation
    protected Program() { }
}
