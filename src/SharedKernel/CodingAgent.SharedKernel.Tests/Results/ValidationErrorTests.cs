using CodingAgent.SharedKernel.Results;
using FluentAssertions;

namespace CodingAgent.SharedKernel.Tests.Results;

[Trait("Category", "Unit")]
public class ValidationErrorTests
{
    [Fact]
    public void Constructor_WithSingleError_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var validationError = new ValidationError("Email", "Email is required");

        // Assert
        validationError.Type.Should().Be(ErrorType.Validation);
        validationError.Code.Should().Be("Validation.Error");
        validationError.Errors.Should().HaveCount(1);
        validationError.Errors["Email"].Should().ContainSingle().Which.Should().Be("Email is required");
    }

    [Fact]
    public void Constructor_WithMultipleErrors_ShouldInitializeCorrectly()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "Email", new[] { "Email is required", "Email format is invalid" } },
            { "Password", new[] { "Password is too short" } }
        };

        // Act
        var validationError = new ValidationError(errors);

        // Assert
        validationError.Type.Should().Be(ErrorType.Validation);
        validationError.Errors.Should().HaveCount(2);
        validationError.Errors["Email"].Should().HaveCount(2);
        validationError.Errors["Password"].Should().ContainSingle();
    }

    [Fact]
    public void FromErrors_ShouldCreateValidationError()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "Username", new[] { "Username is required" } }
        };

        // Act
        var validationError = ValidationError.FromErrors(errors);

        // Assert
        validationError.Errors.Should().HaveCount(1);
        validationError.Errors["Username"].Should().ContainSingle().Which.Should().Be("Username is required");
    }

    [Fact]
    public void ForProperty_ShouldCreateValidationErrorForSingleProperty()
    {
        // Arrange & Act
        var validationError = ValidationError.ForProperty("Age", "Age must be positive");

        // Assert
        validationError.Errors.Should().HaveCount(1);
        validationError.Errors["Age"].Should().ContainSingle().Which.Should().Be("Age must be positive");
    }
}
