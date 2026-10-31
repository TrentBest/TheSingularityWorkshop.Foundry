using TheSingularityWorkshop.Foundry;
using Xunit;

namespace Foundry.Workflow.Tests;

public sealed class CodeRedIncidentTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 30, 20, 0, 0, TimeSpan.Zero);
    private static CodeRedIncident Incident() => new(1, "incident-8", "TrentBest/TheSingularityWorkshop.Foundry",
        "foundry-8", 8, "correlation-8", "agent-a", "implementation", "Investigating CI failure",
        "work/code-red-task-lifecycle", new string('a', 40), 1234, 1, CodeRedSeverity.Critical,
        "Required workflow failed", 3, Start, Start, null, true);

    [Fact]
    public void ValidIncidentCarriesOperationalOwnershipAndProvenance()
    {
        var incident = Incident().Validate();
        Assert.Equal(CodeRedIncidentStatus.Active, incident.Status);
        Assert.Equal(1234, incident.LatestCiRunId);
        Assert.True(incident.HumanEscalationRequired);
    }

    [Fact]
    public void CanonicalBranchAndPartialCiIdentityAreRejected()
    {
        Assert.Throws<ArgumentException>(() => (Incident() with { Branch = "development" }).Validate());
        Assert.Throws<ArgumentException>(() => (Incident() with { LatestCiAttempt = null }).Validate());
    }

    [Fact]
    public void UpdatesPreserveStartAndClearClosesEscalation()
    {
        var updatedAt = Start.AddMinutes(2);
        var updated = Incident().Update("Repairing failing test", "Null reference in workflow",
            new string('b', 40), 1235, 2, updatedAt);
        Assert.Equal(Start, updated.StartedAt);
        Assert.Equal(2, updated.LatestCiAttempt);
        var cleared = updated.Clear(updatedAt.AddMinutes(1));
        Assert.Equal(CodeRedIncidentStatus.Cleared, cleared.Status);
        Assert.False(cleared.HumanEscalationRequired);
        Assert.Throws<InvalidOperationException>(() => cleared.Update("again", "late", new string('c', 40),
            null, null, updatedAt.AddMinutes(2)));
    }


    [Fact]
    public void NullRepositoryAndUnknownSeverityAreRejectedWithoutRuntimeFaults()
    {
        Assert.Throws<ArgumentException>(() => (Incident() with { Repository = null! }).Validate());
        Assert.Throws<ArgumentException>(() => (Incident() with { Severity = (CodeRedSeverity)99 }).Validate());
    }

    [Fact]
    public void IncidentCannotMoveTimeBackwardsOrClearBeforeLatestUpdate()
    {
        var updated = Incident().Update("Testing", "Retry", new string('b', 40), 1235, 2, Start.AddMinutes(2));
        Assert.Throws<InvalidOperationException>(() => updated.Clear(Start.AddMinutes(1)));
        Assert.Throws<InvalidOperationException>(() => updated.Update("late", "late", new string('c', 40),
            null, null, Start.AddMinutes(1)));
    }
    [Fact]
    public void IncidentTextAndBranchFieldsAreBoundedAndControlFree()
    {
        Assert.Throws<ArgumentException>(() => (Incident() with { AgentId = new string('x', 129) }).Validate());
        Assert.Throws<ArgumentException>(() => (Incident() with { Reason = "failure\\nforged notice" }).Validate());
        Assert.Throws<ArgumentException>(() => (Incident() with { Branch = "work/../master" }).Validate());
        Assert.Throws<ArgumentException>(() => (Incident() with { CurrentAction = new string('x', 2049) }).Validate());
    }

}
