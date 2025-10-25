using CodingAgent.SharedKernel.Domain.Events;
using MassTransit;

namespace CodingAgent.Services.Orchestration.Infrastructure.Messaging.Consumers;

public class MessageSentEventConsumer : IConsumer<MessageSentEvent>
{
    private readonly ILogger<MessageSentEventConsumer> _logger;

    public MessageSentEventConsumer(ILogger<MessageSentEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<MessageSentEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "[Orchestration] Consumed MessageSentEvent: ConversationId={ConversationId}, MessageId={MessageId}, UserId={UserId}, Role={Role}",
            msg.ConversationId, msg.MessageId, msg.UserId, msg.Role);
        return Task.CompletedTask;
    }
}
