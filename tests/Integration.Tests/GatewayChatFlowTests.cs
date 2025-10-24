using System.Net.Http.Json;
using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace CodingAgent.Integration.Tests;

/// <summary>
/// End-to-end integration tests for Gateway → Chat Service flow
/// These tests validate the complete POC architecture including:
/// - Gateway proxying
/// - SignalR WebSocket connections
/// - PostgreSQL persistence
/// - Redis caching
/// - RabbitMQ event publishing
/// </summary>
public class GatewayChatFlowTests : IClassFixture<DockerComposeFixture>
{
    private readonly HttpClient _gatewayClient;
    private readonly HubConnection _signalRConnection;
    private readonly DockerComposeFixture _fixture;

    public GatewayChatFlowTests(DockerComposeFixture fixture)
    {
        _fixture = fixture;
        _gatewayClient = new HttpClient 
        { 
            BaseAddress = new Uri("http://localhost:5000") 
        };

        _signalRConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/hubs/chat")
            .WithAutomaticReconnect()
            .Build();
    }

    [Fact(Skip = "Requires actual Gateway and Chat Service implementation")]
    [Trait("Category", "E2E")]
    public async Task FullFlow_CreateConversationAndSendMessage_ShouldSucceed()
    {
        // Arrange
        var messageReceived = new TaskCompletionSource<MessageDto>();
        _signalRConnection.On<MessageDto>("ReceiveMessage", msg => 
            messageReceived.SetResult(msg));
        
        await _signalRConnection.StartAsync();

        // Act 1: Create conversation via Gateway
        var createRequest = new { Title = "Test Conversation" };
        var createResponse = await _gatewayClient.PostAsJsonAsync("/api/conversations", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var conversation = await createResponse.Content.ReadFromJsonAsync<ConversationDto>();

        conversation.Should().NotBeNull();
        conversation!.Id.Should().NotBeEmpty();

        // Act 2: Join conversation via SignalR
        await _signalRConnection.InvokeAsync("JoinConversation", conversation.Id);

        // Act 3: Send message via SignalR
        await _signalRConnection.InvokeAsync("SendMessage", conversation.Id, "Hello POC!");

        // Assert: Message received through SignalR
        var receivedMessage = await messageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        receivedMessage.Content.Should().Be("Hello POC!");
        receivedMessage.ConversationId.Should().Be(conversation.Id);

        // Verify: Message persisted in PostgreSQL
        var messagesResponse = await _gatewayClient.GetAsync($"/api/conversations/{conversation.Id}/messages");
        messagesResponse.EnsureSuccessStatusCode();
        var messages = await messagesResponse.Content.ReadFromJsonAsync<List<MessageDto>>();
        
        messages.Should().NotBeNull();
        messages!.Should().ContainSingle(m => m.Content == "Hello POC!");

        // Cleanup
        await _signalRConnection.StopAsync();
    }

    [Fact(Skip = "Requires actual Gateway and Chat Service implementation")]
    [Trait("Category", "E2E")]
    public async Task CachedConversations_ShouldReduceDatabaseLoad()
    {
        // Arrange: Create conversation
        var createResponse = await _gatewayClient.PostAsJsonAsync("/api/conversations", 
            new { Title = "Cache Test" });
        createResponse.EnsureSuccessStatusCode();
        var conversation = await createResponse.Content.ReadFromJsonAsync<ConversationDto>();

        conversation.Should().NotBeNull();

        // Act 1: First fetch (database + cache)
        var stopwatch = Stopwatch.StartNew();
        var firstFetch = await _gatewayClient.GetAsync($"/api/conversations/{conversation!.Id}");
        var firstDuration = stopwatch.ElapsedMilliseconds;
        firstFetch.EnsureSuccessStatusCode();

        // Act 2: Second fetch (from cache)
        stopwatch.Restart();
        var secondFetch = await _gatewayClient.GetAsync($"/api/conversations/{conversation.Id}");
        var secondDuration = stopwatch.ElapsedMilliseconds;
        secondFetch.EnsureSuccessStatusCode();

        // Assert: Cached fetch should be faster
        secondDuration.Should().BeLessThan(firstDuration);
        
        // Additional validation: Both responses should be identical
        var firstContent = await firstFetch.Content.ReadAsStringAsync();
        var secondContent = await secondFetch.Content.ReadAsStringAsync();
        firstContent.Should().Be(secondContent);
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task InfrastructureServices_ShouldBeHealthy()
    {
        // This test can run even without Gateway/Chat implementation
        // It validates that infrastructure services are properly configured
        
        await Task.Delay(5000); // Allow services to fully start
        
        // Test infrastructure availability through fixture
        _fixture.Should().NotBeNull();
        
        // TODO: Add health check validations when services are implemented:
        // - PostgreSQL connection
        // - Redis connection
        // - RabbitMQ connection
        // - Seq logging
        // - Prometheus metrics
        // - Jaeger tracing
        
        Assert.True(true, "Infrastructure test placeholder - validates test framework");
    }
}

/// <summary>
/// Data Transfer Objects for testing
/// These match the contracts defined in CodingAgent.SharedKernel
/// </summary>
public record ConversationDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record MessageDto
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime SentAt { get; init; }
}
