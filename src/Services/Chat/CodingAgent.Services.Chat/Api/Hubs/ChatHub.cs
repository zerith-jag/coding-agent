using Microsoft.AspNetCore.SignalR;

namespace CodingAgent.Services.Chat.Api.Hubs;

/// <summary>
/// SignalR hub for real-time chat communication
/// </summary>
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        _logger.LogInformation("Client {ConnectionId} joined conversation {ConversationId}",
            Context.ConnectionId, conversationId);
    }

    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
        _logger.LogInformation("Client {ConnectionId} left conversation {ConversationId}",
            Context.ConnectionId, conversationId);
    }

    public async Task SendMessage(string conversationId, string content)
    {
        _logger.LogInformation("Message sent to conversation {ConversationId}", conversationId);
        
        // Broadcast to all clients in the conversation group
        await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
        {
            ConversationId = conversationId,
            Content = content,
            SentAt = DateTime.UtcNow
        });
    }

    public async Task TypingIndicator(string conversationId, bool isTyping)
    {
        await Clients.OthersInGroup(conversationId).SendAsync("UserTyping", 
            Context.ConnectionId, isTyping);
    }
}
