using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Primitives;
using HealthChecks.UI.Client;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Context;
using System.Diagnostics;
using System.Text;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;

var builder = WebApplication.CreateBuilder(args);

// Bootstrap Serilog early for structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .CreateLogger();

builder.Host.UseSerilog();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Authentication:Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured");
var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();

// CORS configuration
var allowedOrigins = builder.Configuration.GetSection("Frontend:Origins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
              .WithExposedHeaders("X-Correlation-Id")
              .AllowCredentials();
    });
});

// Health checks (liveness/readiness)
builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

// OpenTelemetry: Traces + Metrics
builder.Services.AddOpenTelemetry()
    .ConfigureResource(rb => rb.AddService(
        serviceName: "CodingAgent.Gateway",
        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            var endpoint = builder.Configuration["OpenTelemetry:Endpoint"];
            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                options.Endpoint = new Uri(endpoint);
            }
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

// YARP reverse proxy from configuration
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Configure default HTTP client with Polly resilience policies
// YARP uses HttpClient internally, so these policies apply to proxied requests
builder.Services.AddHttpClient(string.Empty)
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
        MaxConnectionsPerServer = 100
    })
    .AddTransientHttpErrorPolicy(policyBuilder =>
        policyBuilder.WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt =>
            {
                // Exponential backoff with jitter: 2^attempt seconds + random 0-1000ms
                var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
                return baseDelay + jitter;
            },
            onRetry: (outcome, timespan, retryAttempt) =>
            {
                Log.Warning(
                    "HTTP retry {RetryAttempt}/3. Waiting {RetryDelayMs}ms. Reason: {Reason}",
                    retryAttempt,
                    (int)timespan.TotalMilliseconds,
                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown");
            }))
    .AddTransientHttpErrorPolicy(policyBuilder =>
        policyBuilder.CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, breakDelay) =>
            {
                Log.Error(
                    "Circuit breaker OPENED for {BreakDurationSeconds}s. Reason: {Reason}",
                    (int)breakDelay.TotalSeconds,
                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown");
            },
            onReset: () =>
            {
                Log.Information("Circuit breaker RESET. Normal operation resumed.");
            },
            onHalfOpen: () =>
            {
                Log.Information("Circuit breaker HALF-OPEN. Testing downstream health.");
            }))
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

var app = builder.Build();

// Apply CORS policy
app.UseCors("FrontendCors");

// Serilog request logging
app.UseSerilogRequestLogging();

// Correlation ID middleware
app.Use(async (context, next) =>
{
    const string HeaderName = "X-Correlation-Id";
    if (!context.Request.Headers.TryGetValue(HeaderName, out var value) || StringValues.IsNullOrEmpty(value))
    {
        value = Guid.NewGuid().ToString();
    }
    var correlationId = value.ToString();

    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        context.Response.Headers[HeaderName] = correlationId;
        // Enrich current Activity and Serilog scope
        Activity.Current?.SetTag("correlation.id", correlationId);
        await next.Invoke();
    }
});

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Expose Prometheus scraping endpoint
app.MapPrometheusScrapingEndpoint("/metrics");

// Map health endpoints (allow anonymous access)
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

// Map reverse proxy (requires authentication)
app.MapReverseProxy().RequireAuthorization();

await app.RunAsync();
