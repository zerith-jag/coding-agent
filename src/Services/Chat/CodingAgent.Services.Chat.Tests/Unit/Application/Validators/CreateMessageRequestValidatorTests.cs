using CodingAgent.Services.Chat.Api.Endpoints;
using CodingAgent.Services.Chat.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace CodingAgent.Services.Chat.Tests.Unit.Application.Validators;

public class CreateMessageRequestValidatorTests
{
    private readonly CreateMessageRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Request_Is_Valid()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var request = new CreateMessageRequest(conversationId, "Valid message content");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_ConversationId_Is_Empty()
    {
        // Arrange
        var request = new CreateMessageRequest(Guid.Empty, "Valid content");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConversationId)
            .WithErrorMessage("ConversationId is required");
    }

    [Fact]
    public void Should_Fail_When_Content_Is_Empty()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var request = new CreateMessageRequest(conversationId, "");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Message content is required");
    }

    [Fact]
    public void Should_Fail_When_Content_Is_Null()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var request = new CreateMessageRequest(conversationId, null!);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Should_Fail_When_Content_Is_Whitespace()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var request = new CreateMessageRequest(conversationId, "   ");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Message content is required");
    }

    [Fact]
    public void Should_Fail_When_Content_Exceeds_10000_Characters()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var longContent = new string('a', 10001);
        var request = new CreateMessageRequest(conversationId, longContent);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Message content must be between 1 and 10,000 characters");
    }

    [Fact]
    public void Should_Pass_When_Content_Is_Exactly_1_Character()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var request = new CreateMessageRequest(conversationId, "a");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Pass_When_Content_Is_Exactly_10000_Characters()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var content = new string('a', 10000);
        var request = new CreateMessageRequest(conversationId, content);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("Short message")]
    [InlineData("A message with special characters: @#$%^&*()")]
    [InlineData("Unicode message: 你好世界")]
    [InlineData("Multi-line\nmessage\nwith\nnewlines")]
    public void Should_Pass_When_Content_Is_Valid_Variation(string content)
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var request = new CreateMessageRequest(conversationId, content);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Both_ConversationId_And_Content_Are_Invalid()
    {
        // Arrange
        var request = new CreateMessageRequest(Guid.Empty, "");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConversationId);
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }
}
