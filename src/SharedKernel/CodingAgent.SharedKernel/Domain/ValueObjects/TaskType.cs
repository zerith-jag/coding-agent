namespace CodingAgent.SharedKernel.Domain.ValueObjects;

/// <summary>
/// Defines the type of coding task to be executed.
/// </summary>
public enum TaskType
{
    /// <summary>
    /// Fix a bug or error in the codebase.
    /// </summary>
    BugFix,

    /// <summary>
    /// Implement a new feature or capability.
    /// </summary>
    Feature,

    /// <summary>
    /// Refactor existing code to improve quality or structure.
    /// </summary>
    Refactor,

    /// <summary>
    /// Add or update documentation.
    /// </summary>
    Documentation,

    /// <summary>
    /// Create or update automated tests.
    /// </summary>
    Test,

    /// <summary>
    /// Deployment-related tasks (CI/CD, infrastructure).
    /// </summary>
    Deployment
}
