namespace TheSingularityWorkshop.Foundry;

/// <summary>Explicit host-approved operations. Privileged release and merge operations are intentionally absent.</summary>
public enum WorkTaskAction
{
    ReadRepository = 0,
    WriteFiles = 1,
    CreateBranch = 2,
    CreateCommit = 3,
    RequestCiRun = 4,
    CreateDraftPullRequest = 5
}

/// <summary>Opaque, short-lived authorization grant bound to one task, branch, candidate, and path scope.</summary>
public sealed record WorkTaskCapability(
    Guid CapabilityId,
    string TaskId,
    string Repository,
    string Branch,
    string BaseSha,
    string CandidateSha,
    IReadOnlyList<string> AllowedPaths,
    IReadOnlyList<WorkTaskAction> AllowedActions,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);

/// <summary>
/// Host-owned in-memory capability registry. Keep this object and capability IDs on the host side;
/// model output and task envelopes cannot issue or expand capabilities.
/// </summary>
public sealed class WorkTaskCapabilityRegistry
{
    private static readonly TimeSpan MaximumLifetime = TimeSpan.FromHours(1);
    private readonly object _gate = new();
    private readonly Dictionary<Guid, WorkTaskCapability> _capabilities = new();

    /// <summary>Issues a capability from an explicitly selected task and host-approved subset of its scope.</summary>
    public WorkTaskCapability Grant(WorkTaskRequest task, string branch, string candidateSha,
        IReadOnlyList<string> allowedPaths, IReadOnlyCollection<WorkTaskAction> allowedActions,
        DateTimeOffset now, TimeSpan lifetime)
    {
        ArgumentNullException.ThrowIfNull(task);
        task = task.Validate();
        ValidateBranch(branch);
        ValidateSha(candidateSha, nameof(candidateSha));
        ArgumentNullException.ThrowIfNull(allowedPaths);
        ArgumentNullException.ThrowIfNull(allowedActions);
        if (lifetime <= TimeSpan.Zero || lifetime > MaximumLifetime)
            throw new ArgumentOutOfRangeException(nameof(lifetime), "Capability lifetime must be positive and no longer than one hour.");
        if (now == default)
            throw new ArgumentException("Capability issue time is required.", nameof(now));
        if (allowedPaths.Count == 0 || allowedPaths.Any(path => !IsSafePath(path)))
            throw new ArgumentException("At least one safe repository-relative path is required.", nameof(allowedPaths));
        var normalizedPaths = allowedPaths.Distinct(StringComparer.Ordinal).ToArray();
        if (normalizedPaths.Any(path => !task.AllowedPaths.Any(scope => IsWithinScope(path, scope))))
            throw new ArgumentException("Capability paths must be contained within the task's authorized path scope.", nameof(allowedPaths));
        if (allowedActions.Count == 0 || allowedActions.Any(action => !Enum.IsDefined(action)))
            throw new ArgumentException("At least one recognized action is required.", nameof(allowedActions));

        var capability = new WorkTaskCapability(Guid.NewGuid(), task.TaskId, task.Repository,
            branch, task.BaseSha, candidateSha, Array.AsReadOnly(normalizedPaths),
            Array.AsReadOnly(allowedActions.Distinct().Order().ToArray()), now, now.Add(lifetime));
        lock (_gate)
            _capabilities.Add(capability.CapabilityId, capability);
        return capability;
    }

    /// <summary>Throws unless the exact operation is within a live host-issued capability.</summary>
    public void Demand(Guid capabilityId, string taskId, string repository, string branch,
        string baseSha, string candidateSha, WorkTaskAction action, string? path, DateTimeOffset now)
    {
        lock (_gate)
        {
            if (!_capabilities.TryGetValue(capabilityId, out var capability))
                throw new UnauthorizedAccessException("Capability is unknown or has been revoked.");
            if (now < capability.IssuedAt || now >= capability.ExpiresAt)
                throw new UnauthorizedAccessException("Capability is not currently valid.");
            if (!string.Equals(taskId, capability.TaskId, StringComparison.Ordinal) ||
                !string.Equals(repository, capability.Repository, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(branch, capability.Branch, StringComparison.Ordinal) ||
                !string.Equals(baseSha, capability.BaseSha, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(candidateSha, capability.CandidateSha, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Operation provenance does not match the capability.");
            if (!Enum.IsDefined(action) || !capability.AllowedActions.Contains(action))
                throw new UnauthorizedAccessException("Operation is not authorized by this capability.");

            var pathScoped = action is WorkTaskAction.ReadRepository or WorkTaskAction.WriteFiles;
            if (pathScoped)
            {
                if (!IsSafePath(path) || !capability.AllowedPaths.Any(scope => IsWithinScope(path!, scope)))
                    throw new UnauthorizedAccessException("Operation path is outside the capability scope.");
            }
            else if (path is not null)
            {
                throw new UnauthorizedAccessException("A repository path is not valid for this operation.");
            }
        }
    }

    /// <summary>Revokes a capability immediately. Repeated revocation is harmless.</summary>
    public void Revoke(Guid capabilityId)
    {
        lock (_gate)
            _capabilities.Remove(capabilityId);
    }

    private static bool IsWithinScope(string path, string scope) =>
        string.Equals(path, scope, StringComparison.Ordinal) ||
        path.StartsWith(scope.TrimEnd('/') + "/", StringComparison.Ordinal);

    private static bool IsSafePath(string? path) =>
        !string.IsNullOrWhiteSpace(path) && !path.StartsWith('/') &&
        !path.Contains('\\') && !path.Contains(':') &&
        path.Split('/').All(segment => segment.Length > 0 && segment is not "." and not ".." &&
            !segment.Any(char.IsControl));

    private static void ValidateBranch(string branch)
    {
        if (string.IsNullOrWhiteSpace(branch) || branch is "master" or "development" ||
            branch.StartsWith('/') || branch.Contains('\\') ||
            branch.Split('/').Any(segment => segment.Length == 0 || segment is "." or ".."))
            throw new ArgumentException("Capability must target an isolated feature branch.", nameof(branch));
    }

    private static void ValidateSha(string sha, string parameterName)
    {
        if (sha is not { Length: 40 } || !sha.All(Uri.IsHexDigit))
            throw new ArgumentException("A full 40-character hexadecimal commit SHA is required.", parameterName);
    }
}
