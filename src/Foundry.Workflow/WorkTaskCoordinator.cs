namespace TheSingularityWorkshop.Foundry;

public sealed record WorkTaskRequest(int SchemaVersion, string TaskId, string CorrelationId,
    string Repository, int IssueNumber, string IssueUrl, string BaseSha,
    IReadOnlyList<string> AllowedPaths, int MaximumAttempts)
{
    public const int CurrentSchemaVersion = 1;

    public WorkTaskRequest Validate()
    {
        if (SchemaVersion != CurrentSchemaVersion)
            throw new ArgumentException($"Unsupported task schema version '{SchemaVersion}'.");
        if (string.IsNullOrWhiteSpace(TaskId) || string.IsNullOrWhiteSpace(CorrelationId))
            throw new ArgumentException("Task and correlation identifiers are required.");
        var repositoryParts = Repository.Split('/');
        if (repositoryParts.Length != 2 || repositoryParts.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Repository must use owner/name identity.");
        if (IssueNumber <= 0 || !Uri.TryCreate(IssueUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || uri.Host != "github.com" ||
            uri.AbsolutePath != $"/{repositoryParts[0]}/{repositoryParts[1]}/issues/{IssueNumber}")
            throw new ArgumentException("Issue URL must identify this repository and issue number.");
        if (BaseSha is null || BaseSha.Length != 40 || !BaseSha.All(Uri.IsHexDigit))
            throw new ArgumentException("Base SHA must be a full 40-character hexadecimal commit.");
        if (AllowedPaths is null || AllowedPaths.Count == 0 ||
            AllowedPaths.Any(path => string.IsNullOrWhiteSpace(path) || path.StartsWith('/') || path.Contains('\\') ||
                path.Split('/').Any(segment => segment is ".." or ".")))
            throw new ArgumentException("At least one safe, repository-relative path is required.");
        if (MaximumAttempts is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(MaximumAttempts));
        return this with { AllowedPaths = Array.AsReadOnly(AllowedPaths.Distinct(StringComparer.Ordinal).ToArray()) };
    }
}

public enum WorkTaskStatus
{
    Queued, Claimed, Investigating, Implementing, Validating, RepairRequired,
    Blocked, CodeRed, ReviewReady, HandedOff, Stopped, Failed
}

public sealed record WorkTaskLease(string AgentId, Guid LeaseId, DateTimeOffset ExpiresAt);
public sealed record WorkTaskEvent(long Sequence, WorkTaskStatus Previous, WorkTaskStatus Current,
    string AgentId, Guid LeaseId, DateTimeOffset OccurredAt, string Reason);
public sealed record WorkTaskSnapshot(WorkTaskRequest Request, WorkTaskStatus Status,
    WorkTaskLease? Lease, int Attempt, IReadOnlyList<WorkTaskEvent> Events);

/// <summary>
/// Deterministic in-process task lifecycle and lease-conflict reference implementation.
/// A durable GitHub-backed store and transport are host adapters, not implied here.
/// </summary>
public sealed class WorkTaskCoordinator
{
    private readonly object _gate = new();
    private readonly Dictionary<string, TaskState> _tasks = new(StringComparer.Ordinal);

    public WorkTaskSnapshot Submit(WorkTaskRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var validated = request.Validate();
        lock (_gate)
        {
            if (_tasks.ContainsKey(validated.TaskId))
                throw new InvalidOperationException($"Task '{validated.TaskId}' already exists.");
            var state = new TaskState(validated);
            _tasks.Add(validated.TaskId, state);
            return state.Snapshot();
        }
    }

    public WorkTaskSnapshot Claim(string taskId, string agentId, TimeSpan leaseDuration, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(agentId))
            throw new ArgumentException("Agent identity is required.", nameof(agentId));
        if (leaseDuration <= TimeSpan.Zero || leaseDuration > TimeSpan.FromHours(1))
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        lock (_gate)
        {
            var state = Get(taskId);
            if (state.Status is WorkTaskStatus.HandedOff or WorkTaskStatus.Stopped or WorkTaskStatus.Failed)
                throw new InvalidOperationException($"Terminal task '{taskId}' cannot be claimed.");
            if (state.Lease is { } current && current.ExpiresAt > now)
                throw new InvalidOperationException($"Task '{taskId}' is leased by '{current.AgentId}'.");
            if (state.Status != WorkTaskStatus.Queued && state.Lease is null)
                throw new InvalidOperationException($"Task '{taskId}' requires recovery before reclaim.");
            var previous = state.Status;
            var lease = new WorkTaskLease(agentId, Guid.NewGuid(), now.Add(leaseDuration));
            state.Lease = lease;
            state.Status = WorkTaskStatus.Claimed;
            state.AddEvent(previous, WorkTaskStatus.Claimed, agentId, lease.LeaseId, now, "Task claimed.");
            return state.Snapshot();
        }
    }

    public WorkTaskSnapshot Transition(string taskId, Guid leaseId, WorkTaskStatus next,
        DateTimeOffset now, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A transition reason is required.", nameof(reason));
        lock (_gate)
        {
            var state = Get(taskId);
            var lease = state.Lease;
            if (lease is null || lease.LeaseId != leaseId || lease.ExpiresAt <= now)
                throw new InvalidOperationException("The task lease is missing, expired, or superseded.");
            if (!Allowed(state.Status, next))
                throw new InvalidOperationException($"Transition '{state.Status}' -> '{next}' is not allowed.");
            if (next == WorkTaskStatus.Validating)
                state.Attempt++;
            if (next == WorkTaskStatus.RepairRequired && state.Attempt >= state.Request.MaximumAttempts)
                next = WorkTaskStatus.Blocked;
            var previous = state.Status;
            state.Status = next;
            if (next is WorkTaskStatus.HandedOff or WorkTaskStatus.Stopped or WorkTaskStatus.Failed)
                state.Lease = null;
            state.AddEvent(previous, next, lease.AgentId, lease.LeaseId, now, reason);
            return state.Snapshot();
        }
    }

    /// <summary>Rehydrates a task from a validated durable snapshot before dispatch starts.</summary>
    public void Restore(WorkTaskSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Request is null)
            throw new ArgumentException("Task request is required.", nameof(snapshot));
        snapshot.Request.Validate();
        if (snapshot.Events is null ||
            snapshot.Events.Select((item, index) => item is not null && item.Sequence == index + 1).Any(valid => !valid) ||
            snapshot.Attempt < 0 || snapshot.Attempt > snapshot.Request.MaximumAttempts ||
            (snapshot.Lease is { } lease && (string.IsNullOrWhiteSpace(lease.AgentId) || lease.LeaseId == Guid.Empty)))
            throw new ArgumentException("The task snapshot is invalid.", nameof(snapshot));
        if (snapshot.Events.Count == 0)
        {
            if (snapshot.Status != WorkTaskStatus.Queued || snapshot.Lease is not null || snapshot.Attempt != 0)
                throw new ArgumentException("An event-free snapshot must be an untouched queued task.", nameof(snapshot));
        }
        else if (snapshot.Events[^1].Current != snapshot.Status)
        {
            throw new ArgumentException("Snapshot status must match the final event.", nameof(snapshot));
        }

        lock (_gate)
        {
            if (_tasks.ContainsKey(snapshot.Request.TaskId))
                throw new InvalidOperationException($"Task '{snapshot.Request.TaskId}' already exists.");
            _tasks.Add(snapshot.Request.TaskId, TaskState.Restore(snapshot));
        }
    }

    public WorkTaskSnapshot GetSnapshot(string taskId)
    {
        lock (_gate) return Get(taskId).Snapshot();
    }

    private TaskState Get(string taskId) => _tasks.TryGetValue(taskId, out var state)
        ? state : throw new KeyNotFoundException($"Unknown task '{taskId}'.");

    private static bool Allowed(WorkTaskStatus current, WorkTaskStatus next) => (current, next) switch
    {
        (WorkTaskStatus.Claimed, WorkTaskStatus.Investigating or WorkTaskStatus.Blocked or WorkTaskStatus.CodeRed or WorkTaskStatus.Stopped or WorkTaskStatus.Failed) => true,
        (WorkTaskStatus.Investigating, WorkTaskStatus.Implementing or WorkTaskStatus.Blocked or WorkTaskStatus.CodeRed or WorkTaskStatus.Stopped or WorkTaskStatus.Failed) => true,
        (WorkTaskStatus.Implementing, WorkTaskStatus.Validating or WorkTaskStatus.Blocked or WorkTaskStatus.CodeRed or WorkTaskStatus.Stopped or WorkTaskStatus.Failed) => true,
        (WorkTaskStatus.Validating, WorkTaskStatus.RepairRequired or WorkTaskStatus.ReviewReady or WorkTaskStatus.Blocked or WorkTaskStatus.CodeRed or WorkTaskStatus.Stopped or WorkTaskStatus.Failed) => true,
        (WorkTaskStatus.RepairRequired, WorkTaskStatus.Implementing or WorkTaskStatus.Blocked or WorkTaskStatus.CodeRed or WorkTaskStatus.Stopped or WorkTaskStatus.Failed) => true,
        (WorkTaskStatus.Blocked, WorkTaskStatus.Claimed or WorkTaskStatus.Stopped or WorkTaskStatus.Failed) => true,
        (WorkTaskStatus.CodeRed, WorkTaskStatus.Investigating or WorkTaskStatus.Implementing or WorkTaskStatus.Validating or WorkTaskStatus.Blocked or WorkTaskStatus.ReviewReady or WorkTaskStatus.Stopped or WorkTaskStatus.Failed) => true,
        (WorkTaskStatus.ReviewReady, WorkTaskStatus.HandedOff or WorkTaskStatus.Stopped) => true,
        _ => false
    };

    private sealed class TaskState(WorkTaskRequest request)
    {
        private readonly List<WorkTaskEvent> _events = new();
        private TaskState(WorkTaskSnapshot snapshot) : this(snapshot.Request.Validate())
        {
            Request = snapshot.Request.Validate();
            Status = snapshot.Status;
            Lease = snapshot.Lease;
            Attempt = snapshot.Attempt;
            _events.AddRange(snapshot.Events);
        }
        public static TaskState Restore(WorkTaskSnapshot snapshot) => new(snapshot);
        public WorkTaskRequest Request { get; } = request;
        public WorkTaskStatus Status { get; set; } = WorkTaskStatus.Queued;
        public WorkTaskLease? Lease { get; set; }
        public int Attempt { get; set; }
        public void AddEvent(WorkTaskStatus previous, WorkTaskStatus current, string agent, Guid lease,
            DateTimeOffset at, string reason) =>
            _events.Add(new WorkTaskEvent(_events.Count + 1, previous, current, agent, lease, at, reason));
        public WorkTaskSnapshot Snapshot() =>
            new(Request, Status, Lease, Attempt, Array.AsReadOnly(_events.ToArray()));
    }
}
