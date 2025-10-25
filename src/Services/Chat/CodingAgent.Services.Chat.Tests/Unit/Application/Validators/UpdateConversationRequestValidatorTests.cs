using CodingAgent.Services.Chat.Api.Endpoints;
using CodingAgent.Services.Chat.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace CodingAgent.Services.Chat.Tests.Unit.Application.Validators;

public class UpdateConversationRequestValidatorTests
{
    private readonly UpdateConversationRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Title_Is_Valid()
    {
        // Arrange
        var request = new UpdateConversationRequest("Updated Title");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Title_Is_Empty()
    {
        // Arrange
        var request = new UpdateConversationRequest("");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title is required");
    }

    [Fact]
    public void Should_Fail_When_Title_Is_Null()
    {
        // Arrange
        var request = new UpdateConversationRequest(null!);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Should_Fail_When_Title_Is_Whitespace()
    {
        // Arrange
        var request = new UpdateConversationRequest("   ");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title is required");
    }

    [Fact]
    public void Should_Fail_When_Title_Exceeds_200_Characters()
    {
        // Arrange
        var longTitle = new string('a', 201);
        var request = new UpdateConversationRequest(longTitle);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title must be between 1 and 200 characters");
    }

    [Fact]
    public void Should_Pass_When_Title_Is_Exactly_1_Character()
    {
        // Arrange
        var request = new UpdateConversationRequest("a");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Pass_When_Title_Is_Exactly_200_Characters()
    {
        // Arrange
        var title = new string('a', 200);
        var request = new UpdateConversationRequest(title);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("Short")]
    [InlineData("Medium Length Title")]
    [InlineData("A title with special characters: @#$%^&*()")]
    [InlineData("Unicode title: 你好世界")]
    public void Should_Pass_When_Title_Is_Valid_Variation(string title)
    {
        // Arrange
        var request = new UpdateConversationRequest(title);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
