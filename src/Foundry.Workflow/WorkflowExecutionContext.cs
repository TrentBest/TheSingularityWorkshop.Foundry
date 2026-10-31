using System.Text.Json;

namespace TheSingularityWorkshop.Foundry;

/// <summary>
/// Bounded, per-run JSON data exchanged by registered workflow handlers.
/// Inputs are immutable snapshots; outputs can be written once by host handlers.
/// Values carry data only and never grant permission to perform an operation.
/// </summary>
public sealed class WorkflowExecutionContext
{
    public const int MaximumEntries = 128;
    public const int MaximumValueBytes = 64 * 1024;
    public const int MaximumTotalBytes = 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new() { MaxDepth = 32 };

    private readonly object _gate = new();
    private readonly Dictionary<string, JsonElement> _inputs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, JsonElement> _outputs = new(StringComparer.Ordinal);
    private int _totalBytes;

    public WorkflowExecutionContext()
    {
    }

    /// <summary>Creates a context from caller-owned JSON values, cloning every value.</summary>
    public WorkflowExecutionContext(IReadOnlyDictionary<string, JsonElement> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        if (inputs.Count > MaximumEntries)
            throw new ArgumentException("Workflow context contains too many entries.", nameof(inputs));

        foreach (var (key, value) in inputs)
        {
            ValidateKey(key);
            var clone = value.Clone();
            var size = JsonSerializer.SerializeToUtf8Bytes(clone, JsonOptions).Length;
            if (size > MaximumValueBytes || _totalBytes + size > MaximumTotalBytes)
                throw new ArgumentException("Workflow context input exceeds its size budget.", nameof(inputs));
            _inputs.Add(key, clone);
            _totalBytes += size;
        }
    }

    public bool TryGetInput<T>(string key, out T? value)
    {
        ValidateKey(key);
        if (!_inputs.TryGetValue(key, out var element))
        {
            value = default;
            return false;
        }

        value = element.Deserialize<T>(JsonOptions);
        return true;
    }

    public T? GetInput<T>(string key) =>
        TryGetInput<T>(key, out var value)
            ? value
            : throw new KeyNotFoundException($"Workflow input '{key}' was not supplied.");

    /// <summary>Stores one bounded host-produced output for later workflow steps.</summary>
    public void SetOutput<T>(string key, T value)
    {
        ValidateKey(key);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        if (bytes.Length > MaximumValueBytes)
            throw new ArgumentException("Workflow output exceeds the per-value size budget.", nameof(value));
        using var document = JsonDocument.Parse(bytes);
        var element = document.RootElement.Clone();

        lock (_gate)
        {
            if (_inputs.ContainsKey(key) || _outputs.ContainsKey(key))
                throw new InvalidOperationException($"Workflow context key '{key}' is already defined.");
            if (_inputs.Count + _outputs.Count >= MaximumEntries ||
                _totalBytes + bytes.Length > MaximumTotalBytes)
                throw new InvalidOperationException("Workflow context has exhausted its size budget.");

            _outputs.Add(key, element);
            _totalBytes += bytes.Length;
        }
    }

    public bool TryGetOutput<T>(string key, out T? value)
    {
        ValidateKey(key);
        lock (_gate)
        {
            if (!_outputs.TryGetValue(key, out var element))
            {
                value = default;
                return false;
            }

            value = element.Deserialize<T>(JsonOptions);
            return true;
        }
    }

    public T? GetOutput<T>(string key) =>
        TryGetOutput<T>(key, out var value)
            ? value
            : throw new KeyNotFoundException($"Workflow output '{key}' has not been produced.");

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 128 ||
            key.Any(char.IsControl))
            throw new ArgumentException("Workflow context keys must be non-empty, bounded, and control-free.", nameof(key));
    }
}
