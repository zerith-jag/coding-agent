namespace CodingAgent.Services.Chat.Domain.Entities;

/// <summary>
/// Represents a conversation between a user and the AI agent
/// </summary>
public class Conversation
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<Message> _messages = new();
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    // EF Core constructor
    private Conversation() { }

    public Conversation(Guid userId, string title)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Title = title;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddMessage(Message message)
    {
        _messages.Add(message);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Hydrates the conversation with cached messages without updating timestamps.
    /// Used when reconstructing conversation state from cache.
    /// </summary>
    internal void HydrateMessages(IEnumerable<Message> messages)
    {
        _messages.Clear();
        _messages.AddRange(messages);
    }

    public void UpdateTitle(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
        {
            throw new ArgumentException("Title cannot be empty", nameof(newTitle));
        }

        Title = newTitle;
        UpdatedAt = DateTime.UtcNow;
    }
}
