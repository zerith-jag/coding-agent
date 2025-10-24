namespace CodingAgent.SharedKernel.Results;

/// <summary>
/// Represents validation errors with property-level details.
/// </summary>
public class ValidationError : Error
{
    /// <summary>
    /// Gets the collection of validation failures.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the ValidationError class.
    /// </summary>
    /// <param name="errors">The validation errors keyed by property name.</param>
    public ValidationError(IReadOnlyDictionary<string, string[]> errors)
        : base("Validation.Error", "One or more validation errors occurred.", ErrorType.Validation)
    {
        Errors = errors;
    }

    /// <summary>
    /// Initializes a new instance of the ValidationError class with a single error.
    /// </summary>
    /// <param name="propertyName">The property name that failed validation.</param>
    /// <param name="errorMessage">The validation error message.</param>
    public ValidationError(string propertyName, string errorMessage)
        : base("Validation.Error", "One or more validation errors occurred.", ErrorType.Validation)
    {
        Errors = new Dictionary<string, string[]>
        {
            { propertyName, new[] { errorMessage } }
        };
    }

    /// <summary>
    /// Creates a validation error from a collection of errors.
    /// </summary>
    public static ValidationError FromErrors(Dictionary<string, string[]> errors)
        => new(errors);

    /// <summary>
    /// Creates a validation error with a single field error.
    /// </summary>
    public static ValidationError ForProperty(string propertyName, string errorMessage)
        => new(propertyName, errorMessage);
}
