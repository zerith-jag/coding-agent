namespace CodingAgent.SharedKernel.Domain.ValueObjects;

/// <summary>
/// Defines the complexity level of a coding task.
/// </summary>
public enum TaskComplexity
{
    /// <summary>
    /// Simple task requiring less than 50 lines of code.
    /// Typically single file changes or minor modifications.
    /// </summary>
    Simple,

    /// <summary>
    /// Medium complexity task requiring 50-200 lines of code.
    /// May involve multiple files or moderate logic changes.
    /// </summary>
    Medium,

    /// <summary>
    /// Complex task requiring 200-1000 lines of code.
    /// Involves architectural changes or multiple components.
    /// </summary>
    Complex,

    /// <summary>
    /// Epic-level task requiring over 1000 lines of code.
    /// Major features or system-wide changes.
    /// </summary>
    Epic
}
