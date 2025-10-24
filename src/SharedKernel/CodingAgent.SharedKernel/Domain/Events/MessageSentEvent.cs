namespace CodingAgent.SharedKernel.Domain.Events;

/// <summary>
/// Event published when a message is sent in a conversation.
/// </summary>
public record MessageSentEvent : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the unique identifier of the conversation.
    /// </summary>
    public required Guid ConversationId { get; init; }

    /// <summary>
    /// Gets the unique identifier of the message.
    /// </summary>
    public required Guid MessageId { get; init; }

    /// <summary>
    /// Gets the user identifier who sent the message.
    /// Null if the message is from an AI assistant.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Gets the message content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the role of the message sender (User, Assistant, System).
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// Gets the timestamp when the message was sent.
    /// </summary>
    public required DateTime SentAt { get; init; }
}
