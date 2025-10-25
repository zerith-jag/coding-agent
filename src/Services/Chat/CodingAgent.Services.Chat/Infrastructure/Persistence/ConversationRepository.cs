using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Domain.Repositories;
using CodingAgent.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace CodingAgent.Services.Chat.Infrastructure.Persistence;

/// <summary>
/// Repository for conversation persistence operations
/// </summary>
public class ConversationRepository : IConversationRepository
{
    private readonly ChatDbContext _context;
    private readonly ILogger<ConversationRepository> _logger;

    public ConversationRepository(
        ChatDbContext context,
        ILogger<ConversationRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching conversation {ConversationId}", id);
        return await _context.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<IEnumerable<Conversation>> GetAllAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching all conversations");
        return await _context.Conversations
            .OrderByDescending(c => c.UpdatedAt)
            .Take(100)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<Conversation>> GetPagedAsync(PaginationParameters pagination, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching conversations page {PageNumber} with size {PageSize}", 
            pagination.PageNumber, pagination.PageSize);

        var query = _context.Conversations
            .OrderByDescending(c => c.UpdatedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(ct);

        return new PagedResult<Conversation>(items, totalCount, pagination.PageNumber, pagination.PageSize);
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
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting conversation {ConversationId}", id);
        var conversation = await _context.Conversations.FindAsync(new object[] { id }, ct);
        if (conversation != null)
        {
            _context.Conversations.Remove(conversation);
            await _context.SaveChangesAsync(ct);
        }
    }
}
