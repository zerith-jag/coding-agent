using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Domain.Repositories;
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
            .Produces<ConversationDto>(StatusCodes.Status201Created);

        group.MapDelete("{id:guid}", DeleteConversation)
            .WithName("DeleteConversation")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetConversations(IConversationRepository repository, ILogger<Program> logger, CancellationToken ct)
    {
        logger.LogInformation("Getting conversations");
        // TODO: Filter by authenticated user when auth is wired
        var conversations = await repository.GetAllAsync(ct);
        var items = conversations.Select(c => new ConversationDto
        {
            Id = c.Id,
            Title = c.Title,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();

        return Results.Ok(items);
    }

    private static async Task<IResult> GetConversation(Guid id, IConversationRepository repository, ILogger<Program> logger, CancellationToken ct)
    {
        logger.LogInformation("Getting conversation {ConversationId}", id);
        var entity = await repository.GetByIdAsync(id, ct);
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

    private static async Task<IResult> CreateConversation(CreateConversationRequest request, IConversationRepository repository, ILogger<Program> logger, CancellationToken ct)
    {
        logger.LogInformation("Creating conversation: {Title}", request.Title);

        var userId = Guid.NewGuid(); // TODO: replace with authenticated user when auth is wired
        var entity = new Conversation(userId, request.Title);
        await repository.CreateAsync(entity, ct);

        var dto = new ConversationDto
        {
            Id = entity.Id,
            Title = entity.Title,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

        return Results.Created($"/conversations/{dto.Id}", dto);
    }

    private static async Task<IResult> DeleteConversation(Guid id, IConversationRepository repository, ILogger<Program> logger, CancellationToken ct)
    {
        logger.LogInformation("Deleting conversation {ConversationId}", id);
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null)
        {
            return Results.NotFound();
        }
        await repository.DeleteAsync(id, ct);
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
