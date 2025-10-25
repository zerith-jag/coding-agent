using CodingAgent.Services.Chat.Api.Hubs;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodingAgent.Services.Chat.Tests.Unit.Api.Hubs;

public class ChatHubTests
{
    private readonly Mock<ILogger<ChatHub>> _loggerMock;
    private readonly Mock<IHubCallerClients> _clientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly Mock<IGroupManager> _groupManagerMock;
    private readonly Mock<HubCallerContext> _contextMock;
    private readonly ChatHub _hub;

    public ChatHubTests()
    {
        _loggerMock = new Mock<ILogger<ChatHub>>();
        _clientsMock = new Mock<IHubCallerClients>();
        _clientProxyMock = new Mock<IClientProxy>();
        _groupManagerMock = new Mock<IGroupManager>();
        _contextMock = new Mock<HubCallerContext>();

        _hub = new ChatHub(_loggerMock.Object)
        {
            Clients = _clientsMock.Object,
            Groups = _groupManagerMock.Object,
            Context = _contextMock.Object
        };
    }

    [Fact]
    public async Task OnConnectedAsync_ShouldLogConnection()
    {
        // Arrange
        var connectionId = "test-connection-id";
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Client connected")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_ShouldLogDisconnection()
    {
        // Arrange
        var connectionId = "test-connection-id";
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        // Act
        await _hub.OnDisconnectedAsync(null);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Client disconnected")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithException_ShouldStillLogDisconnection()
    {
        // Arrange
        var connectionId = "test-connection-id";
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);
        var exception = new Exception("Test exception");

        // Act
        await _hub.OnDisconnectedAsync(exception);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Client disconnected")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task JoinConversation_ShouldAddToGroup()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var conversationId = Guid.NewGuid().ToString();
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        // Act
        await _hub.JoinConversation(conversationId);

        // Assert
        _groupManagerMock.Verify(
            g => g.AddToGroupAsync(connectionId, conversationId, default),
            Times.Once);
    }

    [Fact]
    public async Task JoinConversation_ShouldLogJoin()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var conversationId = Guid.NewGuid().ToString();
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        // Act
        await _hub.JoinConversation(conversationId);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("joined conversation")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LeaveConversation_ShouldRemoveFromGroup()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var conversationId = Guid.NewGuid().ToString();
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        // Act
        await _hub.LeaveConversation(conversationId);

        // Assert
        _groupManagerMock.Verify(
            g => g.RemoveFromGroupAsync(connectionId, conversationId, default),
            Times.Once);
    }

    [Fact]
    public async Task LeaveConversation_ShouldLogLeave()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var conversationId = Guid.NewGuid().ToString();
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        // Act
        await _hub.LeaveConversation(conversationId);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("left conversation")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendMessage_ShouldBroadcastToGroup()
    {
        // Arrange
        var conversationId = Guid.NewGuid().ToString();
        var content = "Hello, world!";
        _clientsMock.Setup(c => c.Group(conversationId)).Returns(_clientProxyMock.Object);

        // Act
        await _hub.SendMessage(conversationId, content);

        // Assert
        _clientProxyMock.Verify(
            c => c.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object[]>(o => o.Length == 1),
                default),
            Times.Once);
    }

    [Fact]
    public async Task SendMessage_ShouldLogMessage()
    {
        // Arrange
        var conversationId = Guid.NewGuid().ToString();
        var content = "Test message";
        _clientsMock.Setup(c => c.Group(conversationId)).Returns(_clientProxyMock.Object);

        // Act
        await _hub.SendMessage(conversationId, content);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Message sent to conversation")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendMessage_WithEmptyContent_ShouldStillBroadcast()
    {
        // Arrange
        var conversationId = Guid.NewGuid().ToString();
        var content = "";
        _clientsMock.Setup(c => c.Group(conversationId)).Returns(_clientProxyMock.Object);

        // Act
        await _hub.SendMessage(conversationId, content);

        // Assert
        _clientProxyMock.Verify(
            c => c.SendCoreAsync("ReceiveMessage", It.IsAny<object[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task TypingIndicator_WhenTyping_ShouldNotifyOthersInGroup()
    {
        // Arrange
        var conversationId = Guid.NewGuid().ToString();
        var connectionId = "test-connection-id";
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);
        _clientsMock.Setup(c => c.OthersInGroup(conversationId)).Returns(_clientProxyMock.Object);

        // Act
        await _hub.TypingIndicator(conversationId, true);

        // Assert
        _clientProxyMock.Verify(
            c => c.SendCoreAsync(
                "UserTyping",
                It.Is<object[]>(o => o.Length == 2 && o[0].Equals(connectionId) && o[1].Equals(true)),
                default),
            Times.Once);
    }

    [Fact]
    public async Task TypingIndicator_WhenStoppedTyping_ShouldNotifyOthersInGroup()
    {
        // Arrange
        var conversationId = Guid.NewGuid().ToString();
        var connectionId = "test-connection-id";
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);
        _clientsMock.Setup(c => c.OthersInGroup(conversationId)).Returns(_clientProxyMock.Object);

        // Act
        await _hub.TypingIndicator(conversationId, false);

        // Assert
        _clientProxyMock.Verify(
            c => c.SendCoreAsync(
                "UserTyping",
                It.Is<object[]>(o => o.Length == 2 && o[0].Equals(connectionId) && o[1].Equals(false)),
                default),
            Times.Once);
    }

    [Fact]
    public async Task TypingIndicator_ShouldOnlyNotifyOthers()
    {
        // Arrange
        var conversationId = Guid.NewGuid().ToString();
        _clientsMock.Setup(c => c.OthersInGroup(conversationId)).Returns(_clientProxyMock.Object);

        // Act
        await _hub.TypingIndicator(conversationId, true);

        // Assert - verify OthersInGroup is called, not Group or All
        _clientsMock.Verify(c => c.OthersInGroup(conversationId), Times.Once);
        _clientsMock.Verify(c => c.Group(It.IsAny<string>()), Times.Never);
        _clientsMock.Verify(c => c.All, Times.Never);
    }
}
