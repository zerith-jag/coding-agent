using CodingAgent.Services.Chat.Domain.Entities;

namespace CodingAgent.Services.Chat.Domain.Services;

/// <summary>
/// Service for caching conversation messages in Redis
/// </summary>
public interface IMessageCacheService
{
    /// <summary>
    /// Get cached messages for a conversation (last 100)
    /// </summary>
    Task<IEnumerable<Message>?> GetMessagesAsync(Guid conversationId, CancellationToken ct = default);

    /// <summary>
    /// Cache messages for a conversation
    /// </summary>
    Task SetMessagesAsync(Guid conversationId, IEnumerable<Message> messages, CancellationToken ct = default);

    /// <summary>
    /// Add a new message to the cache
    /// </summary>
    Task AddMessageAsync(Guid conversationId, Message message, CancellationToken ct = default);

    /// <summary>
    /// Invalidate cache for a conversation
    /// </summary>
    Task InvalidateCacheAsync(Guid conversationId, CancellationToken ct = default);

    /// <summary>
    /// Update the conversation's last updated timestamp
    /// </summary>
    Task UpdateConversationTimestampAsync(Guid conversationId, DateTime timestamp, CancellationToken ct = default);
}
