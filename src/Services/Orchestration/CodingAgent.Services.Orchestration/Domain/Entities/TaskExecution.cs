using CodingAgent.SharedKernel.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;

namespace CodingAgent.Services.Orchestration.Domain.Entities;

/// <summary>
/// Represents an execution attempt of a coding task
/// </summary>
public class TaskExecution : BaseEntity
{
    public Guid TaskId { get; private set; }
    public ExecutionStrategy Strategy { get; private set; }
    public string ModelUsed { get; private set; } = string.Empty;
    public int TokensUsed { get; private set; }
    public decimal CostUSD { get; private set; }
    public TimeSpan Duration { get; private set; }
    public ExecutionResult Result { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // EF Core constructor
    private TaskExecution() { }

    public TaskExecution(Guid taskId, ExecutionStrategy strategy, string modelUsed)
    {
        TaskId = taskId;
        Strategy = strategy;
        ModelUsed = modelUsed;
        Result = ExecutionResult.Pending;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete(int tokensUsed, decimal costUSD, TimeSpan duration)
    {
        TokensUsed = tokensUsed;
        CostUSD = costUSD;
        Duration = duration;
        Result = ExecutionResult.Success;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        ErrorMessage = errorMessage;
        Result = ExecutionResult.Failed;
        CompletedAt = DateTime.UtcNow;
    }
}

public enum ExecutionResult
{
    Pending,
    Success,
    Failed
}
