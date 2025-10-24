using Microsoft.AspNetCore.Mvc;

namespace CodingAgent.Services.Orchestration.Api.Endpoints;

/// <summary>
/// Task management endpoints for the Orchestration service.
/// </summary>
public static class TaskEndpoints
{
    /// <summary>
    /// Maps task-related endpoints.
    /// </summary>
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tasks").WithTags("Tasks");

        // Ping endpoint for health verification
        group.MapGet("/ping", () => Results.Ok(new
        {
            service = "Orchestration Service",
            version = "2.0.0",
            status = "healthy",
            timestamp = DateTime.UtcNow
        }))
        .WithName("PingTasks")
        .WithOpenApi()
        .Produces<object>(StatusCodes.Status200OK);
    }
}
