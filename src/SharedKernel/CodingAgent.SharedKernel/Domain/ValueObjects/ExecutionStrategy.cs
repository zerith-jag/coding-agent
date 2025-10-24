namespace CodingAgent.SharedKernel.Domain.ValueObjects;

/// <summary>
/// Defines the execution strategy for task completion.
/// Strategy selection is based on task complexity.
/// </summary>
public enum ExecutionStrategy
{
    /// <summary>
    /// Single-shot execution using one LLM call.
    /// Best for simple tasks (complexity: Simple).
    /// Model: gpt-4o-mini, estimated tokens: ~2000.
    /// </summary>
    SingleShot,

    /// <summary>
    /// Iterative execution with multiple turns and validation.
    /// Best for medium complexity tasks (complexity: Medium).
    /// Model: gpt-4o, estimated tokens: ~6000.
    /// </summary>
    Iterative,

    /// <summary>
    /// Multi-agent execution with parallel specialized agents.
    /// Best for complex tasks (complexity: Complex).
    /// Models: gpt-4o + claude-3.5-sonnet, estimated tokens: ~20000.
    /// </summary>
    MultiAgent,

    /// <summary>
    /// Hybrid execution combining multiple strategies and models.
    /// Best for epic-level tasks (complexity: Epic).
    /// Models: Ensemble (3+ models), estimated tokens: 50000+.
    /// </summary>
    HybridExecution
}
