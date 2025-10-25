using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CodingAgent.Services.Chat.Api.Endpoints;

/// <summary>
/// Endpoints for conversation management
/// </summary>
public static class ConversationEndpoints
{
    public static void MapConversationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/conversations")
            .WithTags("Conversations")
            .WithOpenApi();

        group.MapGet("", GetConversations)
            .WithName("GetConversations")
            .Produces<List<ConversationDto>>();

        group.MapGet("{id:guid}", GetConversation)
            .WithName("GetConversation")
            .Produces<ConversationDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", CreateConversation)
            .WithName("CreateConversation")
            .Produces<ConversationDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("{id:guid}", UpdateConversation)
            .WithName("UpdateConversation")
            .Produces<ConversationDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapDelete("{id:guid}", DeleteConversation)
            .WithName("DeleteConversation")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetConversations(ChatDbContext db, ILogger<Program> logger, CancellationToken ct)
    {
        logger.LogInformation("Getting conversations");
        var items = await db.Conversations
            .OrderByDescending(c => c.UpdatedAt)
            .Take(100)
            .Select(c => new ConversationDto
            {
                Id = c.Id,
                Title = c.Title,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> GetConversation(Guid id, ChatDbContext db, ILogger<Program> logger, CancellationToken ct)
    {
        logger.LogInformation("Getting conversation {ConversationId}", id);
        var entity = await db.Conversations.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null)
        {
            return Results.NotFound();
        }

        var dto = new ConversationDto
        {
            Id = entity.Id,
            Title = entity.Title,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
        return Results.Ok(dto);
    }

    private static async Task<IResult> CreateConversation(
        CreateConversationRequest request,
        IValidator<CreateConversationRequest> validator,
        ChatDbContext db,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        logger.LogInformation("Creating conversation: {Title}", request.Title);

        var userId = Guid.NewGuid(); // TODO: replace with authenticated user when auth is wired
        var entity = new Conversation(userId, request.Title);
        db.Conversations.Add(entity);
        await db.SaveChangesAsync(ct);

        var dto = new ConversationDto
        {
            Id = entity.Id,
            Title = entity.Title,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

        return Results.Created($"/conversations/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdateConversation(
        Guid id,
        UpdateConversationRequest request,
        IValidator<UpdateConversationRequest> validator,
        ChatDbContext db,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        logger.LogInformation("Updating conversation {ConversationId}: {Title}", id, request.Title);
        
        var entity = await db.Conversations.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null)
        {
            return Results.NotFound();
        }

        entity.UpdateTitle(request.Title);
        await db.SaveChangesAsync(ct);

        var dto = new ConversationDto
        {
            Id = entity.Id,
            Title = entity.Title,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteConversation(Guid id, ChatDbContext db, ILogger<Program> logger, CancellationToken ct)
    {
        logger.LogInformation("Deleting conversation {ConversationId}", id);
        var entity = await db.Conversations.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null)
        {
            return Results.NotFound();
        }
        db.Conversations.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}

// DTOs
public record ConversationDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateConversationRequest(string Title);

public record UpdateConversationRequest(string Title);

public record CreateMessageRequest(Guid ConversationId, string Content);

