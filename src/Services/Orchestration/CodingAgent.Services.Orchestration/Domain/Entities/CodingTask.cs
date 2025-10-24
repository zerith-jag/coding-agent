using CodingAgent.SharedKernel.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;

namespace CodingAgent.Services.Orchestration.Domain.Entities;

/// <summary>
/// Represents a coding task to be executed
/// </summary>
public class CodingTask : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TaskType Type { get; private set; }
    public TaskComplexity Complexity { get; private set; }
    public TaskStatus Status { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private readonly List<TaskExecution> _executions = new();
    public IReadOnlyCollection<TaskExecution> Executions => _executions.AsReadOnly();

    // EF Core constructor
    private CodingTask() { }

    public CodingTask(Guid userId, string title, string description)
    {
        UserId = userId;
        Title = title;
        Description = description;
        Status = TaskStatus.Pending;
    }

    public void Classify(TaskType type, TaskComplexity complexity)
    {
        Type = type;
        Complexity = complexity;
        Status = TaskStatus.Classifying;
    }

    public void Start()
    {
        Status = TaskStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = TaskStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = TaskStatus.Failed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = TaskStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }

    public void AddExecution(TaskExecution execution)
    {
        _executions.Add(execution);
    }
}

public enum TaskStatus
{
    Pending,
    Classifying,
    InProgress,
    Completed,
    Failed,
    Cancelled
}
