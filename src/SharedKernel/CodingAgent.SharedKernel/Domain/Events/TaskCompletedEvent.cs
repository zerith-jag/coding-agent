using CodingAgent.SharedKernel.Domain.ValueObjects;

namespace CodingAgent.SharedKernel.Domain.Events;

/// <summary>
/// Event published when a task execution is completed.
/// </summary>
public record TaskCompletedEvent : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the unique identifier of the completed task.
    /// </summary>
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Gets the task type.
    /// </summary>
    public required TaskType TaskType { get; init; }

    /// <summary>
    /// Gets the task complexity.
    /// </summary>
    public required TaskComplexity Complexity { get; init; }

    /// <summary>
    /// Gets the execution strategy used.
    /// </summary>
    public required ExecutionStrategy Strategy { get; init; }

    /// <summary>
    /// Gets whether the task execution was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the number of tokens used during execution.
    /// </summary>
    public required int TokensUsed { get; init; }

    /// <summary>
    /// Gets the cost in USD for the execution.
    /// </summary>
    public required decimal CostUsd { get; init; }

    /// <summary>
    /// Gets the duration of the execution.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the error message if the task failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
