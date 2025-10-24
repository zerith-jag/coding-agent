using CodingAgent.Services.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodingAgent.Services.Chat.Infrastructure.Persistence;

/// <summary>
/// Database context for Chat service
/// </summary>
public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options)
        : base(options)
    {
    }

    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Conversation configuration
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            // One-to-many relationship with Messages
            entity.HasMany(e => e.Messages)
                .WithOne()
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Message configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConversationId).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.SentAt).IsRequired();
        });
    }
}
