using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Domain.Repositories;
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
            .WithDescription("Retrieve all conversations ordered by most recently updated. Use 'q' parameter for full-text search across conversation titles and message content.")
            .WithSummary("List or search conversations")
            .Produces<List<ConversationDto>>();

        group.MapGet("{id:guid}", GetConversation)
            .WithName("GetConversation")
            .WithDescription("Retrieve a specific conversation by its unique identifier")
            .WithSummary("Get conversation by ID")
            .Produces<ConversationDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", CreateConversation)
            .WithName("CreateConversation")
            .WithDescription("Create a new conversation with the specified title. Title must be between 1 and 200 characters.")
            .WithSummary("Create a new conversation")
            .Produces<ConversationDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("{id:guid}", UpdateConversation)
            .WithName("UpdateConversation")
            .WithDescription("Update the title of an existing conversation. Title must be between 1 and 200 characters.")
            .WithSummary("Update conversation title")
            .Produces<ConversationDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapDelete("{id:guid}", DeleteConversation)
            .WithName("DeleteConversation")
            .WithDescription("Delete a conversation by its unique identifier")
            .WithSummary("Delete conversation")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetConversations(
        string? q,
        IConversationRepository repository,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(q))
        {
            logger.LogInformation("Searching conversations with query: {Query}", q);
            var searchResults = await repository.SearchAsync(q, ct);
            var searchItems = searchResults.Select(c => new ConversationDto
            {
                Id = c.Id,
                Title = c.Title,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();
            return Results.Ok(searchItems);
        }

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

    private static async Task<IResult> CreateConversation(
        CreateConversationRequest request,
        IValidator<CreateConversationRequest> validator,
        IConversationRepository repository,
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

    private static async Task<IResult> UpdateConversation(
        Guid id,
        UpdateConversationRequest request,
        IValidator<UpdateConversationRequest> validator,
        IConversationRepository repository,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        logger.LogInformation("Updating conversation {ConversationId}: {Title}", id, request.Title);
        
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null)
        {
            return Results.NotFound();
        }

        entity.UpdateTitle(request.Title);
        await repository.UpdateAsync(entity, ct);

        var dto = new ConversationDto
        {
            Id = entity.Id,
            Title = entity.Title,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

        return Results.Ok(dto);
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

/// <summary>
/// Conversation data transfer object
/// </summary>
public record ConversationDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Request to create a new conversation
/// </summary>
/// <param name="Title">Conversation title (1-200 characters)</param>
public record CreateConversationRequest(string Title);

/// <summary>
/// Request to update an existing conversation
/// </summary>
/// <param name="Title">New conversation title (1-200 characters)</param>
public record UpdateConversationRequest(string Title);

/// <summary>
/// Request to create a message in a conversation
/// </summary>
/// <param name="ConversationId">The conversation identifier</param>
/// <param name="Content">Message content (1-10,000 characters)</param>
public record CreateMessageRequest(Guid ConversationId, string Content);

