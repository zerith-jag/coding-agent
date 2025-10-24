using CodingAgent.SharedKernel.Results;
using FluentAssertions;

namespace CodingAgent.SharedKernel.Tests.Results;

public class ErrorTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var error = new Error("TEST_CODE", "Test message", ErrorType.Validation);

        // Assert
        error.Code.Should().Be("TEST_CODE");
        error.Message.Should().Be("Test message");
        error.Type.Should().Be(ErrorType.Validation);
        error.Metadata.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMetadata_ShouldInitializeMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 42 }
        };

        // Act
        var error = new Error("TEST_CODE", "Test message", ErrorType.Failure, metadata);

        // Assert
        error.Metadata.Should().NotBeNull();
        error.Metadata.Should().HaveCount(2);
        error.Metadata!["key1"].Should().Be("value1");
        error.Metadata["key2"].Should().Be(42);
    }

    [Fact]
    public void Validation_ShouldCreateValidationError()
    {
        // Arrange & Act
        var error = Error.Validation("VALIDATION_ERROR", "Validation failed");

        // Assert
        error.Type.Should().Be(ErrorType.Validation);
        error.Code.Should().Be("VALIDATION_ERROR");
        error.Message.Should().Be("Validation failed");
    }

    [Fact]
    public void NotFound_ShouldCreateNotFoundError()
    {
        // Arrange & Act
        var error = Error.NotFound("NOT_FOUND", "Entity not found");

        // Assert
        error.Type.Should().Be(ErrorType.NotFound);
        error.Code.Should().Be("NOT_FOUND");
        error.Message.Should().Be("Entity not found");
    }

    [Fact]
    public void Conflict_ShouldCreateConflictError()
    {
        // Arrange & Act
        var error = Error.Conflict("CONFLICT", "Resource conflict");

        // Assert
        error.Type.Should().Be(ErrorType.Conflict);
        error.Code.Should().Be("CONFLICT");
        error.Message.Should().Be("Resource conflict");
    }

    [Fact]
    public void Failure_ShouldCreateFailureError()
    {
        // Arrange & Act
        var error = Error.Failure("FAILURE", "Operation failed");

        // Assert
        error.Type.Should().Be(ErrorType.Failure);
        error.Code.Should().Be("FAILURE");
        error.Message.Should().Be("Operation failed");
    }

    [Fact]
    public void Unauthorized_ShouldCreateUnauthorizedError()
    {
        // Arrange & Act
        var error = Error.Unauthorized("UNAUTHORIZED", "Access denied");

        // Assert
        error.Type.Should().Be(ErrorType.Unauthorized);
        error.Code.Should().Be("UNAUTHORIZED");
        error.Message.Should().Be("Access denied");
    }

    [Fact]
    public void Forbidden_ShouldCreateForbiddenError()
    {
        // Arrange & Act
        var error = Error.Forbidden("FORBIDDEN", "Action not allowed");

        // Assert
        error.Type.Should().Be(ErrorType.Forbidden);
        error.Code.Should().Be("FORBIDDEN");
        error.Message.Should().Be("Action not allowed");
    }
}
