using CodingAgent.Services.Chat.Api.Endpoints;
using CodingAgent.Services.Chat.Api.Hubs;
using CodingAgent.Services.Chat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Database configuration (stub - no actual connection yet)
builder.Services.AddDbContext<ChatDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ChatDb");
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        // Use in-memory for development/testing if no connection string
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

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ChatDbContext>();

// Add Redis health check if configured
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddHealthChecks()
        .AddRedis(redisConnection, name: "redis");
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

// SignalR hub
app.MapHub<ChatHub>("/hubs/chat");

// Health checks
app.MapHealthChecks("/health");

// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

// Ensure database schema exists (dev/test convenience)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
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

// Expose Program class for WebApplicationFactory in tests
public partial class Program
{
    // Prevent instantiation
    protected Program() { }
}
