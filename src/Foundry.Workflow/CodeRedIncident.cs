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
        if (string.IsNullOrWhiteSpace(Repository))
            throw new ArgumentException("Incident repository identity is required.", nameof(Repository));
        var parts = Repository.Split('/');
        if (SchemaVersion != CurrentSchemaVersion ||
            !Enum.IsDefined(Severity) ||
            string.IsNullOrWhiteSpace(IncidentId) ||
            parts.Length != 2 || parts.Any(string.IsNullOrWhiteSpace) ||
            string.IsNullOrWhiteSpace(TaskId) || IssueNumber <= 0 ||
            string.IsNullOrWhiteSpace(CorrelationId) ||
            string.IsNullOrWhiteSpace(AgentId) || string.IsNullOrWhiteSpace(AgentRole) ||
            string.IsNullOrWhiteSpace(CurrentAction) || string.IsNullOrWhiteSpace(Reason))
            throw new ArgumentException("Incident identity, ownership, action, and reason are required.");
        if (string.IsNullOrWhiteSpace(Branch) ||
            Branch is "master" or "development" ||
            Branch.StartsWith('/') || Branch.Contains('\\') ||
            Branch.Split('/').Any(segment => segment.Length == 0 || segment is "." or "..") ||
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
