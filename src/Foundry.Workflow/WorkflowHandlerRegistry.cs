namespace TheSingularityWorkshop.Foundry;

/// <summary>Host-owned allowlist of operations workflow data may request.</summary>
public sealed class WorkflowHandlerRegistry
{
    private readonly Dictionary<string, Func<CancellationToken, ValueTask>> _handlers =
        new(StringComparer.Ordinal);

    public void Register(string key, Func<CancellationToken, ValueTask> handler)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Handler key must be non-empty.", nameof(key));
        ArgumentNullException.ThrowIfNull(handler);
        if (!_handlers.TryAdd(key, handler))
            throw new InvalidOperationException($"A handler named '{key}' is already registered.");
    }

    internal bool TryGet(string key, out Func<CancellationToken, ValueTask>? handler) =>
        _handlers.TryGetValue(key, out handler);
}
