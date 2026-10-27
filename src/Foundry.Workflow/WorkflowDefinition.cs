namespace TheSingularityWorkshop.Foundry;

/// <summary>A data-only workflow definition. Version 1 supports sequential, named-handler steps.</summary>
public sealed class WorkflowDefinition
{
    public const int SupportedSchemaVersion = 1;

    public WorkflowDefinition(string id, string name, int schemaVersion, IEnumerable<WorkflowStep> steps)
    {
        Id = id ?? string.Empty;
        Name = name ?? string.Empty;
        SchemaVersion = schemaVersion;
        ArgumentNullException.ThrowIfNull(steps);
        Steps = Array.AsReadOnly(steps.Select(step => step ??
            throw new ArgumentException("Workflow steps cannot contain null.", nameof(steps))).ToArray());
    }

    public string Id { get; }
    public string Name { get; }
    public int SchemaVersion { get; }
    public IReadOnlyList<WorkflowStep> Steps { get; }
}

/// <summary>A named unit of work routed to a handler registered by the host.</summary>
public sealed record WorkflowStep(string Id, string HandlerKey);
