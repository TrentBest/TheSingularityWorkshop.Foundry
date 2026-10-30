using System.Text;
using System.Text.Json;

namespace TheSingularityWorkshop.Foundry;

/// <summary>
/// Append-only, single-process durable journal for task snapshots. Each accepted
/// snapshot is a complete recovery point; earlier snapshots are never overwritten.
/// </summary>
public sealed class JsonlWorkTaskStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly object _gate = new();
    private readonly string _path;
    private readonly Dictionary<string, WorkTaskSnapshot> _latest = new(StringComparer.Ordinal);

    public JsonlWorkTaskStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A journal path is required.", nameof(path));
        _path = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(directory);
        if (File.Exists(_path))
            Replay();
    }

    public WorkTaskSnapshot Create(WorkTaskSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ValidateSnapshot(snapshot);
        lock (_gate)
        {
            var id = snapshot.Request.TaskId;
            if (_latest.ContainsKey(id))
                throw new InvalidOperationException($"Task '{id}' already exists in the journal.");
            if (snapshot.Events.Count != 0 || snapshot.Status != WorkTaskStatus.Queued ||
                snapshot.Lease is not null || snapshot.Attempt != 0)
                throw new ArgumentException("A new journal task must be an untouched queued snapshot.", nameof(snapshot));
            WriteLine(snapshot);
            _latest.Add(id, Clone(snapshot));
            return Clone(snapshot);
        }
    }

    public WorkTaskSnapshot Append(WorkTaskSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ValidateSnapshot(snapshot);
        lock (_gate)
        {
            if (!_latest.TryGetValue(snapshot.Request.TaskId, out var previous))
                throw new KeyNotFoundException($"Task '{snapshot.Request.TaskId}' is not in the journal.");
            if (snapshot.Events.Count <= previous.Events.Count ||
                snapshot.Events.Count != previous.Events.Count + 1 ||
                !snapshot.Events.Take(previous.Events.Count).SequenceEqual(previous.Events))
                throw new InvalidOperationException("A journal append must add exactly one event to the existing history.");
            if (!SameRequest(snapshot.Request, previous.Request))
                throw new InvalidOperationException("Task provenance cannot change after creation.");
            WriteLine(snapshot);
            _latest[snapshot.Request.TaskId] = Clone(snapshot);
            return Clone(snapshot);
        }
    }

    public WorkTaskSnapshot Get(string taskId)
    {
        lock (_gate)
            return _latest.TryGetValue(taskId, out var snapshot)
                ? Clone(snapshot)
                : throw new KeyNotFoundException($"Unknown task '{taskId}'.");
    }

    public IReadOnlyList<WorkTaskSnapshot> GetAll()
    {
        lock (_gate)
            return Array.AsReadOnly(_latest.Values.OrderBy(x => x.Request.TaskId, StringComparer.Ordinal)
                .Select(Clone).ToArray());
    }

    private void Replay()
    {
        using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        string? line;
        var lineNumber = 0;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                throw new InvalidDataException($"Journal contains an empty record at line {lineNumber}.");
            WorkTaskSnapshot snapshot;
            try
            {
                snapshot = JsonSerializer.Deserialize<WorkTaskSnapshot>(line, JsonOptions)
                    ?? throw new InvalidDataException($"Journal record {lineNumber} is null.");
                ValidateSnapshot(snapshot);
            }
            catch (Exception exception) when (exception is JsonException or ArgumentException)
            {
                throw new InvalidDataException($"Journal record {lineNumber} is invalid.", exception);
            }

            var id = snapshot.Request.TaskId;
            if (!_latest.TryGetValue(id, out var previous))
            {
                if (snapshot.Events.Count != 0 || snapshot.Status != WorkTaskStatus.Queued ||
                    snapshot.Lease is not null || snapshot.Attempt != 0)
                    throw new InvalidDataException($"Journal task '{id}' does not begin with a queued snapshot.");
            }
            else if (snapshot.Events.Count != previous.Events.Count + 1 ||
                     !snapshot.Events.Take(previous.Events.Count).SequenceEqual(previous.Events) ||
                     !SameRequest(snapshot.Request, previous.Request))
            {
                throw new InvalidDataException($"Journal history for task '{id}' is discontinuous.");
            }
            _latest[id] = Clone(snapshot);
        }
    }

    private void WriteLine(WorkTaskSnapshot snapshot)
    {
        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.WriteLine(json);
        writer.Flush();
        stream.Flush(flushToDisk: true);
    }

    private static bool SameRequest(WorkTaskRequest left, WorkTaskRequest right) =>
        left.SchemaVersion == right.SchemaVersion && left.TaskId == right.TaskId &&
        left.CorrelationId == right.CorrelationId && left.Repository == right.Repository &&
        left.IssueNumber == right.IssueNumber && left.IssueUrl == right.IssueUrl &&
        left.BaseSha == right.BaseSha && left.MaximumAttempts == right.MaximumAttempts &&
        left.AllowedPaths.SequenceEqual(right.AllowedPaths, StringComparer.Ordinal);

    private static WorkTaskSnapshot Clone(WorkTaskSnapshot snapshot) =>
        snapshot with
        {
            Request = snapshot.Request with
            {
                AllowedPaths = Array.AsReadOnly(snapshot.Request.AllowedPaths.ToArray())
            },
            Events = Array.AsReadOnly(snapshot.Events.ToArray())
        };

    private static void ValidateSnapshot(WorkTaskSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot.Request);
        snapshot.Request.Validate();
        if (snapshot.Events is null || snapshot.Events.Select((item, index) => item is not null && item.Sequence == index + 1).Any(valid => !valid))
            throw new ArgumentException("Task event history must be contiguous and one-based.", nameof(snapshot));
        if (!Enum.IsDefined(snapshot.Status))
            throw new ArgumentException("Task status is invalid.", nameof(snapshot));
        if (snapshot.Attempt < 0 || snapshot.Attempt > snapshot.Request.MaximumAttempts)
            throw new ArgumentException("Task attempt is outside its configured budget.", nameof(snapshot));
        if (snapshot.Lease is { } lease &&
            (string.IsNullOrWhiteSpace(lease.AgentId) || lease.LeaseId == Guid.Empty))
            throw new ArgumentException("Task lease identity is invalid.", nameof(snapshot));
    }
}
