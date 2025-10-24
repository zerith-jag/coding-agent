namespace CodingAgent.Services.Orchestration.Domain.Strategies;

/// <summary>
/// Interface for execution strategy implementations.
/// Each strategy handles tasks of specific complexity levels.
/// </summary>
public interface IExecutionStrategy
{
    /// <summary>
    /// Gets the name of the strategy (e.g., "SingleShot", "Iterative").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the complexity level this strategy is designed for.
    /// </summary>
    ValueObjects.TaskComplexity SupportsComplexity { get; }

    // Note: Actual execution methods will be added in later phases
    // Task<ExecutionResult> ExecuteAsync(CodingTask task, ExecutionContext context, CancellationToken ct = default);
}
