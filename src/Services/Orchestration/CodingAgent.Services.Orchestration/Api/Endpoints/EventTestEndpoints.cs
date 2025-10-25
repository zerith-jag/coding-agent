using CodingAgent.SharedKernel.Domain.Events;
using CodingAgent.SharedKernel.Domain.ValueObjects;
using MassTransit;

namespace CodingAgent.Services.Orchestration.Api.Endpoints;

public static class EventTestEndpoints
{
    public static void MapEventTestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/events/test").WithTags("Events");

        group.MapPost("/task-completed", async (IPublishEndpoint publish, ILoggerFactory lf) =>
        {
            var logger = lf.CreateLogger("EventTest");
            var @event = new TaskCompletedEvent
            {
                TaskId = Guid.NewGuid(),
                TaskType = TaskType.Feature,
                Complexity = TaskComplexity.Simple,
                Strategy = ExecutionStrategy.SingleShot,
                Success = true,
                TokensUsed = 1234,
                CostUsd = 0.12m,
                Duration = TimeSpan.FromSeconds(3)
            };

            await publish.Publish(@event);
            logger.LogInformation("[Orchestration] Published TaskCompletedEvent: TaskId={TaskId}", @event.TaskId);
            return Results.Accepted(value: new { published = true, @event.TaskId });
        })
        .WithName("PublishTaskCompletedEventTest");
    }
}
