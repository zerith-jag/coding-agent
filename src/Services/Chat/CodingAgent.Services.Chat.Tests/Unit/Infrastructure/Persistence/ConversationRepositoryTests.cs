using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Domain.Services;
using CodingAgent.Services.Chat.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodingAgent.Services.Chat.Tests.Unit.Infrastructure.Persistence;

public class ConversationRepositoryTests : IDisposable
{
    private readonly ChatDbContext _context;
    private readonly ConversationRepository _repository;
    private readonly Mock<ILogger<ConversationRepository>> _loggerMock;
    private readonly Mock<IMessageCacheService> _cacheServiceMock;

    public ConversationRepositoryTests()
    {
        // Use in-memory database for unit testing
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChatDbContext(options);
        _loggerMock = new Mock<ILogger<ConversationRepository>>();
        _cacheServiceMock = new Mock<IMessageCacheService>();
        
        // Default cache behavior: return null (cache miss)
        _cacheServiceMock
            .Setup(x => x.GetMessagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Message>?)null);
        
        _repository = new ConversationRepository(_context, _loggerMock.Object, _cacheServiceMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_WhenConversationExists_ShouldReturnConversation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Test Conversation");
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(conversation.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(conversation.Id);
        result.Title.Should().Be("Test Conversation");
        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenConversationDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnConversationsOrderedByUpdatedAt()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        
        var conversation1 = new Conversation(userId1, "First");
        var conversation2 = new Conversation(userId2, "Second");
        var conversation3 = new Conversation(userId1, "Third");

        _context.Conversations.AddRange(conversation1, conversation2, conversation3);
        await _context.SaveChangesAsync();

        // Act
        var results = (await _repository.GetAllAsync()).ToList();

        // Assert
        results.Should().HaveCount(3);
        results.Should().BeInDescendingOrder(c => c.UpdatedAt);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoConversations_ShouldReturnEmptyCollection()
    {
        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnOnlyUserConversations()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        
        var conversation1 = new Conversation(userId1, "User1 Conv1");
        var conversation2 = new Conversation(userId2, "User2 Conv1");
        var conversation3 = new Conversation(userId1, "User1 Conv2");

        _context.Conversations.AddRange(conversation1, conversation2, conversation3);
        await _context.SaveChangesAsync();

        // Act
        var results = (await _repository.GetByUserIdAsync(userId1)).ToList();

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(c => c.UserId.Should().Be(userId1));
        results.Should().BeInDescendingOrder(c => c.UpdatedAt);
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenUserHasNoConversations_ShouldReturnEmptyCollection()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        
        var conversation = new Conversation(otherUserId, "Other User Conv");
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetByUserIdAsync(userId);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_ShouldAddConversationToDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "New Conversation");

        // Act
        var result = await _repository.CreateAsync(conversation);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(conversation.Id);
        
        var dbConversation = await _context.Conversations.FindAsync(conversation.Id);
        dbConversation.Should().NotBeNull();
        dbConversation!.Title.Should().Be("New Conversation");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreatedConversation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Test");

        // Act
        var result = await _repository.CreateAsync(conversation);

        // Assert
        result.Should().BeSameAs(conversation);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingConversation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Original Title");
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        // Detach to simulate a new context
        _context.Entry(conversation).State = EntityState.Detached;

        // Modify the conversation
        conversation.UpdateTitle("Updated Title");

        // Act
        await _repository.UpdateAsync(conversation);

        // Assert
        var dbConversation = await _context.Conversations.FindAsync(conversation.Id);
        dbConversation.Should().NotBeNull();
        dbConversation!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task DeleteAsync_WhenConversationExists_ShouldRemoveConversation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "To Delete");
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(conversation.Id);

        // Assert
        var dbConversation = await _context.Conversations.FindAsync(conversation.Id);
        dbConversation.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenConversationDoesNotExist_ShouldNotThrow()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = async () => await _repository.DeleteAsync(nonExistentId);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetAllAsync_ShouldLimitResultsTo100()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversations = Enumerable.Range(1, 150)
            .Select(i => new Conversation(userId, $"Conversation {i}"))
            .ToList();

        _context.Conversations.AddRange(conversations);
        await _context.SaveChangesAsync();

        // Act
        var results = (await _repository.GetAllAsync()).ToList();

        // Assert
        results.Should().HaveCount(100);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldLimitResultsTo100()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversations = Enumerable.Range(1, 150)
            .Select(i => new Conversation(userId, $"Conversation {i}"))
            .ToList();

        _context.Conversations.AddRange(conversations);
        await _context.SaveChangesAsync();

        // Act
        var results = (await _repository.GetByUserIdAsync(userId)).ToList();

        // Assert
        results.Should().HaveCount(100);
    }
}
