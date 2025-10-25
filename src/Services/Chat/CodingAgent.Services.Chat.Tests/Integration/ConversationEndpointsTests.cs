using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace CodingAgent.Services.Chat.Tests.Integration;

[Collection("ChatServiceCollection")]
[Trait("Category", "Integration")]
public class ConversationEndpointsTests
{
    private readonly ChatServiceFixture _fixture;

    public ConversationEndpointsTests(ChatServiceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateConversation_Then_GetById_ShouldSucceed()
    {
        // Arrange
        var request = new { title = "Test Conversation" };

        // Act: create
        var createResponse = await _fixture.Client.PostAsJsonAsync("/conversations", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ConversationDto>();
        created.Should().NotBeNull();

        // Act: get by id
        var getResponse = await _fixture.Client.GetAsync($"/conversations/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ConversationDto>();

        // Assert
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(created.Id);
        fetched.Title.Should().Be("Test Conversation");
    }

    [Fact]
    public async Task ListConversations_ShouldContainCreated()
    {
        // Arrange
        var title = $"List Test {Guid.NewGuid()}";
        var createResponse = await _fixture.Client.PostAsJsonAsync("/conversations", new { title });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var response = await _fixture.Client.GetAsync("/conversations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<ConversationDto>>();
        list.Should().NotBeNull();
        list!.Any(c => c.Title == title).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteConversation_ShouldReturn204()
    {
        // Arrange
        var title = $"Delete Test {Guid.NewGuid()}";
        var createResponse = await _fixture.Client.PostAsJsonAsync("/conversations", new { title });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ConversationDto>();

        // Act
        var deleteResponse = await _fixture.Client.DeleteAsync($"/conversations/{created!.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteConversation_NonExistent_ShouldReturn404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var deleteResponse = await _fixture.Client.DeleteAsync($"/conversations/{nonExistentId}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetConversation_NonExistent_ShouldReturn404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var getResponse = await _fixture.Client.GetAsync($"/conversations/{nonExistentId}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private record ConversationDto(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt);
}
