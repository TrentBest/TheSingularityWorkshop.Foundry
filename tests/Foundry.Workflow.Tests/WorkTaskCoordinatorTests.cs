using TheSingularityWorkshop.Foundry;
using Xunit;

namespace Foundry.Workflow.Tests;

public sealed class WorkTaskCoordinatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SubmitValidatesAndRetainsVersionedTaskProvenance()
    {
        var coordinator = new WorkTaskCoordinator();
        var snapshot = coordinator.Submit(Request());
        Assert.Equal(1, snapshot.Request.SchemaVersion);
        Assert.Equal("TrentBest/TheSingularityWorkshop.Foundry", snapshot.Request.Repository);
        Assert.Equal(WorkTaskStatus.Queued, snapshot.Status);
        Assert.Empty(snapshot.Events);
    }

    [Fact]
    public void ClaimConflictAndExpiredLeaseAreDeterministic()
    {
        var coordinator = new WorkTaskCoordinator();
        coordinator.Submit(Request());
        var claimed = coordinator.Claim("foundry-8", "agent-a", TimeSpan.FromMinutes(2), Now);
        Assert.Throws<InvalidOperationException>(() =>
            coordinator.Claim("foundry-8", "agent-b", TimeSpan.FromMinutes(2), Now.AddMinutes(1)));
        var reclaimed = coordinator.Claim("foundry-8", "agent-b", TimeSpan.FromMinutes(2), Now.AddMinutes(3));
        Assert.Equal("agent-b", reclaimed.Lease!.AgentId);
        Assert.Equal(2, reclaimed.Events.Count);
        Assert.Throws<InvalidOperationException>(() =>
            coordinator.Transition("foundry-8", claimed.Lease!.LeaseId,
                WorkTaskStatus.Investigating, Now.AddMinutes(3), "stale owner"));
    }

    [Fact]
    public void LifecycleTransitionsAreValidatedAndAudited()
    {
        var coordinator = new WorkTaskCoordinator();
        coordinator.Submit(Request());
        var lease = coordinator.Claim("foundry-8", "agent-a", TimeSpan.FromMinutes(10), Now).Lease!.LeaseId;
        Assert.Throws<InvalidOperationException>(() =>
            coordinator.Transition("foundry-8", lease, WorkTaskStatus.ReviewReady, Now, "skip steps"));
        coordinator.Transition("foundry-8", lease, WorkTaskStatus.Investigating, Now, "inspect issue");
        coordinator.Transition("foundry-8", lease, WorkTaskStatus.Implementing, Now, "implement bounded slice");
        var validating = coordinator.Transition("foundry-8", lease, WorkTaskStatus.Validating, Now, "run checks");
        Assert.Equal(1, validating.Attempt);
        Assert.Equal(4, validating.Events.Count);
        Assert.Equal(4L, validating.Events[^1].Sequence);
    }

    [Fact]
    public void RetryBudgetExhaustionBlocksInsteadOfLooping()
    {
        var coordinator = new WorkTaskCoordinator();
        coordinator.Submit(Request(maximumAttempts: 1));
        var lease = coordinator.Claim("foundry-8", "agent-a", TimeSpan.FromMinutes(10), Now).Lease!.LeaseId;
        coordinator.Transition("foundry-8", lease, WorkTaskStatus.Investigating, Now, "inspect");
        coordinator.Transition("foundry-8", lease, WorkTaskStatus.Implementing, Now, "change");
        coordinator.Transition("foundry-8", lease, WorkTaskStatus.Validating, Now, "validate");
        var blocked = coordinator.Transition("foundry-8", lease, WorkTaskStatus.RepairRequired, Now, "budget exhausted");
        Assert.Equal(WorkTaskStatus.Blocked, blocked.Status);
        Assert.Equal(1, blocked.Attempt);
    }

    [Fact]
    public void DuplicateTaskAndUnsafeScopeAreRejected()
    {
        var coordinator = new WorkTaskCoordinator();
        coordinator.Submit(Request());
        Assert.Throws<InvalidOperationException>(() => coordinator.Submit(Request()));
        Assert.Throws<ArgumentException>(() => coordinator.Submit(Request(paths: new[] { "../secrets" })));
        Assert.Throws<ArgumentException>(() => coordinator.Submit(Request(baseSha: "abc")));
    }

    [Fact]
    public void TerminalHandoffReleasesLease()
    {
        var coordinator = new WorkTaskCoordinator();
        coordinator.Submit(Request());
        var lease = coordinator.Claim("foundry-8", "agent-a", TimeSpan.FromMinutes(10), Now).Lease!.LeaseId;
        coordinator.Transition("foundry-8", lease, WorkTaskStatus.Investigating, Now, "inspect");
        coordinator.Transition("foundry-8", lease, WorkTaskStatus.Implementing, Now, "implement");
        coordinator.Transition("foundry-8", lease, WorkTaskStatus.Validating, Now, "validate");
        coordinator.Transition("foundry-8", lease, WorkTaskStatus.ReviewReady, Now, "checks passed");
        var handedOff = coordinator.Transition("foundry-8", lease, WorkTaskStatus.HandedOff, Now, "draft PR ready");
        Assert.Equal(WorkTaskStatus.HandedOff, handedOff.Status);
        Assert.Null(handedOff.Lease);
        Assert.Equal(6, handedOff.Events.Count);
    }

    private static WorkTaskRequest Request(int maximumAttempts = 3, IReadOnlyList<string>? paths = null, string? baseSha = null) =>
        new(1, "foundry-8", "correlation-8", "TrentBest/TheSingularityWorkshop.Foundry", 8,
            "https://github.com/TrentBest/TheSingularityWorkshop.Foundry/issues/8",
            baseSha ?? new string('a', 40), paths ?? new[] { "src/Foundry.Workflow", "tests/Foundry.Workflow.Tests" },
            maximumAttempts);
}
