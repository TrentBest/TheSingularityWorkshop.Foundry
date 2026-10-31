namespace TheSingularityWorkshop.Foundry;

public enum CodeRedSeverity { Warning, High, Critical }
public enum CodeRedIncidentStatus { Active, Cleared }

/// <summary>Durable-ready, credential-free incident data. Persistence and notification are host responsibilities.</summary>
public sealed record CodeRedIncident(
    int SchemaVersion,
    string IncidentId,
    string Repository,
    string TaskId,
    int IssueNumber,
    string CorrelationId,
    string AgentId,
    string AgentRole,
    string CurrentAction,
    string Branch,
    string CandidateSha,
    long? LatestCiRunId,
    int? LatestCiAttempt,
    CodeRedSeverity Severity,
    string Reason,
    int RetryBudget,
    DateTimeOffset StartedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ClearedAt,
    bool HumanEscalationRequired)
{
    public const int CurrentSchemaVersion = 1;

    public CodeRedIncident Validate()
    {
        ValidateText(IncidentId, nameof(IncidentId), 128);
        ValidateText(Repository, nameof(Repository), 256);
        ValidateText(TaskId, nameof(TaskId), 128);
        ValidateText(CorrelationId, nameof(CorrelationId), 128);
        ValidateText(AgentId, nameof(AgentId), 128);
        ValidateText(AgentRole, nameof(AgentRole), 128);
        ValidateText(CurrentAction, nameof(CurrentAction), 2048);
        ValidateText(Reason, nameof(Reason), 2048);
        var parts = Repository.Split('/');
        if (SchemaVersion != CurrentSchemaVersion ||
            !Enum.IsDefined(Severity) ||
            parts.Length != 2 || parts.Any(string.IsNullOrWhiteSpace) ||
            IssueNumber <= 0)
            throw new ArgumentException("Incident schema, repository, issue, or severity is invalid.");
        ValidateText(Branch, nameof(Branch), 512);
        if (Branch is "master" or "development" ||
            Branch.StartsWith('/') || Branch.EndsWith('/') || Branch.EndsWith('.') ||
            Branch.Contains('\\') || Branch.Contains("..", StringComparison.Ordinal) ||
            Branch.Contains("@{", StringComparison.Ordinal) ||
            Branch.Split('/').Any(segment => segment.Length == 0 || segment is "." or ".." ||
                segment.EndsWith(".lock", StringComparison.OrdinalIgnoreCase)) ||
            string.IsNullOrWhiteSpace(CandidateSha) || CandidateSha.Length != 40 ||
            !CandidateSha.All(Uri.IsHexDigit))
            throw new ArgumentException("Incident must identify an isolated branch and full candidate SHA.");
        if (LatestCiRunId is <= 0 || LatestCiAttempt is <= 0 ||
            (LatestCiRunId.HasValue != LatestCiAttempt.HasValue))
            throw new ArgumentException("CI run and attempt must both be absent or positive.");
        if (RetryBudget is < 0 or > 10 || UpdatedAt < StartedAt ||
            (ClearedAt.HasValue && (ClearedAt.Value < StartedAt || ClearedAt.Value > UpdatedAt)))
            throw new ArgumentException("Incident budget or timestamps are invalid.");
        if (ClearedAt.HasValue && HumanEscalationRequired)
            throw new ArgumentException("A cleared incident cannot still require human escalation.");
        return this;
    }

    private static void ValidateText(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength ||
            value.Any(char.IsControl))
            throw new ArgumentException($"Incident field must contain 1 to {maximumLength} non-control characters.",
                parameterName);
    }

    public CodeRedIncident Update(string action, string reason, string candidateSha,
        long? ciRunId, int? ciAttempt, DateTimeOffset at)
    {
        if (ClearedAt.HasValue || at < UpdatedAt)
            throw new InvalidOperationException("A cleared incident cannot be updated and timestamps cannot move backwards.");
        return (this with { CurrentAction = action, Reason = reason, CandidateSha = candidateSha,
            LatestCiRunId = ciRunId, LatestCiAttempt = ciAttempt, UpdatedAt = at }).Validate();
    }

    public CodeRedIncident Clear(DateTimeOffset at)
    {
        if (ClearedAt.HasValue || at < UpdatedAt)
            throw new InvalidOperationException("Incident is already cleared or clear time precedes its latest update.");
        return (this with { ClearedAt = at, UpdatedAt = at, HumanEscalationRequired = false }).Validate();
    }

    public CodeRedIncidentStatus Status => ClearedAt.HasValue
        ? CodeRedIncidentStatus.Cleared : CodeRedIncidentStatus.Active;
}
