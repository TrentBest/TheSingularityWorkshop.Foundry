namespace TheSingularityWorkshop.Foundry;

/// <summary>
/// Serializes local incident snapshot selection with persistent task recovery so an incident
/// update or clear cannot race the receipt's incident-sequence check and task-journal append.
/// Human authentication and receipt issuance remain host responsibilities.
/// </summary>
public sealed class PersistentCodeRedRecoveryCoordinator
{
    private readonly JsonlCodeRedIncidentStore _incidents;
    private readonly PersistentWorkTaskCoordinator _tasks;

    public PersistentCodeRedRecoveryCoordinator(JsonlCodeRedIncidentStore incidents,
        PersistentWorkTaskCoordinator tasks)
    {
        _incidents = incidents ?? throw new ArgumentNullException(nameof(incidents));
        _tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
    }

    public WorkTaskSnapshot RecoverFromCodeRed(string taskId, Guid leaseId, string incidentId,
        CodeRedRecoveryApprovalReceipt receipt, DateTimeOffset now, string reason)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        return _incidents.ExecuteWithActiveIncident(incidentId, incident =>
            _tasks.RecoverFromCodeRed(taskId, leaseId, incident, receipt, now, reason));
    }
}
