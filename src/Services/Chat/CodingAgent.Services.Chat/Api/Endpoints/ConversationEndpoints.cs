using CodingAgent.Services.Chat.Domain.Entities;
using CodingAgent.Services.Chat.Domain.Repositories;
using CodingAgent.SharedKernel.Results;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
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
            .WithDescription("Retrieve conversations with pagination support. Default page size: 50, Max page size: 100")
            .WithSummary("List conversations")
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
        IConversationRepository repository, 
        ILogger<Program> logger, 
        HttpContext httpContext,
        int page = 1, 
        int pageSize = 50,
        CancellationToken ct = default)
    {
        logger.LogInformation("Getting conversations (page: {Page}, pageSize: {PageSize})", page, pageSize);
        
        var pagination = new PaginationParameters(page, pageSize);
        var pagedResult = await repository.GetPagedAsync(pagination, ct);
        
        var items = pagedResult.Items.Select(c => new ConversationDto
        {
            Id = c.Id,
            Title = c.Title,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();

        // Add pagination metadata to response headers
        httpContext.Response.Headers["X-Total-Count"] = pagedResult.TotalCount.ToString();
        httpContext.Response.Headers["X-Page-Number"] = pagedResult.PageNumber.ToString();
        httpContext.Response.Headers["X-Page-Size"] = pagedResult.PageSize.ToString();
        httpContext.Response.Headers["X-Total-Pages"] = pagedResult.TotalPages.ToString();

        // Add HATEOAS links
        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.Path}";
        var links = new List<string>();

        // First page link
        links.Add($"<{baseUrl}?page=1&pageSize={pageSize}>; rel=\"first\"");

        // Last page link
        if (pagedResult.TotalPages > 0)
        {
            links.Add($"<{baseUrl}?page={pagedResult.TotalPages}&pageSize={pageSize}>; rel=\"last\"");
        }

        // Previous page link
        if (pagedResult.HasPreviousPage)
        {
            links.Add($"<{baseUrl}?page={pagedResult.PageNumber - 1}&pageSize={pageSize}>; rel=\"prev\"");
        }

        // Next page link
        if (pagedResult.HasNextPage)
        {
            links.Add($"<{baseUrl}?page={pagedResult.PageNumber + 1}&pageSize={pageSize}>; rel=\"next\"");
        }

        if (links.Any())
        {
            httpContext.Response.Headers["Link"] = string.Join(", ", links);
        }

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

