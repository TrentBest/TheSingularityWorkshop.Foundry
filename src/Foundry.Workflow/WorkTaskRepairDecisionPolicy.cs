namespace TheSingularityWorkshop.Foundry;

public enum WorkTaskRepairDecision
{
    ReviewReady,
    RepairRequired,
    Blocked,
    CodeRed
}

public sealed record WorkTaskRepairDecisionResult(
    WorkTaskRepairDecision Decision,
    string ReasonCode,
    string Reason);

public sealed record WorkTaskRepairEvidenceExpectation(
    string TaskId,
    string CorrelationId,
    string Repository,
    string Branch,
    string WorkflowPath,
    string CandidateSha,
    DateTimeOffset TaskStartedAt,
    long? ExpectedCiRunId = null,
    int? ExpectedCiRunAttempt = null)
{
    public WorkTaskRepairEvidenceExpectation Validate()
    {
        if (string.IsNullOrWhiteSpace(TaskId) || string.IsNullOrWhiteSpace(CorrelationId) ||
            string.IsNullOrWhiteSpace(Repository) || Repository.Split('/').Length != 2 ||
            string.IsNullOrWhiteSpace(Branch) || Branch is "master" or "development" ||
            Branch.StartsWith('/') || Branch.Contains('\\') ||
            Branch.Split('/').Any(part => part.Length == 0 || part is "." or "..") ||
            string.IsNullOrWhiteSpace(WorkflowPath) ||
            !WorkflowPath.StartsWith(".github/workflows/", StringComparison.Ordinal) ||
            !(WorkflowPath.EndsWith(".yml", StringComparison.Ordinal) || WorkflowPath.EndsWith(".yaml", StringComparison.Ordinal)) ||
            WorkflowPath.Contains('\\') || WorkflowPath.Split('/').Any(part => part.Length == 0 || part is "." or "..") ||
            CandidateSha is not { Length: 40 } || !CandidateSha.All(Uri.IsHexDigit) ||
            TaskStartedAt == default ||
            ExpectedCiRunId.HasValue != ExpectedCiRunAttempt.HasValue ||
            ExpectedCiRunId is <= 0 || ExpectedCiRunAttempt is <= 0)
            throw new ArgumentException("Expected CI evidence provenance is invalid.");
        return this;
    }
}

/// <summary>Deterministic bounded decision policy over exact, append-only CI attempt history.</summary>
public static class WorkTaskRepairDecisionPolicy
{
    public static WorkTaskRepairDecisionResult Evaluate(
        IReadOnlyList<WorkTaskRepairAttempt> previousAttempts,
        WorkTaskRepairAttempt currentAttempt,
        WorkTaskRepairEvidenceExpectation expectation,
        int maximumAttempts,
        TimeSpan maximumDuration,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(previousAttempts);
        ArgumentNullException.ThrowIfNull(currentAttempt);
        currentAttempt.Validate();
        ArgumentNullException.ThrowIfNull(expectation);
        expectation.Validate();
        if (!MatchesExpectation(currentAttempt, expectation))
            return new(WorkTaskRepairDecision.Blocked, "repair.evidence-provenance-mismatch",
                "The CI packet does not match the task, repository, branch, workflow, and candidate SHA authorized for this attempt.");
        if (maximumAttempts is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts), "Maximum attempts must be between one and ten.");
        if (maximumDuration <= TimeSpan.Zero || maximumDuration > TimeSpan.FromHours(24))
            throw new ArgumentOutOfRangeException(nameof(maximumDuration), "Maximum repair duration must be positive and no longer than 24 hours.");
        if (now == default || now < currentAttempt.ObservedAt)
            throw new ArgumentException("Decision time must be at or after the evidence observation.", nameof(now));

        for (var index = 0; index < previousAttempts.Count; index++)
        {
            var previous = previousAttempts[index] ?? throw new ArgumentException("Attempt history cannot contain null entries.", nameof(previousAttempts));
            previous.Validate();
            if (previous.AttemptNumber != index + 1 ||
                !SameTask(previous, currentAttempt) ||
                (index > 0 && (previous.StartedAt < previousAttempts[index - 1].ObservedAt ||
                    previousAttempts[index - 1].IsSuccessful)))
                throw new ArgumentException("Previous attempt history is not contiguous for this task.", nameof(previousAttempts));
        }

        if (currentAttempt.AttemptNumber != previousAttempts.Count + 1)
            return new(WorkTaskRepairDecision.Blocked, "repair.attempt-sequence-mismatch",
                "The observed CI attempt is missing an earlier attempt or arrived out of order.");
        if (previousAttempts.Count > 0)
        {
            var previous = previousAttempts[^1];
            if (previous.IsSuccessful)
                return new(WorkTaskRepairDecision.Blocked, "repair.attempt-after-success",
                    "A successful task cannot enter another repair cycle.");
            if (previous.StartedAt > currentAttempt.StartedAt ||
                previous.ObservedAt > currentAttempt.StartedAt ||
                (previous.CiRunId == currentAttempt.CiRunId && previous.CiRunAttempt == currentAttempt.CiRunAttempt))
                return new(WorkTaskRepairDecision.Blocked, "repair.duplicate-or-stale-evidence",
                    "The CI run attempt is duplicated or older than the previously accepted evidence.");
        }

        if (currentAttempt.AttemptNumber > maximumAttempts ||
            now - currentAttempt.TaskStartedAt > maximumDuration)
            return new(WorkTaskRepairDecision.Blocked, "repair.budget-exhausted",
                "The configured repair attempt or wall-clock budget is exhausted.");

        if (!currentAttempt.EvidenceComplete)
            return new(WorkTaskRepairDecision.Blocked, "repair.evidence-incomplete",
                "CI evidence is incomplete; incomplete evidence cannot authorize repair or handoff.");
        if (currentAttempt.IsSuccessful)
            return new(WorkTaskRepairDecision.ReviewReady, "repair.validation-passed",
                "The exact candidate SHA passed complete CI evidence.");
        if (!currentAttempt.HasActionableDiagnostics)
            return new(WorkTaskRepairDecision.CodeRed, "repair.failure-not-actionable",
                "CI failed without actionable diagnostics; stop and request human review.");

        if (previousAttempts.Count > 0)
        {
            var previous = previousAttempts[^1];
            if (string.Equals(previous.CandidateSha, currentAttempt.CandidateSha, StringComparison.OrdinalIgnoreCase))
                return new(WorkTaskRepairDecision.CodeRed, "repair.candidate-unchanged",
                    "The candidate SHA did not change after a failed attempt; stop rather than rerun unchanged code.");
            if (previous.DiagnosticsFingerprint is not null &&
                string.Equals(previous.DiagnosticsFingerprint, currentAttempt.DiagnosticsFingerprint, StringComparison.OrdinalIgnoreCase))
                return new(WorkTaskRepairDecision.CodeRed, "repair.diagnostics-repeated",
                    "The same actionable diagnostics repeated after a repair; stop and escalate instead of looping.");
        }

        if (currentAttempt.AttemptNumber >= maximumAttempts)
            return new(WorkTaskRepairDecision.Blocked, "repair.attempt-budget-exhausted",
                "The final permitted CI attempt failed; no further repair cycle is authorized.");

        return new(WorkTaskRepairDecision.RepairRequired, "repair.actionable-failure",
            "The exact candidate failed with new actionable diagnostics and remains within configured budgets.");
    }


    private static bool MatchesExpectation(WorkTaskRepairAttempt attempt, WorkTaskRepairEvidenceExpectation expected) =>
        attempt.TaskId == expected.TaskId && attempt.CorrelationId == expected.CorrelationId &&
        string.Equals(attempt.Repository, expected.Repository, StringComparison.OrdinalIgnoreCase) &&
        attempt.Branch == expected.Branch && attempt.WorkflowPath == expected.WorkflowPath &&
        string.Equals(attempt.CandidateSha, expected.CandidateSha, StringComparison.OrdinalIgnoreCase) &&
        attempt.TaskStartedAt == expected.TaskStartedAt &&
        (!expected.ExpectedCiRunId.HasValue ||
            (attempt.CiRunId == expected.ExpectedCiRunId.Value &&
             attempt.CiRunAttempt == expected.ExpectedCiRunAttempt!.Value));

    private static bool SameTask(WorkTaskRepairAttempt left, WorkTaskRepairAttempt right) =>
        left.TaskId == right.TaskId && left.CorrelationId == right.CorrelationId &&
        string.Equals(left.Repository, right.Repository, StringComparison.OrdinalIgnoreCase) &&
        left.Branch == right.Branch && left.WorkflowPath == right.WorkflowPath &&
        left.TaskStartedAt == right.TaskStartedAt;
}
