namespace TheSingularityWorkshop.Foundry;

public enum WorkflowStatus
{
    Succeeded,
    Failed,
    Cancelled,
    Invalid
}

public enum StepStatus
{
    Succeeded,
    Failed,
    Cancelled,
    Skipped
}

public sealed record WorkflowDiagnostic(string Code, string Message, string? StepId = null);

public sealed record StepResult(string StepId, string HandlerKey, StepStatus Status, string? Error = null);

public sealed record WorkflowResult(
    string WorkflowId,
    WorkflowStatus Status,
    IReadOnlyList<StepResult> Steps,
    IReadOnlyList<WorkflowDiagnostic> Diagnostics)
{
    public bool IsSuccess => Status == WorkflowStatus.Succeeded;
}
