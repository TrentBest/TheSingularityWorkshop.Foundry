namespace TheSingularityWorkshop.Foundry;

/// <summary>Executes validated steps sequentially through explicitly registered host handlers.</summary>
public sealed class WorkflowRunner(WorkflowHandlerRegistry handlers)
{
    private readonly WorkflowHandlerRegistry _handlers =
        handlers ?? throw new ArgumentNullException(nameof(handlers));

    public ValueTask<WorkflowResult> RunAsync(
        WorkflowDefinition? definition, CancellationToken cancellationToken = default) =>
        RunAsync(definition, new WorkflowExecutionContext(), cancellationToken);

    public async ValueTask<WorkflowResult> RunAsync(
        WorkflowDefinition? definition, WorkflowExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var diagnostics = WorkflowValidator.Validate(definition, _handlers);
        if (diagnostics.Count != 0)
            return new WorkflowResult(definition?.Id ?? string.Empty, WorkflowStatus.Invalid,
                Array.Empty<StepResult>(), diagnostics);

        var validDefinition = definition!;
        var results = new List<StepResult>(validDefinition.Steps.Count);
        var cancelled = false;
        var failed = false;

        foreach (var step in validDefinition.Steps)
        {
            if (cancelled || failed)
            {
                results.Add(new(step.Id, step.HandlerKey, StepStatus.Skipped));
                continue;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                cancelled = true;
                results.Add(new(step.Id, step.HandlerKey, StepStatus.Cancelled));
                continue;
            }

            _handlers.TryGet(step.HandlerKey, out var handler);
            try
            {
                await handler!(context, cancellationToken).ConfigureAwait(false);
                results.Add(new(step.Id, step.HandlerKey, StepStatus.Succeeded));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancelled = true;
                results.Add(new(step.Id, step.HandlerKey, StepStatus.Cancelled));
            }
            catch (Exception exception)
            {
                failed = true;
                results.Add(new(step.Id, step.HandlerKey, StepStatus.Failed,
                    $"Handler failed with {exception.GetType().Name}."));
            }
        }

        var status = cancelled ? WorkflowStatus.Cancelled :
            failed ? WorkflowStatus.Failed : WorkflowStatus.Succeeded;
        return new WorkflowResult(validDefinition.Id, status, results.AsReadOnly(),
            Array.Empty<WorkflowDiagnostic>());
    }
}
