namespace TheSingularityWorkshop.Foundry;

public enum CodeRedNoticeStatus
{
    Active,
    Cleared
}

/// <summary>Credential-free human-facing projection of a CODE RED incident.</summary>
public sealed record CodeRedStatusNotice(
    string IncidentId,
    CodeRedNoticeStatus Status,
    string Repository,
    int IssueNumber,
    string TaskId,
    string CorrelationId,
    string AgentId,
    string CandidateSha,
    long? CiRunId,
    int? CiAttempt,
    string CurrentAction,
    string Reason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? NextUpdateAt,
    bool HumanEscalationRequired,
    string Message);

/// <summary>Creates truthful status notices; active notices require a matching live CODE RED task lease.</summary>
public static class CodeRedStatusNoticeFactory
{
    public static CodeRedStatusNotice CreateActive(CodeRedIncident incident, WorkTaskSnapshot task,
        DateTimeOffset now, DateTimeOffset nextUpdateAt)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(task);
        incident.Validate();
        JsonlWorkTaskStore.ValidateSnapshot(task);
        if (incident.Status != CodeRedIncidentStatus.Active ||
            task.Status != WorkTaskStatus.CodeRed ||
            task.Lease is not { } lease || lease.ExpiresAt <= now)
            throw new InvalidOperationException("An active CODE RED notice requires an active incident and a live CODE RED task lease.");
        if (now == default || nextUpdateAt <= now || incident.UpdatedAt > now ||
            incident.Repository != task.Request.Repository ||
            incident.TaskId != task.Request.TaskId ||
            incident.IssueNumber != task.Request.IssueNumber ||
            incident.CorrelationId != task.Request.CorrelationId ||
            incident.AgentId != lease.AgentId)
            throw new ArgumentException("Incident, task, agent, and status timing must match.", nameof(incident));

        var ciIdentity = incident.LatestCiRunId.HasValue
            ? $"CI run {incident.LatestCiRunId.Value}, attempt {incident.LatestCiAttempt!.Value}"
            : "CI run not yet observed";
        var message = $"I’m currently engaged with a CODE RED in {incident.Repository} " +
            $"(issue #{incident.IssueNumber}, {ciIdentity}, candidate {incident.CandidateSha}). " +
            $"Current action: {incident.CurrentAction}. Blocker: {incident.Reason}. " +
            $"Next update: {nextUpdateAt:O}. " +
            "I’ll request human input only if a defined authorization or decision is required.";

        return new CodeRedStatusNotice(incident.IncidentId, CodeRedNoticeStatus.Active,
            incident.Repository, incident.IssueNumber, incident.TaskId, incident.CorrelationId,
            lease.AgentId, incident.CandidateSha, incident.LatestCiRunId, incident.LatestCiAttempt,
            incident.CurrentAction, incident.Reason, now, nextUpdateAt,
            incident.HumanEscalationRequired, message);
    }

    public static CodeRedStatusNotice CreateCleared(CodeRedIncident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);
        incident.Validate();
        if (incident.Status != CodeRedIncidentStatus.Cleared || incident.ClearedAt is not { } clearedAt)
            throw new InvalidOperationException("A cleared notice requires an explicitly cleared incident.");

        var message = $"CODE RED cleared for {incident.Repository} issue #{incident.IssueNumber} " +
            $"(task {incident.TaskId}, incident {incident.IncidentId}) at {clearedAt:O}.";
        return new CodeRedStatusNotice(incident.IncidentId, CodeRedNoticeStatus.Cleared,
            incident.Repository, incident.IssueNumber, incident.TaskId, incident.CorrelationId,
            incident.AgentId, incident.CandidateSha, incident.LatestCiRunId, incident.LatestCiAttempt,
            incident.CurrentAction, incident.Reason, clearedAt, null, false, message);
    }
}
