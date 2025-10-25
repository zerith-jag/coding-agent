using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Domain.Repositories;
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

    public async Task<IEnumerable<Conversation>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching conversations for user {UserId}", userId);
        return await _context.Conversations
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .Take(100)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Conversation>> SearchAsync(string query, CancellationToken ct = default)
    {
        _logger.LogDebug("Searching conversations with query: {Query}", query);
        
        // Check if we're using PostgreSQL or in-memory database
        var providerName = _context.Database.ProviderName;
        var isPostgres = providerName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) ?? false;
        
        if (isPostgres)
        {
            // PostgreSQL full-text search
            // Validate query length to prevent abuse
            if (query.Length > 200)
            {
                _logger.LogWarning("Search query exceeds maximum length of 200 characters");
                return Enumerable.Empty<Conversation>();
            }
            
            // Sanitize input by removing special PostgreSQL full-text search operators
            var sanitizedQuery = query
                .Replace("&", " ")
                .Replace("|", " ")
                .Replace("!", " ")
                .Replace("(", " ")
                .Replace(")", " ")
                .Replace(":", " ")
                .Trim();
            
            var searchTerms = sanitizedQuery
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(term => !string.IsNullOrWhiteSpace(term) && term.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                .ToArray();
            
            if (searchTerms.Length == 0)
            {
                // If query becomes empty after sanitization, return empty results
                _logger.LogDebug("Search query contains no valid terms after sanitization");
                return Enumerable.Empty<Conversation>();
            }
            
            var searchQuery = string.Join(" & ", searchTerms);
            
            var conversations = await _context.Conversations
                .Include(c => c.Messages)
                .Where(c => 
                    // Search in conversation title
                    EF.Functions.ToTsVector("english", c.Title).Matches(EF.Functions.ToTsQuery("english", searchQuery)) ||
                    // Search in message content
                    c.Messages.Any(m => EF.Functions.ToTsVector("english", m.Content).Matches(EF.Functions.ToTsQuery("english", searchQuery)))
                )
                .Select(c => new 
                {
                    Conversation = c,
                    // Calculate relevance score
                    Rank = EF.Functions.ToTsVector("english", c.Title).Rank(EF.Functions.ToTsQuery("english", searchQuery)) +
                           c.Messages.Max(m => (double?)EF.Functions.ToTsVector("english", m.Content).Rank(EF.Functions.ToTsQuery("english", searchQuery))) ?? 0
                })
                .OrderByDescending(x => x.Rank)
                .ThenByDescending(x => x.Conversation.UpdatedAt)
                .Take(100)
                .Select(x => x.Conversation)
                .ToListAsync(ct);
            
            return conversations;
        }
        else
        {
            // Fallback for in-memory database: simple LIKE search
            var searchTerms = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            // Search in conversation titles - materialize IDs first
            var conversationIdsByTitle = await _context.Conversations
                .Where(c => searchTerms.Any(term => c.Title.ToLower().Contains(term)))
                .Select(c => c.Id)
                .ToListAsync(ct);
            
            // Search in message content - materialize IDs first
            var conversationIdsByMessage = await _context.Messages
                .Where(m => searchTerms.Any(term => m.Content.ToLower().Contains(term)))
                .Select(m => m.ConversationId)
                .Distinct()
                .ToListAsync(ct);
            
            // Combine IDs
            var allConversationIds = conversationIdsByTitle
                .Union(conversationIdsByMessage)
                .ToHashSet();
            
            // Fetch conversations by IDs
            var conversations = await _context.Conversations
                .Include(c => c.Messages)
                .Where(c => allConversationIds.Contains(c.Id))
                .OrderByDescending(c => c.UpdatedAt)
                .Take(100)
                .ToListAsync(ct);
            
            return conversations;
        }
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
