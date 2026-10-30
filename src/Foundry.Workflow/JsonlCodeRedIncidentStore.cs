using System.Text;
using System.Text.Json;

namespace TheSingularityWorkshop.Foundry;

/// <summary>Append-only, single-process durable journal for CODE RED incident snapshots.</summary>
public sealed class JsonlCodeRedIncidentStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly object _gate = new();
    private readonly string _path;
    private readonly Dictionary<string, CodeRedIncident> _latest = new(StringComparer.Ordinal);

    public JsonlCodeRedIncidentStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A journal path is required.", nameof(path));
        _path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        if (File.Exists(_path))
            Replay();
    }

    public CodeRedIncident Create(CodeRedIncident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);
        incident.Validate();
        lock (_gate)
        {
            if (_latest.ContainsKey(incident.IncidentId))
                throw new InvalidOperationException($"Incident '{incident.IncidentId}' already exists.");
            if (incident.ClearedAt.HasValue || incident.UpdatedAt != incident.StartedAt)
                throw new ArgumentException("A new incident must be active and have matching start/update timestamps.", nameof(incident));
            WriteLine(incident);
            _latest.Add(incident.IncidentId, incident);
            return incident;
        }
    }

    public CodeRedIncident Update(CodeRedIncident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);
        incident.Validate();
        lock (_gate)
        {
            var previous = GetExisting(incident.IncidentId);
            ValidateUpdate(previous, incident);
            WriteLine(incident);
            _latest[incident.IncidentId] = incident;
            return incident;
        }
    }

    public CodeRedIncident Clear(CodeRedIncident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);
        incident.Validate();
        lock (_gate)
        {
            var previous = GetExisting(incident.IncidentId);
            ValidateClear(previous, incident);
            WriteLine(incident);
            _latest[incident.IncidentId] = incident;
            return incident;
        }
    }

    public CodeRedIncident Get(string incidentId)
    {
        lock (_gate)
            return GetExisting(incidentId);
    }

    public IReadOnlyList<CodeRedIncident> GetAll()
    {
        lock (_gate)
            return Array.AsReadOnly(_latest.Values.OrderBy(x => x.IncidentId, StringComparer.Ordinal).ToArray());
    }

    private CodeRedIncident GetExisting(string incidentId) =>
        _latest.TryGetValue(incidentId, out var incident)
            ? incident
            : throw new KeyNotFoundException($"Unknown incident '{incidentId}'.");

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
                throw new InvalidDataException($"Incident journal contains an empty record at line {lineNumber}.");
            CodeRedIncident incident;
            try
            {
                incident = JsonSerializer.Deserialize<CodeRedIncident>(line, JsonOptions)
                    ?? throw new InvalidDataException($"Incident journal record {lineNumber} is null.");
                incident.Validate();
            }
            catch (Exception exception) when (exception is JsonException or ArgumentException)
            {
                throw new InvalidDataException($"Incident journal record {lineNumber} is invalid.", exception);
            }

            if (!_latest.TryGetValue(incident.IncidentId, out var previous))
            {
                if (incident.ClearedAt.HasValue || incident.UpdatedAt != incident.StartedAt)
                    throw new InvalidDataException($"Incident '{incident.IncidentId}' does not begin with an active snapshot.");
            }
            else
            {
                try
                {
                    if (incident.ClearedAt.HasValue)
                        ValidateClear(previous, incident);
                    else
                        ValidateUpdate(previous, incident);
                }
                catch (ArgumentException exception)
                {
                    throw new InvalidDataException($"Incident history for '{incident.IncidentId}' is discontinuous.", exception);
                }
                catch (InvalidOperationException exception)
                {
                    throw new InvalidDataException($"Incident history for '{incident.IncidentId}' is discontinuous.", exception);
                }
            }
            _latest[incident.IncidentId] = incident;
        }
    }

    private void WriteLine(CodeRedIncident incident)
    {
        var json = JsonSerializer.Serialize(incident, JsonOptions);
        using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.WriteLine(json);
        writer.Flush();
        stream.Flush(flushToDisk: true);
    }

    private static void ValidateUpdate(CodeRedIncident previous, CodeRedIncident next)
    {
        if (previous.ClearedAt.HasValue || next.ClearedAt.HasValue || next.UpdatedAt <= previous.UpdatedAt ||
            !SameIdentity(previous, next) ||
            previous.HumanEscalationRequired != next.HumanEscalationRequired ||
            previous.CurrentAction == next.CurrentAction && previous.Reason == next.Reason &&
            previous.CandidateSha == next.CandidateSha && previous.LatestCiRunId == next.LatestCiRunId &&
            previous.LatestCiAttempt == next.LatestCiAttempt)
            throw new InvalidOperationException("Incident update must advance an active incident without changing its identity or repeating its state.");
    }

    private static void ValidateClear(CodeRedIncident previous, CodeRedIncident next)
    {
        if (previous.ClearedAt.HasValue || !next.ClearedAt.HasValue || next.UpdatedAt <= previous.UpdatedAt ||
            next.ClearedAt != next.UpdatedAt || !SameIdentity(previous, next) ||
            next.HumanEscalationRequired || previous.CurrentAction != next.CurrentAction ||
            previous.CandidateSha != next.CandidateSha || previous.LatestCiRunId != next.LatestCiRunId ||
            previous.LatestCiAttempt != next.LatestCiAttempt || previous.Reason != next.Reason)
            throw new InvalidOperationException("Incident clear must close the current active snapshot without rewriting its evidence.");
    }

    private static bool SameIdentity(CodeRedIncident left, CodeRedIncident right) =>
        left.SchemaVersion == right.SchemaVersion && left.IncidentId == right.IncidentId &&
        left.Repository == right.Repository && left.TaskId == right.TaskId &&
        left.IssueNumber == right.IssueNumber && left.CorrelationId == right.CorrelationId &&
        left.AgentId == right.AgentId && left.AgentRole == right.AgentRole &&
        left.Branch == right.Branch && left.Severity == right.Severity &&
        left.RetryBudget == right.RetryBudget && left.StartedAt == right.StartedAt;
}
