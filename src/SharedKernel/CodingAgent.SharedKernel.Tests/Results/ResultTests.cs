using CodingAgent.SharedKernel.Results;
using FluentAssertions;

namespace CodingAgent.SharedKernel.Tests.Results;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Arrange & Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_WithError_ShouldCreateFailedResult()
    {
        // Arrange
        var error = new Error("TEST_ERROR", "Test error message");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Failure_WithCodeAndMessage_ShouldCreateFailedResult()
    {
        // Arrange & Act
        var result = Result.Failure("TEST_ERROR", "Test error message");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("TEST_ERROR");
        result.Error.Message.Should().Be("Test error message");
    }

    [Fact]
    public void Match_WhenSuccess_ShouldExecuteOnSuccess()
    {
        // Arrange
        var result = Result.Success();
        var executed = false;

        // Act
        var output = result.Match(
            onSuccess: () => { executed = true; return "success"; },
            onFailure: _ => "failure"
        );

        // Assert
        executed.Should().BeTrue();
        output.Should().Be("success");
    }

    [Fact]
    public void Match_WhenFailure_ShouldExecuteOnFailure()
    {
        // Arrange
        var error = new Error("TEST_ERROR", "Test error");
        var result = Result.Failure(error);
        var executed = false;

        // Act
        var output = result.Match(
            onSuccess: () => "success",
            onFailure: e => { executed = true; return e.Code; }
        );

        // Assert
        executed.Should().BeTrue();
        output.Should().Be("TEST_ERROR");
    }

    [Fact]
    public async Task MatchAsync_WhenSuccess_ShouldExecuteOnSuccess()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var output = await result.MatchAsync(
            onSuccess: async () => { await Task.Delay(1); return "success"; },
            onFailure: _ => Task.FromResult("failure")
        );

        // Assert
        output.Should().Be("success");
    }
}

public class ResultTTests
{
    [Fact]
    public void Success_WithValue_ShouldCreateSuccessfulResult()
    {
        // Arrange & Act
        var result = Result<int>.Success(42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(42);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_WithError_ShouldCreateFailedResult()
    {
        // Arrange
        var error = new Error("TEST_ERROR", "Test error message");

        // Act
        var result = Result<int>.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Value.Should().Be(0);
        result.Error.Should().Be(error);
    }

    [Fact]
    public void ImplicitConversion_FromValue_ShouldCreateSuccessfulResult()
    {
        // Arrange & Act
        Result<string> result = "test value";

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test value");
    }

    [Fact]
    public void ImplicitConversion_FromError_ShouldCreateFailedResult()
    {
        // Arrange
        var error = new Error("TEST_ERROR", "Test error");

        // Act
        Result<int> result = error;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Match_WhenSuccess_ShouldExecuteOnSuccess()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var output = result.Match(
            onSuccess: value => $"Value: {value}",
            onFailure: _ => "Error"
        );

        // Assert
        output.Should().Be("Value: 42");
    }

    [Fact]
    public void Match_WhenFailure_ShouldExecuteOnFailure()
    {
        // Arrange
        var error = new Error("TEST_ERROR", "Test error");
        var result = Result<int>.Failure(error);

        // Act
        var output = result.Match(
            onSuccess: value => $"Value: {value}",
            onFailure: e => $"Error: {e.Code}"
        );

        // Assert
        output.Should().Be("Error: TEST_ERROR");
    }

    [Fact]
    public async Task MatchAsync_WhenSuccess_ShouldExecuteOnSuccess()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var output = await result.MatchAsync(
            onSuccess: async value => { await Task.Delay(1); return $"Value: {value}"; },
            onFailure: _ => Task.FromResult("Error")
        );

        // Assert
        output.Should().Be("Value: 42");
    }
}
