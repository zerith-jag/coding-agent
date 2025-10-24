using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodingAgent.Services.Chat.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "chat");

        migrationBuilder.CreateTable(
            name: "conversations",
            schema: "chat",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_conversations", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "messages",
            schema: "chat",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: true),
                Content = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: false),
                Role = table.Column<string>(type: "text", nullable: false),
                SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_messages", x => x.Id);
                table.ForeignKey(
                    name: "FK_messages_conversations_ConversationId",
                    column: x => x.ConversationId,
                    principalSchema: "chat",
                    principalTable: "conversations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_conversations_CreatedAt",
            schema: "chat",
            table: "conversations",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_conversations_UserId",
            schema: "chat",
            table: "conversations",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_messages_ConversationId",
            schema: "chat",
            table: "messages",
            column: "ConversationId");

        migrationBuilder.CreateIndex(
            name: "IX_messages_SentAt",
            schema: "chat",
            table: "messages",
            column: "SentAt");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "messages",
            schema: "chat");

        migrationBuilder.DropTable(
            name: "conversations",
            schema: "chat");
    }
}
