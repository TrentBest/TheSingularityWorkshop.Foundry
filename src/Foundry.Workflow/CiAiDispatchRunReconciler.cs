namespace TheSingularityWorkshop.Foundry;

/// <summary>
/// A run candidate assembled by a host adapter from independently observed
/// GitHub metadata and a parsed marker retrieved from that exact run/attempt.
/// The marker must never come from the callback notice or a search query.
/// </summary>
public sealed record CiAiDispatchRunObservation(
    CiAiDispatchRunBinding Binding,
    CiAiDispatchRunMarker Marker);

public enum CiAiDispatchReconciliationOutcome
{
    Bound,
    Duplicate,
    NoMatch,
    Ambiguous,
    Conflict
}

public sealed record CiAiDispatchReconciliationResult(
    CiAiDispatchReconciliationOutcome Outcome,
    CiAiDispatchRunBinding? Binding);

/// <summary>
/// Deterministically reconciles a persisted dispatch intent with independently
/// observed exact-run metadata plus the trusted workflow's request marker.
/// Zero or multiple matches remain non-actionable; this component never guesses.
/// </summary>
public static class CiAiDispatchRunReconciler
{
    public static CiAiDispatchReconciliationResult Reconcile(
        JsonlCiAiRequestTaskMappingJournal journal,
        string requestId,
        IEnumerable<CiAiDispatchRunObservation> observations,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
        ArgumentNullException.ThrowIfNull(observations);
        if (now == default)
            throw new ArgumentException("A current observation time is required.", nameof(now));

        var snapshot = journal.Get(requestId);
        var mapping = snapshot.Mapping;
        if (snapshot.Cancelled || now < mapping.CreatedAt || now > mapping.ExpiresAt)
            return new(CiAiDispatchReconciliationOutcome.NoMatch, null);

        var matches = new List<CiAiDispatchRunBinding>();
        foreach (var observation in observations)
        {
            if (observation is null || observation.Binding is null || observation.Marker is null ||
                observation.Marker.RequestId != mapping.RequestId ||
                observation.Marker.OperationId != mapping.OperationId ||
                observation.Marker.IdempotencyKey != mapping.IdempotencyKey ||
                observation.Marker.RunId != observation.Binding.RunId ||
                observation.Marker.RunAttempt != observation.Binding.RunAttempt)
                continue;

            try
            {
                observation.Binding.ValidateAgainst(mapping);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            if (observation.Binding.BoundAt > now)
                continue;

            matches.Add(observation.Binding);
        }

        var unique = matches
            .GroupBy(binding => (binding.Repository.ToUpperInvariant(), binding.RunId, binding.RunAttempt))
            .Select(group => group.Distinct().ToArray())
            .ToArray();

        if (unique.Length == 0)
            return new(CiAiDispatchReconciliationOutcome.NoMatch, null);
        if (unique.Length != 1 || unique[0].Length != 1)
            return new(CiAiDispatchReconciliationOutcome.Ambiguous, null);

        var binding = unique[0][0];
        var outcome = journal.BindRun(requestId, binding, now);
        return outcome switch
        {
            CiAiRequestMappingAppendOutcome.Appended =>
                new(CiAiDispatchReconciliationOutcome.Bound, binding),
            CiAiRequestMappingAppendOutcome.Duplicate =>
                new(CiAiDispatchReconciliationOutcome.Duplicate, binding),
            _ => new(CiAiDispatchReconciliationOutcome.Conflict, null)
        };
    }
}
