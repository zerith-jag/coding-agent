using CodingAgent.Services.Chat.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CodingAgent.Services.Chat.Tests.Unit.Domain.Entities;

public class MessageTests
{
    [Fact]
    public void Constructor_WithUserMessage_ShouldCreateMessage()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var content = "Hello, world!";
        var role = MessageRole.User;

        // Act
        var message = new Message(conversationId, userId, content, role);

        // Assert
        message.Id.Should().NotBeEmpty();
        message.ConversationId.Should().Be(conversationId);
        message.UserId.Should().Be(userId);
        message.Content.Should().Be(content);
        message.Role.Should().Be(role);
        message.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithAssistantMessage_ShouldHaveNullUserId()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var content = "I'm here to help!";
        var role = MessageRole.Assistant;

        // Act
        var message = new Message(conversationId, null, content, role);

        // Assert
        message.Id.Should().NotBeEmpty();
        message.ConversationId.Should().Be(conversationId);
        message.UserId.Should().BeNull();
        message.Content.Should().Be(content);
        message.Role.Should().Be(role);
        message.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(MessageRole.User)]
    [InlineData(MessageRole.Assistant)]
    [InlineData(MessageRole.System)]
    public void Constructor_WithDifferentRoles_ShouldSetRoleCorrectly(MessageRole role)
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = role == MessageRole.User ? Guid.NewGuid() : (Guid?)null;
        var content = "Test message";

        // Act
        var message = new Message(conversationId, userId, content, role);

        // Assert
        message.Role.Should().Be(role);
    }

    [Fact]
    public void Constructor_WithSystemMessage_ShouldHaveNullUserId()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var content = "System notification";
        var role = MessageRole.System;

        // Act
        var message = new Message(conversationId, null, content, role);

        // Assert
        message.UserId.Should().BeNull();
        message.Role.Should().Be(role);
    }

    [Fact]
    public void Constructor_WithEmptyContent_ShouldStillCreateMessage()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var content = "";

        // Act
        var message = new Message(conversationId, userId, content, MessageRole.User);

        // Assert
        message.Content.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithLongContent_ShouldCreateMessage()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var content = new string('x', 10000);

        // Act
        var message = new Message(conversationId, userId, content, MessageRole.User);

        // Assert
        message.Content.Should().HaveLength(10000);
    }

    [Fact]
    public void TwoMessages_CreatedSequentially_ShouldHaveDifferentIds()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var message1 = new Message(conversationId, userId, "First", MessageRole.User);
        var message2 = new Message(conversationId, userId, "Second", MessageRole.User);

        // Assert
        message1.Id.Should().NotBe(message2.Id);
    }

    [Fact]
    public void Constructor_SetsCorrectTimestamp()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;
        var conversationId = Guid.NewGuid();

        // Act
        var message = new Message(conversationId, null, "Test", MessageRole.Assistant);
        var afterCreation = DateTime.UtcNow;

        // Assert
        message.SentAt.Should().BeOnOrAfter(beforeCreation);
        message.SentAt.Should().BeOnOrBefore(afterCreation);
    }
}
