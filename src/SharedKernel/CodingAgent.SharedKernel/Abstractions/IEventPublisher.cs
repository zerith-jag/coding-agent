using CodingAgent.SharedKernel.Domain.Events;

namespace CodingAgent.SharedKernel.Abstractions;

/// <summary>
/// Interface for publishing domain events to a message bus.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a domain event to the message bus.
    /// </summary>
    /// <typeparam name="TEvent">The type of domain event.</typeparam>
    /// <param name="event">The domain event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;

    /// <summary>
    /// Publishes multiple domain events to the message bus.
    /// </summary>
    /// <typeparam name="TEvent">The type of domain events.</typeparam>
    /// <param name="events">The collection of domain events to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}
