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
        
        // Try to get connection string from environment variable first, fall back to dummy for migrations
        var connectionString = Environment.GetEnvironmentVariable("CHAT_DB_CONNECTION_STRING")
            ?? "Host=localhost;Database=coding_agent;Username=postgres;Password=postgres";
        
        optionsBuilder.UseNpgsql(connectionString);
        
        return new ChatDbContext(optionsBuilder.Options);
    }
}
