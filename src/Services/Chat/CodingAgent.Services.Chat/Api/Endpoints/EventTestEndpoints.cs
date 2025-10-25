using CodingAgent.SharedKernel.Domain.Events;
using MassTransit;

namespace CodingAgent.Services.Chat.Api.Endpoints;

public static class EventTestEndpoints
{
    public static void MapEventTestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/events/test").WithTags("Events");

        group.MapPost("/message-sent", async (IPublishEndpoint publish, ILoggerFactory lf) =>
        {
            var logger = lf.CreateLogger("EventTest");
            var @event = new MessageSentEvent
            {
                ConversationId = Guid.NewGuid(),
                MessageId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Content = "Hello from Chat test endpoint",
                Role = "User",
                SentAt = DateTime.UtcNow
            };

            await publish.Publish(@event);
            logger.LogInformation("[Chat] Published MessageSentEvent: ConversationId={ConversationId}, MessageId={MessageId}", @event.ConversationId, @event.MessageId);
            return Results.Accepted(value: new { published = true, @event.MessageId });
        })
        .WithName("PublishMessageSentEventTest");
    }
}
