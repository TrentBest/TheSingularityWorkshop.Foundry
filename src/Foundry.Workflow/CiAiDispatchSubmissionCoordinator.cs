namespace TheSingularityWorkshop.Foundry;

/// <summary>
/// Host boundary for submitting an already-persisted CI workflow intent.
/// Implementations own GitHub credentials and transport; they must not infer
/// workflow trust from the candidate ref or from caller-supplied run metadata.
/// </summary>
public interface ICiAiWorkflowDispatcher
{
    /// <summary>
    /// Submits the registered workflow using the exact trusted workflow and
    /// candidate provenance in the mapping. GitHub dispatch normally does not
    /// return the run ID; callers must reconcile the resulting run separately.
    /// </summary>
    Task DispatchAsync(CiAiRequestTaskMapping mapping, CancellationToken cancellationToken);
}

public enum CiAiDispatchSubmissionOutcome
{
    SubmittedAwaitingReconciliation,
    ExistingIntentRequiresReconciliation,
    Conflict
}

public sealed record CiAiDispatchSubmissionResult(
    CiAiDispatchSubmissionOutcome Outcome,
    string RequestId);

/// <summary>
/// Persists a dispatch intent before crossing the host/network boundary.
/// It never resubmits an existing intent: after restart or an ambiguous transport
/// failure, the host must reconcile the exact run marker before considering a
/// separately authorized new dispatch.
/// </summary>
public static class CiAiDispatchSubmissionCoordinator
{
    public static async Task<CiAiDispatchSubmissionResult> SubmitAsync(
        JsonlCiAiRequestTaskMappingJournal journal,
        CiAiRequestTaskMapping mapping,
        ICiAiWorkflowDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(dispatcher);
        mapping.Validate();

        var intent = journal.CreateDispatchIntent(mapping);
        if (intent == CiAiRequestMappingAppendOutcome.Conflict)
            return new(CiAiDispatchSubmissionOutcome.Conflict, mapping.RequestId);
        if (intent == CiAiRequestMappingAppendOutcome.Duplicate)
            return new(CiAiDispatchSubmissionOutcome.ExistingIntentRequiresReconciliation, mapping.RequestId);

        // The durable intent exists before any external side effect. If this call
        // throws or is cancelled, its outcome may be ambiguous; do not resubmit it.
        await dispatcher.DispatchAsync(mapping, cancellationToken).ConfigureAwait(false);
        return new(CiAiDispatchSubmissionOutcome.SubmittedAwaitingReconciliation, mapping.RequestId);
    }
}
