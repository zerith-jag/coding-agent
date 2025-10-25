using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodingAgent.Services.Chat.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddFullTextSearch : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create GIN index for full-text search on conversation titles
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS IX_conversations_Title_FullText 
            ON chat.conversations 
            USING GIN (to_tsvector('english', ""Title""));
        ");

        // Create GIN index for full-text search on message content
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS IX_messages_Content_FullText 
            ON chat.messages 
            USING GIN (to_tsvector('english', ""Content""));
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop the full-text search indexes
        migrationBuilder.Sql("DROP INDEX IF EXISTS chat.IX_conversations_Title_FullText;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS chat.IX_messages_Content_FullText;");
    }
}
