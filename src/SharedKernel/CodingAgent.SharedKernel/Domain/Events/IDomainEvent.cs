namespace CodingAgent.SharedKernel.Domain.Events;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something significant that happened in the domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier for the event.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    DateTime OccurredAt { get; }
}
