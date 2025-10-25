using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Domain.Repositories;
using CodingAgent.Services.Chat.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CodingAgent.Services.Chat.Infrastructure.Persistence;

/// <summary>
/// Repository for conversation persistence operations with Redis caching
/// </summary>
public class ConversationRepository : IConversationRepository
{
    private readonly ChatDbContext _context;
    private readonly ILogger<ConversationRepository> _logger;
    private readonly IMessageCacheService _cacheService;

    public ConversationRepository(
        ChatDbContext context,
        ILogger<ConversationRepository> logger,
        IMessageCacheService cacheService)
    {
        _context = context;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching conversation {ConversationId}", id);
        
        // Fetch conversation without messages first
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (conversation == null)
        {
            return null;
        }

        // Try to get messages from cache first (cache-aside pattern)
        var cachedMessages = await _cacheService.GetMessagesAsync(id, ct);

        if (cachedMessages != null)
        {
            _logger.LogDebug("Using cached messages for conversation {ConversationId}", id);
            // Add cached messages to conversation
            foreach (var message in cachedMessages)
            {
                conversation.AddMessage(message);
            }
        }
        else
        {
            _logger.LogDebug("Cache miss, loading messages from database for conversation {ConversationId}", id);
            // Load messages from database
            await _context.Entry(conversation)
                .Collection(c => c.Messages)
                .LoadAsync(ct);

            // Cache the messages for future requests
            if (conversation.Messages.Any())
            {
                await _cacheService.SetMessagesAsync(id, conversation.Messages, ct);
            }
        }

        return conversation;
    }

    public async Task<IEnumerable<Conversation>> GetAllAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching all conversations");
        return await _context.Conversations
            .OrderByDescending(c => c.UpdatedAt)
            .Take(100)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Conversation>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching conversations for user {UserId}", userId);
        return await _context.Conversations
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .Take(100)
            .ToListAsync(ct);
    }

    public async Task<Conversation> CreateAsync(Conversation conversation, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating conversation {ConversationId}", conversation.Id);
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync(ct);
        return conversation;
    }

    public async Task UpdateAsync(Conversation conversation, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating conversation {ConversationId}", conversation.Id);
        _context.Conversations.Update(conversation);
        await _context.SaveChangesAsync(ct);

        // Update cache with new messages
        if (conversation.Messages.Any())
        {
            await _cacheService.SetMessagesAsync(conversation.Id, conversation.Messages, ct);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting conversation {ConversationId}", id);
        var conversation = await _context.Conversations.FindAsync(new object[] { id }, ct);
        if (conversation != null)
        {
            _context.Conversations.Remove(conversation);
            await _context.SaveChangesAsync(ct);

            // Invalidate cache
            await _cacheService.InvalidateCacheAsync(id, ct);
        }
    }
}
