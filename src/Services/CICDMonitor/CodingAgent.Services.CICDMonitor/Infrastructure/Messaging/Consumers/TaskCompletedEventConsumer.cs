using CodingAgent.SharedKernel.Domain.Events;
using MassTransit;

namespace CodingAgent.Services.CICDMonitor.Infrastructure.Messaging.Consumers;

public class TaskCompletedEventConsumer : IConsumer<TaskCompletedEvent>
{
    private readonly ILogger<TaskCompletedEventConsumer> _logger;

    public TaskCompletedEventConsumer(ILogger<TaskCompletedEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<TaskCompletedEvent> context)
    {
        var e = context.Message;
        _logger.LogInformation(
            "[CICDMonitor] Consumed TaskCompletedEvent: TaskId={TaskId}, Success={Success}, CostUsd={CostUsd}, Duration={Duration}",
            e.TaskId, e.Success, e.CostUsd, e.Duration);
        return Task.CompletedTask;
    }
}
