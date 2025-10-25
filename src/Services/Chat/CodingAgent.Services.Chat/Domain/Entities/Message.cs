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
    /// Factory method to reconstruct a message from cached data
    /// </summary>
    public static Message FromCache(Guid id, Guid conversationId, Guid? userId, string content, MessageRole role, DateTime sentAt)
    {
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
