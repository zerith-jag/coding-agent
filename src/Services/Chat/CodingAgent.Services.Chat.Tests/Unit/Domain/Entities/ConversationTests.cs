using CodingAgent.Services.Chat.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CodingAgent.Services.Chat.Tests.Unit.Domain.Entities;

public class ConversationTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateConversation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var title = "Test Conversation";

        // Act
        var conversation = new Conversation(userId, title);

        // Assert
        conversation.Id.Should().NotBeEmpty();
        conversation.UserId.Should().Be(userId);
        conversation.Title.Should().Be(title);
        conversation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        conversation.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        conversation.Messages.Should().BeEmpty();
    }

    [Fact]
    public void AddMessage_WithValidMessage_ShouldAddToCollection()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Test");
        var message = new Message(conversation.Id, userId, "Hello", MessageRole.User);

        // Act
        conversation.AddMessage(message);

        // Assert
        conversation.Messages.Should().HaveCount(1);
        conversation.Messages.Should().Contain(message);
        conversation.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AddMessage_MultipleMessages_ShouldMaintainOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Test");
        var message1 = new Message(conversation.Id, userId, "First", MessageRole.User);
        var message2 = new Message(conversation.Id, userId, "Second", MessageRole.Assistant);
        var message3 = new Message(conversation.Id, userId, "Third", MessageRole.User);

        // Act
        conversation.AddMessage(message1);
        conversation.AddMessage(message2);
        conversation.AddMessage(message3);

        // Assert
        conversation.Messages.Should().HaveCount(3);
        conversation.Messages.Should().ContainInOrder(message1, message2, message3);
    }

    [Fact]
    public void UpdateTitle_WithValidTitle_ShouldUpdateTitleAndTimestamp()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Original Title");
        var originalUpdatedAt = conversation.UpdatedAt;

        // Act
        conversation.UpdateTitle("New Title");

        // Assert
        conversation.Title.Should().Be("New Title");
        conversation.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateTitle_WithInvalidTitle_ShouldThrowArgumentException(string? invalidTitle)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Valid Title");

        // Act
        var act = () => conversation.UpdateTitle(invalidTitle!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Title cannot be empty*");
    }

    [Fact]
    public void UpdateTitle_WithInvalidTitle_ShouldNotModifyConversation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Original Title");
        var originalTitle = conversation.Title;
        var originalUpdatedAt = conversation.UpdatedAt;

        // Act
        try
        {
            conversation.UpdateTitle("");
        }
        catch (ArgumentException)
        {
            // Expected exception
        }

        // Assert
        conversation.Title.Should().Be(originalTitle);
        conversation.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void Messages_Collection_ShouldBeReadOnly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Test");

        // Act
        var messages = conversation.Messages;

        // Assert
        messages.Should().BeAssignableTo<IReadOnlyCollection<Message>>();
    }

    [Fact]
    public void CreatedAt_ShouldNotChangeAfterUpdates()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Test");
        var createdAt = conversation.CreatedAt;

        // Act
        conversation.UpdateTitle("New Title");
        var message = new Message(conversation.Id, userId, "Test", MessageRole.User);
        conversation.AddMessage(message);

        // Assert
        conversation.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void AddMessage_UpdatesTimestamp()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Test");
        var originalUpdatedAt = conversation.UpdatedAt;

        // Act
        var message = new Message(conversation.Id, userId, "Test", MessageRole.User);
        conversation.AddMessage(message);

        // Assert
        conversation.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }
}
