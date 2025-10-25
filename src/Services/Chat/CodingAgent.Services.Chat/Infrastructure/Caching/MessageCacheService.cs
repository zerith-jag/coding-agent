using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Domain.Services;
using StackExchange.Redis;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;

namespace CodingAgent.Services.Chat.Infrastructure.Caching;

/// <summary>
/// Redis implementation of message cache service using cache-aside pattern
/// </summary>
public class MessageCacheService : IMessageCacheService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<MessageCacheService> _logger;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromHours(1);
    private const int MaxCachedMessages = 100;

    // Metrics
    private readonly Counter<long> _cacheHitCounter;
    private readonly Counter<long> _cacheMissCounter;
    private readonly ActivitySource _activitySource;

    public MessageCacheService(
        IConnectionMultiplexer? redis,
        ILogger<MessageCacheService> logger,
        IMeterFactory meterFactory)
    {
        _redis = redis;
        _logger = logger;
        _activitySource = new ActivitySource("chat-service");

        // Create metrics
        var meter = meterFactory.Create("chat-service");
        _cacheHitCounter = meter.CreateCounter<long>(
            "message_cache_hits_total",
            description: "Total number of message cache hits");
        _cacheMissCounter = meter.CreateCounter<long>(
            "message_cache_misses_total",
            description: "Total number of message cache misses");
    }

    public async Task<IEnumerable<Message>?> GetMessagesAsync(Guid conversationId, CancellationToken ct = default)
    {
        using var activity = _activitySource.StartActivity("GetMessagesFromCache");
        activity?.SetTag("conversation.id", conversationId);

        if (_redis == null || !_redis.IsConnected)
        {
            _logger.LogWarning("Redis not available, returning null");
            _cacheMissCounter.Add(1, new KeyValuePair<string, object?>("reason", "redis_unavailable"));
            return null;
        }

        try
        {
            var db = _redis.GetDatabase();
            var key = GetMessagesKey(conversationId);

            // Get all messages from sorted set (ordered by timestamp)
            var entries = await db.SortedSetRangeByScoreAsync(key, take: MaxCachedMessages);

            if (entries.Length == 0)
            {
                _logger.LogDebug("Cache miss for conversation {ConversationId}", conversationId);
                _cacheMissCounter.Add(1, new KeyValuePair<string, object?>("reason", "not_found"));
                return null;
            }

            _logger.LogDebug("Cache hit for conversation {ConversationId}, {Count} messages", conversationId, entries.Length);
            _cacheHitCounter.Add(1);

            var messages = new List<Message>();
            foreach (var entry in entries)
            {
                try
                {
                    var message = JsonSerializer.Deserialize<MessageDto>(entry.ToString());
                    if (message != null)
                    {
                        messages.Add(message.ToEntity());
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize message from cache");
                }
            }

            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading from cache for conversation {ConversationId}", conversationId);
            _cacheMissCounter.Add(1, new KeyValuePair<string, object?>("reason", "error"));
            return null;
        }
    }

    public async Task SetMessagesAsync(Guid conversationId, IEnumerable<Message> messages, CancellationToken ct = default)
    {
        using var activity = _activitySource.StartActivity("SetMessagesToCache");
        activity?.SetTag("conversation.id", conversationId);

        if (_redis == null || !_redis.IsConnected)
        {
            _logger.LogWarning("Redis not available, skipping cache set");
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            var key = GetMessagesKey(conversationId);

            // Clear existing cache
            await db.KeyDeleteAsync(key);

            // Take last 100 messages and add to sorted set (ordered by ascending timestamps for consistent storage)
            var messagesToCache = messages
                .OrderBy(m => m.SentAt)
                .TakeLast(MaxCachedMessages)
                .ToList();

            if (messagesToCache.Any())
            {
                var entries = messagesToCache.Select(m => new SortedSetEntry(
                    JsonSerializer.Serialize(MessageDto.FromEntity(m)),
                    m.SentAt.Ticks
                )).ToArray();

                await db.SortedSetAddAsync(key, entries);

                // Set TTL
                await db.KeyExpireAsync(key, _cacheTtl);

                _logger.LogDebug("Cached {Count} messages for conversation {ConversationId}", messagesToCache.Count, conversationId);
            }

            // Update timestamp
            await UpdateConversationTimestampAsync(conversationId, DateTime.UtcNow, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing to cache for conversation {ConversationId}", conversationId);
        }
    }

    public async Task AddMessageAsync(Guid conversationId, Message message, CancellationToken ct = default)
    {
        using var activity = _activitySource.StartActivity("AddMessageToCache");
        activity?.SetTag("conversation.id", conversationId);
        activity?.SetTag("message.id", message.Id);

        if (_redis == null || !_redis.IsConnected)
        {
            _logger.LogWarning("Redis not available, skipping cache add");
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            var key = GetMessagesKey(conversationId);

            // Add message to sorted set
            await db.SortedSetAddAsync(
                key,
                JsonSerializer.Serialize(MessageDto.FromEntity(message)),
                message.SentAt.Ticks
            );

            // Trim to keep only last 100 messages
            var count = await db.SortedSetLengthAsync(key);
            if (count > MaxCachedMessages)
            {
                await db.SortedSetRemoveRangeByRankAsync(key, 0, count - MaxCachedMessages - 1);
            }

            // Reset TTL
            await db.KeyExpireAsync(key, _cacheTtl);

            _logger.LogDebug("Added message {MessageId} to cache for conversation {ConversationId}", message.Id, conversationId);

            // Update timestamp
            await UpdateConversationTimestampAsync(conversationId, DateTime.UtcNow, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message to cache for conversation {ConversationId}", conversationId);
        }
    }

    public async Task InvalidateCacheAsync(Guid conversationId, CancellationToken ct = default)
    {
        using var activity = _activitySource.StartActivity("InvalidateCache");
        activity?.SetTag("conversation.id", conversationId);

        if (_redis == null || !_redis.IsConnected)
        {
            _logger.LogWarning("Redis not available, skipping cache invalidation");
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            var messagesKey = GetMessagesKey(conversationId);
            var timestampKey = GetTimestampKey(conversationId);

            await db.KeyDeleteAsync(new RedisKey[] { messagesKey, timestampKey });

            _logger.LogInformation("Invalidated cache for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for conversation {ConversationId}", conversationId);
        }
    }

    public async Task UpdateConversationTimestampAsync(Guid conversationId, DateTime timestamp, CancellationToken ct = default)
    {
        if (_redis == null || !_redis.IsConnected)
        {
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            var key = GetTimestampKey(conversationId);

            await db.StringSetAsync(key, timestamp.Ticks, _cacheTtl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error updating timestamp in cache for conversation {ConversationId}", conversationId);
        }
    }

    private static string GetMessagesKey(Guid conversationId) => $"conversation:{conversationId}:messages";
    private static string GetTimestampKey(Guid conversationId) => $"conversation:{conversationId}:updated";

    /// <summary>
    /// DTO for serializing messages to Redis
    /// </summary>
    private class MessageDto
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid? UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }

        public static MessageDto FromEntity(Message message)
        {
            return new MessageDto
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                UserId = message.UserId,
                Content = message.Content,
                Role = message.Role.ToString(),
                SentAt = message.SentAt
            };
        }

        public Message ToEntity()
        {
            if (!Enum.TryParse<MessageRole>(Role, true, out var role))
            {
                throw new ArgumentException($"Invalid message role value in cache: '{Role}'", nameof(Role));
            }
            return Message.FromCache(Id, ConversationId, UserId, Content, role, SentAt);
        }
    }
}
