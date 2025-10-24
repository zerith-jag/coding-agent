using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);

// Health checks
builder.Services.AddHealthChecks();

// YARP reverse proxy from configuration
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Map health endpoint
app.MapHealthChecks("/health");

// Map reverse proxy
app.MapReverseProxy();

app.Run();
