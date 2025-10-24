namespace CodingAgent.SharedKernel.Domain.Events;

/// <summary>
/// Event published when a new conversation is created.
/// </summary>
public record ConversationCreatedEvent : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the unique identifier of the created conversation.
    /// </summary>
    public required Guid ConversationId { get; init; }

    /// <summary>
    /// Gets the user identifier who created the conversation.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets the conversation title.
    /// </summary>
    public required string Title { get; init; }
}
