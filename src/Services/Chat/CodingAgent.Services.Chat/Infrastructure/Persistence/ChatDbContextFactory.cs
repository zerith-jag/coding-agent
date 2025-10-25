using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CodingAgent.Services.Chat.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations
/// </summary>
public class ChatDbContextFactory : IDesignTimeDbContextFactory<ChatDbContext>
{
    public ChatDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ChatDbContext>();
        
        // Use a dummy connection string for migrations
        // The actual connection string will be provided at runtime
        optionsBuilder.UseNpgsql("Host=localhost;Database=coding_agent;Username=postgres;Password=postgres");
        
        return new ChatDbContext(optionsBuilder.Options);
    }
}
