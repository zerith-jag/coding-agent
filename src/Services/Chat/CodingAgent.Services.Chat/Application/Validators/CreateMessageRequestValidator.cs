using FluentValidation;

namespace CodingAgent.Services.Chat.Application.Validators;

/// <summary>
/// Validator for CreateMessageRequest
/// </summary>
public class CreateMessageRequestValidator : AbstractValidator<Api.Endpoints.CreateMessageRequest>
{
    public CreateMessageRequestValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Message content is required")
            .Length(1, 10000)
            .WithMessage("Message content must be between 1 and 10,000 characters");
    }
}

