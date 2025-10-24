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

    private static IResult GetConversations(ILogger<Program> logger)
    {
        logger.LogInformation("Getting conversations");
        
        // Stub implementation - returns empty list
        return Results.Ok(new List<ConversationDto>());
    }

    private static IResult GetConversation(Guid id, ILogger<Program> logger)
    {
        logger.LogInformation("Getting conversation {ConversationId}", id);
        
        // Stub implementation - returns 404
        return Results.NotFound();
    }

    private static IResult CreateConversation(CreateConversationRequest request, ILogger<Program> logger)
    {
        logger.LogInformation("Creating conversation: {Title}", request.Title);
        
        // Stub implementation - returns created conversation
        var conversation = new ConversationDto
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        return Results.Created($"/conversations/{conversation.Id}", conversation);
    }

    private static IResult DeleteConversation(Guid id, ILogger<Program> logger)
    {
        logger.LogInformation("Deleting conversation {ConversationId}", id);
        
        // Stub implementation
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
