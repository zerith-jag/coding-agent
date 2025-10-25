using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace CodingAgent.Services.Chat.Tests.Integration;

[Collection("ChatServiceCollection")]
public class ConversationSearchTests
{
    private readonly ChatServiceFixture _fixture;

    public ConversationSearchTests(ChatServiceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SearchConversations_ByTitle_ShouldReturnMatchingResults()
    {
        // Arrange: Create conversations with specific titles
        var uniqueWord = $"bug{Guid.NewGuid().ToString("N")[..8]}";
        var title1 = $"{uniqueWord} fix authentication issue";
        var title2 = "Feature: Add new API endpoint";
        var title3 = $"Refactor {uniqueWord} in service layer";

        await _fixture.Client.PostAsJsonAsync("/conversations", new { title = title1 });
        await _fixture.Client.PostAsJsonAsync("/conversations", new { title = title2 });
        await _fixture.Client.PostAsJsonAsync("/conversations", new { title = title3 });

        // Act: Search for the unique word
        var response = await _fixture.Client.GetAsync($"/conversations?q={uniqueWord}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<ConversationDto>>();
        results.Should().NotBeNull();
        results!.Count.Should().BeGreaterOrEqualTo(2);
        results.Should().Contain(c => c.Title.Contains(uniqueWord, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchConversations_CaseInsensitive_ShouldReturnResults()
    {
        // Arrange
        var uniqueWord = $"SignalR{Guid.NewGuid().ToString("N")[..8]}";
        var title = $"{uniqueWord} Connection Management";
        await _fixture.Client.PostAsJsonAsync("/conversations", new { title });

        // Act: Search with lowercase
        var response = await _fixture.Client.GetAsync($"/conversations?q={uniqueWord.ToLower()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<ConversationDto>>();
        results.Should().NotBeNull();
        results!.Should().Contain(c => c.Title.Contains(uniqueWord, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchConversations_MultipleWords_ShouldReturnRelevantResults()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var title1 = $"Authentication bug fix {uniqueId}";
        var title2 = $"Security authentication enhancement {uniqueId}";
        var title3 = $"Documentation update {uniqueId}";

        await _fixture.Client.PostAsJsonAsync("/conversations", new { title = title1 });
        await _fixture.Client.PostAsJsonAsync("/conversations", new { title = title2 });
        await _fixture.Client.PostAsJsonAsync("/conversations", new { title = title3 });

        // Act: Search for "authentication"
        var response = await _fixture.Client.GetAsync($"/conversations?q=authentication {uniqueId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<ConversationDto>>();
        results.Should().NotBeNull();
        
        // Should return conversations with "authentication" in the title
        var authResults = results!.Where(c => c.Title.Contains("authentication", StringComparison.OrdinalIgnoreCase)).ToList();
        authResults.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task SearchConversations_NoMatches_ShouldReturnEmptyList()
    {
        // Arrange: Search for something that doesn't exist
        var nonExistentQuery = $"xyznonexistent{Guid.NewGuid()}";

        // Act
        var response = await _fixture.Client.GetAsync($"/conversations?q={nonExistentQuery}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<ConversationDto>>();
        results.Should().NotBeNull();
        results!.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchConversations_EmptyQuery_ShouldReturnAllConversations()
    {
        // Arrange: Create at least one conversation
        var uniqueTitle = $"Test {Guid.NewGuid()}";
        await _fixture.Client.PostAsJsonAsync("/conversations", new { title = uniqueTitle });

        // Act: Search with empty query
        var response = await _fixture.Client.GetAsync("/conversations?q=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<ConversationDto>>();
        results.Should().NotBeNull();
        results!.Should().Contain(c => c.Title == uniqueTitle);
    }

    [Fact]
    public async Task SearchConversations_QuotedPhrase_ShouldReturnResults()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var exactPhrase = $"connection timeout error {uniqueId}";
        var title1 = $"Fix {exactPhrase} in production";
        var title2 = $"Random title {uniqueId}";

        await _fixture.Client.PostAsJsonAsync("/conversations", new { title = title1 });
        await _fixture.Client.PostAsJsonAsync("/conversations", new { title = title2 });

        // Act: Search for words from the phrase
        var response = await _fixture.Client.GetAsync($"/conversations?q=connection timeout {uniqueId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<ConversationDto>>();
        results.Should().NotBeNull();
        results!.Should().Contain(c => c.Title.Contains("connection", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchConversations_WithSpecialCharacters_ShouldHandleGracefully()
    {
        // Arrange: Create a conversation
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var title = $"Authentication & authorization {uniqueId}";
        await _fixture.Client.PostAsJsonAsync("/conversations", new { title });

        // Act: Search with special characters that need sanitization
        var response = await _fixture.Client.GetAsync($"/conversations?q=authentication%20%26%20authorization%20{uniqueId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<ConversationDto>>();
        results.Should().NotBeNull();
        // Should still find the conversation despite special characters
        results!.Should().Contain(c => c.Title.Contains("Authentication", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ListConversations_WithoutSearchParameter_ShouldReturnAll()
    {
        // Arrange: Create a conversation
        var title = $"List Test {Guid.NewGuid()}";
        await _fixture.Client.PostAsJsonAsync("/conversations", new { title });

        // Act: Get without search parameter
        var response = await _fixture.Client.GetAsync("/conversations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<ConversationDto>>();
        results.Should().NotBeNull();
        results!.Should().Contain(c => c.Title == title);
    }

    private record ConversationDto(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt);
}
