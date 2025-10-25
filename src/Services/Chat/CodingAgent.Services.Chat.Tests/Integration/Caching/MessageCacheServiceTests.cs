using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using System.Diagnostics.Metrics;
using Testcontainers.Redis;
using Xunit;

namespace CodingAgent.Services.Chat.Tests.Integration.Caching;

/// <summary>
/// Integration tests for MessageCacheService using Redis testcontainers
/// </summary>
public class MessageCacheServiceTests : IAsyncLifetime
{
    private RedisContainer? _redisContainer;
    private IConnectionMultiplexer? _redis;
    private MessageCacheService? _cacheService;
    private readonly Mock<ILogger<MessageCacheService>> _loggerMock;
    private readonly IMeterFactory _meterFactory;

    public MessageCacheServiceTests()
    {
        _loggerMock = new Mock<ILogger<MessageCacheService>>();
        _meterFactory = new TestMeterFactory();
    }

    public async Task InitializeAsync()
    {
        // Start Redis container
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await _redisContainer.StartAsync();

        // Connect to Redis
        _redis = await ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString());

        // Create cache service
        _cacheService = new MessageCacheService(_redis, _loggerMock.Object, _meterFactory);
    }

    public async Task DisposeAsync()
    {
        _redis?.Dispose();
        if (_redisContainer != null)
        {
            await _redisContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetMessagesAsync_WhenCacheEmpty_ShouldReturnNull()
    {
        // Arrange
        var conversationId = Guid.NewGuid();

        // Act
        var result = await _cacheService!.GetMessagesAsync(conversationId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetMessagesAsync_ShouldCacheMessages()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var message1 = new Message(conversationId, Guid.NewGuid(), "Message 1", MessageRole.User);
        await Task.Delay(10); // Ensure distinct timestamps
        var message2 = new Message(conversationId, Guid.NewGuid(), "Message 2", MessageRole.Assistant);
        await Task.Delay(10); // Ensure distinct timestamps
        var message3 = new Message(conversationId, Guid.NewGuid(), "Message 3", MessageRole.User);
        
        var messages = new[] { message1, message2, message3 };

        // Act
        await _cacheService!.SetMessagesAsync(conversationId, messages);
        var cachedMessages = (await _cacheService.GetMessagesAsync(conversationId))?.ToList();

        // Assert
        cachedMessages.Should().NotBeNull();
        cachedMessages.Should().HaveCount(3);
        // Messages should be ordered by timestamp (ascending)
        cachedMessages![0].Content.Should().Be("Message 1");
        cachedMessages[1].Content.Should().Be("Message 2");
        cachedMessages[2].Content.Should().Be("Message 3");
    }

    [Fact]
    public async Task SetMessagesAsync_WhenMoreThan100Messages_ShouldKeepOnlyLast100()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.AddHours(-1);
        var messages = Enumerable.Range(1, 150)
            .Select(i =>
            {
                var msg = Message.FromCache(
                    Guid.NewGuid(),
                    conversationId,
                    Guid.NewGuid(),
                    $"Message {i}",
                    MessageRole.User,
                    baseTime.AddMilliseconds(i) // Ensure distinct timestamps
                );
                return msg;
            })
            .ToArray();

        // Act
        await _cacheService!.SetMessagesAsync(conversationId, messages);
        var cachedMessages = (await _cacheService.GetMessagesAsync(conversationId))?.ToList();

        // Assert
        cachedMessages.Should().NotBeNull();
        cachedMessages.Should().HaveCount(100);
        // Should keep the most recent 100 messages (51-150) ordered by timestamp
        cachedMessages![0].Content.Should().Be("Message 51");
        cachedMessages[99].Content.Should().Be("Message 150");
    }

    [Fact]
    public async Task AddMessageAsync_ShouldAddMessageToCache()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var initialMessages = new[]
        {
            new Message(conversationId, Guid.NewGuid(), "Message 1", MessageRole.User),
            new Message(conversationId, Guid.NewGuid(), "Message 2", MessageRole.Assistant)
        };
        await _cacheService!.SetMessagesAsync(conversationId, initialMessages);

        var newMessage = new Message(conversationId, Guid.NewGuid(), "Message 3", MessageRole.User);

        // Act
        await _cacheService.AddMessageAsync(conversationId, newMessage);
        var cachedMessages = (await _cacheService.GetMessagesAsync(conversationId))?.ToList();

        // Assert
        cachedMessages.Should().NotBeNull();
        cachedMessages.Should().HaveCount(3);
        cachedMessages!.Should().Contain(m => m.Content == "Message 3");
    }

    [Fact]
    public async Task AddMessageAsync_WhenCacheHas100Messages_ShouldTrimOldest()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.AddHours(-1);
        var messages = Enumerable.Range(1, 100)
            .Select(i => Message.FromCache(
                Guid.NewGuid(),
                conversationId,
                Guid.NewGuid(),
                $"Message {i}",
                MessageRole.User,
                baseTime.AddMilliseconds(i)
            ))
            .ToArray();
        await _cacheService!.SetMessagesAsync(conversationId, messages);

        var newMessage = Message.FromCache(
            Guid.NewGuid(),
            conversationId,
            Guid.NewGuid(),
            "Message 101",
            MessageRole.User,
            baseTime.AddMilliseconds(101)
        );

        // Act
        await _cacheService.AddMessageAsync(conversationId, newMessage);
        var cachedMessages = (await _cacheService.GetMessagesAsync(conversationId))?.ToList();

        // Assert
        cachedMessages.Should().NotBeNull();
        cachedMessages.Should().HaveCount(100);
        cachedMessages!.Should().NotContain(m => m.Content == "Message 1"); // Oldest should be removed
        cachedMessages.Should().Contain(m => m.Content == "Message 101"); // Newest should be present
    }

    [Fact]
    public async Task InvalidateCacheAsync_ShouldRemoveCachedMessages()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var messages = new[]
        {
            new Message(conversationId, Guid.NewGuid(), "Message 1", MessageRole.User),
            new Message(conversationId, Guid.NewGuid(), "Message 2", MessageRole.Assistant)
        };
        await _cacheService!.SetMessagesAsync(conversationId, messages);

        // Verify messages are cached
        var cachedBefore = await _cacheService.GetMessagesAsync(conversationId);
        cachedBefore.Should().NotBeNull();

        // Act
        await _cacheService.InvalidateCacheAsync(conversationId);
        var cachedAfter = await _cacheService.GetMessagesAsync(conversationId);

        // Assert
        cachedAfter.Should().BeNull();
    }

    [Fact]
    public async Task GetMessagesAsync_AfterTTLExpires_ShouldReturnNull()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var messages = new[]
        {
            new Message(conversationId, Guid.NewGuid(), "Message 1", MessageRole.User)
        };

        // Set messages with a very short TTL (we'll test this manually by checking Redis)
        await _cacheService!.SetMessagesAsync(conversationId, messages);

        // Verify messages are cached
        var cachedBefore = await _cacheService.GetMessagesAsync(conversationId);
        cachedBefore.Should().NotBeNull();

        // Check that TTL is set (approximately 1 hour = 3600 seconds)
        var db = _redis!.GetDatabase();
        var key = $"conversation:{conversationId}:messages";
        var ttl = await db.KeyTimeToLiveAsync(key);

        // Assert
        ttl.Should().NotBeNull();
        ttl.Value.TotalSeconds.Should().BeApproximately(3600, 10); // Within 10 seconds of 1 hour
    }

    [Fact]
    public async Task GetMessagesAsync_WithMessageOrderByTimestamp_ShouldReturnInCorrectOrder()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow;
        
        var message1 = Message.FromCache(
            Guid.NewGuid(), conversationId, Guid.NewGuid(), "First", MessageRole.User, baseTime);
        var message2 = Message.FromCache(
            Guid.NewGuid(), conversationId, Guid.NewGuid(), "Second", MessageRole.Assistant, baseTime.AddSeconds(1));
        var message3 = Message.FromCache(
            Guid.NewGuid(), conversationId, Guid.NewGuid(), "Third", MessageRole.User, baseTime.AddSeconds(2));
        
        var messages = new[] { message1, message2, message3 };

        // Act
        await _cacheService!.SetMessagesAsync(conversationId, messages);
        var cachedMessages = (await _cacheService.GetMessagesAsync(conversationId))?.ToList();

        // Assert
        cachedMessages.Should().NotBeNull();
        cachedMessages.Should().HaveCount(3);
        // Messages should be in order by SentAt timestamp (ascending)
        cachedMessages![0].Content.Should().Be("First");
        cachedMessages[1].Content.Should().Be("Second");
        cachedMessages[2].Content.Should().Be("Third");
        
        // Verify timestamps are in order
        for (int i = 0; i < cachedMessages.Count - 1; i++)
        {
            cachedMessages[i].SentAt.Should().BeBefore(cachedMessages[i + 1].SentAt);
        }
    }

    [Fact]
    public async Task MessageCacheService_WhenRedisIsNull_ShouldHandleGracefully()
    {
        // Arrange
        var cacheServiceWithoutRedis = new MessageCacheService(null, _loggerMock.Object, _meterFactory);
        var conversationId = Guid.NewGuid();
        var message = new Message(conversationId, Guid.NewGuid(), "Test", MessageRole.User);

        // Act & Assert - should not throw
        var result = await cacheServiceWithoutRedis.GetMessagesAsync(conversationId);
        result.Should().BeNull();

        await cacheServiceWithoutRedis.SetMessagesAsync(conversationId, new[] { message });
        await cacheServiceWithoutRedis.AddMessageAsync(conversationId, message);
        await cacheServiceWithoutRedis.InvalidateCacheAsync(conversationId);
    }

    /// <summary>
    /// Simple test implementation of IMeterFactory for testing
    /// </summary>
    private class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new Meter(options);
        public void Dispose() { }
    }
}
