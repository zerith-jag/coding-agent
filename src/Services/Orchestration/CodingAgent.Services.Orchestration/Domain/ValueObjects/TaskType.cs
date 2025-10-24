namespace CodingAgent.Services.Orchestration.Domain.ValueObjects;

/// <summary>
/// Represents the type of coding task.
/// Aligned with ML Classifier TaskType enum.
/// </summary>
public enum TaskType
{
    BugFix,
    Feature,
    Refactor,
    Documentation,
    Test,
    Deployment
}
