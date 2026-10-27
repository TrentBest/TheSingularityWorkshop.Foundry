namespace TheSingularityWorkshop.Foundry;

/// <summary>Validates all definition-level constraints before any handler can run.</summary>
public static class WorkflowValidator
{
    public static IReadOnlyList<WorkflowDiagnostic> Validate(
        WorkflowDefinition? definition, WorkflowHandlerRegistry handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        var diagnostics = new List<WorkflowDiagnostic>();
        if (definition is null)
        {
            diagnostics.Add(new("WF001", "Workflow definition is required."));
            return diagnostics.AsReadOnly();
        }

        if (string.IsNullOrWhiteSpace(definition.Id))
            diagnostics.Add(new("WF002", "Workflow ID must be non-empty."));
        if (string.IsNullOrWhiteSpace(definition.Name))
            diagnostics.Add(new("WF003", "Workflow name must be non-empty."));
        if (definition.SchemaVersion != WorkflowDefinition.SupportedSchemaVersion)
            diagnostics.Add(new("WF004", $"Unsupported workflow schema version '{definition.SchemaVersion}'."));
        if (definition.Steps.Count == 0)
            diagnostics.Add(new("WF005", "Workflow must contain at least one step."));

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in definition.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Id))
                diagnostics.Add(new("WF006", "Step ID must be non-empty."));
            else if (!ids.Add(step.Id))
                diagnostics.Add(new("WF007", $"Duplicate step ID '{step.Id}'.", step.Id));

            if (string.IsNullOrWhiteSpace(step.HandlerKey))
                diagnostics.Add(new("WF008", "Step handler key must be non-empty.", step.Id));
            else if (!handlers.TryGet(step.HandlerKey, out _))
                diagnostics.Add(new("WF009", $"No host handler is registered for '{step.HandlerKey}'.", step.Id));
        }

        return diagnostics.AsReadOnly();
    }
}
