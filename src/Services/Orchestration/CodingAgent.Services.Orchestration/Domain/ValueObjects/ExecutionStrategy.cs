namespace CodingAgent.Services.Orchestration.Domain.ValueObjects;

/// <summary>
/// Represents the execution strategy for task processing.
/// Strategy selection is based on task complexity.
/// </summary>
public enum ExecutionStrategy
{
    /// <summary>
    /// Single LLM call for simple tasks. Fast but no refinement.
    /// Success Rate: 85%, Avg Duration: 15s, Avg Cost: $0.02
    /// </summary>
    SingleShot,

    /// <summary>
    /// Multi-turn execution with validation and refinement.
    /// Success Rate: 92%, Avg Duration: 60s, Avg Cost: $0.12
    /// </summary>
    Iterative,

    /// <summary>
    /// Parallel specialized agents (Planner, Coder, Reviewer, Tester).
    /// Success Rate: 95%, Avg Duration: 300s, Avg Cost: $0.80
    /// </summary>
    MultiAgent,

    /// <summary>
    /// Ensemble approach using multiple models for critical tasks.
    /// Success Rate: 98%, Avg Duration: 480s, Avg Cost: $1.50
    /// </summary>
    HybridExecution
}
