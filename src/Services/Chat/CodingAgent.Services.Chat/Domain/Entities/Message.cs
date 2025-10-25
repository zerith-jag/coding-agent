namespace CodingAgent.Services.Chat.Domain.Entities;

/// <summary>
/// Represents a message in a conversation
/// </summary>
public class Message
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid? UserId { get; private set; } // null for AI messages
    public string Content { get; private set; } = string.Empty;
    public MessageRole Role { get; private set; }
    public DateTime SentAt { get; private set; }

    // EF Core constructor
    private Message() { }

    public Message(Guid conversationId, Guid? userId, string content, MessageRole role)
    {
        Id = Guid.NewGuid();
        ConversationId = conversationId;
        UserId = userId;
        Content = content;
        Role = role;
        SentAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory method to reconstruct a message from cached data.
    /// This method bypasses the primary constructor to avoid setting SentAt to DateTime.UtcNow,
    /// preserving the original timestamp from cached data. Used only for hydrating messages
    /// from cache where the original timestamp must be maintained.
    /// </summary>
    public static Message FromCache(Guid id, Guid conversationId, Guid? userId, string content, MessageRole role, DateTime sentAt)
    {
        // Validate that sentAt is not in the future (basic sanity check)
        // Allow 1 minute tolerance for clock skew
        if (sentAt > DateTime.UtcNow.AddMinutes(1))
        {
            throw new ArgumentException("SentAt timestamp cannot be in the future", nameof(sentAt));
        }

        return new Message
        {
            Id = id,
            ConversationId = conversationId,
            UserId = userId,
            Content = content,
            Role = role,
            SentAt = sentAt
        };
    }
}

public enum MessageRole
{
    User,
    Assistant,
    System
}
