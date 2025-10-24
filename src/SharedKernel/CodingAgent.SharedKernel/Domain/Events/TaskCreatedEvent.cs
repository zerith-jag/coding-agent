using CodingAgent.SharedKernel.Domain.ValueObjects;

namespace CodingAgent.SharedKernel.Domain.Events;

/// <summary>
/// Event published when a new task is created.
/// </summary>
public record TaskCreatedEvent : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the unique identifier of the created task.
    /// </summary>
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Gets the user identifier who created the task.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets the task description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the task type.
    /// </summary>
    public TaskType? TaskType { get; init; }

    /// <summary>
    /// Gets the task complexity.
    /// </summary>
    public TaskComplexity? Complexity { get; init; }
}
