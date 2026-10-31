namespace TheSingularityWorkshop.Foundry;

/// <summary>Host-owned allowlist of operations workflow data may request.</summary>
public sealed class WorkflowHandlerRegistry
{
    private readonly Dictionary<string, Func<WorkflowExecutionContext, CancellationToken, ValueTask>> _handlers =
        new(StringComparer.Ordinal);

    /// <summary>Registers a legacy handler that does not consume workflow context.</summary>
    public void Register(string key, Func<CancellationToken, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        Register(key, (_, cancellationToken) => handler(cancellationToken));
    }

    /// <summary>Registers a context-aware handler under a host-approved key.</summary>
    public void Register(
        string key, Func<WorkflowExecutionContext, CancellationToken, ValueTask> handler)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Handler key must be non-empty.", nameof(key));
        ArgumentNullException.ThrowIfNull(handler);
        if (!_handlers.TryAdd(key, handler))
            throw new InvalidOperationException($"A handler named '{key}' is already registered.");
    }

    internal bool TryGet(
        string key, out Func<WorkflowExecutionContext, CancellationToken, ValueTask>? handler) =>
        _handlers.TryGetValue(key, out handler);
}
