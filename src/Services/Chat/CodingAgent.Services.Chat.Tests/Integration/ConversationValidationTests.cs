using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace CodingAgent.Services.Chat.Tests.Integration;

/// <summary>
/// Integration tests for validation behavior in conversation endpoints
/// </summary>
[Collection("ChatServiceCollection")]
public class ConversationValidationTests
{
    private readonly ChatServiceFixture _fixture;

    public ConversationValidationTests(ChatServiceFixture fixture)
    {
        _fixture = fixture;
    }

    #region CreateConversation Validation Tests

    [Fact]
    public async Task CreateConversation_WithValidTitle_ShouldSucceed()
    {
        // Arrange
        var request = new { title = "Valid Conversation Title" };

        // Act
        var response = await _fixture.Client.PostAsJsonAsync("/conversations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateConversation_WithEmptyTitle_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new { title = "" };

        // Act
        var response = await _fixture.Client.PostAsJsonAsync("/conversations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Errors.Should().ContainKey("Title");
        problemDetails.Errors["Title"].Should().Contain("Title is required");
    }

    [Fact]
    public async Task CreateConversation_WithNullTitle_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new { title = (string?)null };

        // Act
        var response = await _fixture.Client.PostAsJsonAsync("/conversations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Errors.Should().ContainKey("Title");
    }

    [Fact]
    public async Task CreateConversation_WithWhitespaceTitle_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new { title = "   " };

        // Act
        var response = await _fixture.Client.PostAsJsonAsync("/conversations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Errors.Should().ContainKey("Title");
        problemDetails.Errors["Title"].Should().Contain("Title is required");
    }

    [Fact]
    public async Task CreateConversation_WithTitleExceeding200Characters_ShouldReturnBadRequest()
    {
        // Arrange
        var longTitle = new string('a', 201);
        var request = new { title = longTitle };

        // Act
        var response = await _fixture.Client.PostAsJsonAsync("/conversations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Errors.Should().ContainKey("Title");
        problemDetails.Errors["Title"].Should().Contain("Title must be between 1 and 200 characters");
    }

    [Fact]
    public async Task CreateConversation_WithTitleExactly200Characters_ShouldSucceed()
    {
        // Arrange
        var title = new string('a', 200);
        var request = new { title };

        // Act
        var response = await _fixture.Client.PostAsJsonAsync("/conversations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    #endregion

    #region UpdateConversation Validation Tests

    [Fact]
    public async Task UpdateConversation_WithValidTitle_ShouldSucceed()
    {
        // Arrange - first create a conversation
        var createRequest = new { title = "Original Title" };
        var createResponse = await _fixture.Client.PostAsJsonAsync("/conversations", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ConversationDto>();

        var updateRequest = new { title = "Updated Title" };

        // Act
        var response = await _fixture.Client.PutAsJsonAsync($"/conversations/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ConversationDto>();
        updated!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task UpdateConversation_WithEmptyTitle_ShouldReturnBadRequest()
    {
        // Arrange - first create a conversation
        var createRequest = new { title = "Original Title" };
        var createResponse = await _fixture.Client.PostAsJsonAsync("/conversations", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ConversationDto>();

        var updateRequest = new { title = "" };

        // Act
        var response = await _fixture.Client.PutAsJsonAsync($"/conversations/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Errors.Should().ContainKey("Title");
    }

    [Fact]
    public async Task UpdateConversation_WithTitleExceeding200Characters_ShouldReturnBadRequest()
    {
        // Arrange - first create a conversation
        var createRequest = new { title = "Original Title" };
        var createResponse = await _fixture.Client.PostAsJsonAsync("/conversations", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ConversationDto>();

        var longTitle = new string('a', 201);
        var updateRequest = new { title = longTitle };

        // Act
        var response = await _fixture.Client.PutAsJsonAsync($"/conversations/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Errors.Should().ContainKey("Title");
    }

    [Fact]
    public async Task UpdateConversation_NonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new { title = "Updated Title" };

        // Act
        var response = await _fixture.Client.PutAsJsonAsync($"/conversations/{nonExistentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    private record ConversationDto(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt);

    private class ValidationProblemDetails
    {
        public Dictionary<string, string[]> Errors { get; set; } = new();
    }
}
