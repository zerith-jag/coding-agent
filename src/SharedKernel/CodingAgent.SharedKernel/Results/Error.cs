namespace CodingAgent.SharedKernel.Results;

/// <summary>
/// Represents the outcome of an operation with detailed error information.
/// </summary>
public class Error
{
    /// <summary>
    /// Gets the error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the error type.
    /// </summary>
    public ErrorType Type { get; }

    /// <summary>
    /// Gets additional metadata about the error.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the Error class.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="type">The error type.</param>
    /// <param name="metadata">Additional error metadata.</param>
    public Error(string code, string message, ErrorType type = ErrorType.Failure, Dictionary<string, object>? metadata = null)
    {
        Code = code;
        Message = message;
        Type = type;
        Metadata = metadata;
    }

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    public static Error Validation(string code, string message, Dictionary<string, object>? metadata = null)
        => new(code, message, ErrorType.Validation, metadata);

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    public static Error NotFound(string code, string message, Dictionary<string, object>? metadata = null)
        => new(code, message, ErrorType.NotFound, metadata);

    /// <summary>
    /// Creates a conflict error.
    /// </summary>
    public static Error Conflict(string code, string message, Dictionary<string, object>? metadata = null)
        => new(code, message, ErrorType.Conflict, metadata);

    /// <summary>
    /// Creates a failure error.
    /// </summary>
    public static Error Failure(string code, string message, Dictionary<string, object>? metadata = null)
        => new(code, message, ErrorType.Failure, metadata);

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    public static Error Unauthorized(string code, string message, Dictionary<string, object>? metadata = null)
        => new(code, message, ErrorType.Unauthorized, metadata);

    /// <summary>
    /// Creates a forbidden error.
    /// </summary>
    public static Error Forbidden(string code, string message, Dictionary<string, object>? metadata = null)
        => new(code, message, ErrorType.Forbidden, metadata);
}

/// <summary>
/// Defines the type of error.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// General failure.
    /// </summary>
    Failure,

    /// <summary>
    /// Validation error.
    /// </summary>
    Validation,

    /// <summary>
    /// Resource not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// Conflict with existing data.
    /// </summary>
    Conflict,

    /// <summary>
    /// Unauthorized access.
    /// </summary>
    Unauthorized,

    /// <summary>
    /// Forbidden action.
    /// </summary>
    Forbidden
}
