using TheSingularityWorkshop.Foundry;
using Xunit;

namespace Foundry.Workflow.Tests;

public sealed class WorkTaskCapabilityRegistryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 20, 0, 0, TimeSpan.Zero);
    private static readonly string BaseSha = new('a', 40);
    private static readonly string CandidateSha = new('b', 40);

    private static WorkTaskRequest Request() => new(1, "task-8", "correlation-8",
        "TrentBest/TheSingularityWorkshop.Foundry", 8,
        "https://github.com/TrentBest/TheSingularityWorkshop.Foundry/issues/8",
        BaseSha, new[] { "src/Foundry.Workflow", "tests/Foundry.Workflow.Tests" }, 3);

    private static WorkTaskSnapshot ActiveTask(TimeSpan? leaseDuration = null)
    {
        var coordinator = new WorkTaskCoordinator();
        coordinator.Submit(Request());
        return coordinator.Claim("task-8", "agent-a", leaseDuration ?? TimeSpan.FromMinutes(20), Now);
    }

    private static WorkTaskCapability Grant(WorkTaskCapabilityRegistry registry,
        WorkTaskSnapshot task, TimeSpan? lifetime = null) =>
        registry.Grant(task, "work/task-8", CandidateSha,
            new[] { "src/Foundry.Workflow" },
            new[] { WorkTaskAction.ReadRepository, WorkTaskAction.WriteFiles, WorkTaskAction.CreateCommit },
            Now, lifetime ?? TimeSpan.FromMinutes(10));

    private static void Demand(WorkTaskCapabilityRegistry registry, WorkTaskCapability capability,
        WorkTaskSnapshot task, WorkTaskAction action, string? path = null,
        DateTimeOffset? now = null, string? branch = null, string? candidateSha = null) =>
        registry.Demand(capability.CapabilityId, task, branch ?? "work/task-8",
            candidateSha ?? CandidateSha, action, path, now ?? Now.AddMinutes(1));

    [Fact]
    public void CapabilityAuthorizesOnlyExactTaskProvenanceLeaseActionsAndPaths()
    {
        var registry = new WorkTaskCapabilityRegistry();
        var task = ActiveTask();
        var capability = Grant(registry, task);

        Demand(registry, capability, task, WorkTaskAction.ReadRepository,
            "src/Foundry.Workflow/WorkTaskCoordinator.cs");
        Demand(registry, capability, task, WorkTaskAction.WriteFiles,
            "src/Foundry.Workflow/WorkTaskCoordinator.cs");
        Demand(registry, capability, task, WorkTaskAction.CreateCommit);

        Assert.Throws<UnauthorizedAccessException>(() => Demand(registry, capability,
            task with { Request = task.Request with { TaskId = "other-task" } },
            WorkTaskAction.WriteFiles, "src/Foundry.Workflow/file.cs"));
        Assert.Throws<UnauthorizedAccessException>(() => Demand(registry, capability,
            task with { Request = task.Request with { Repository = "Other/Repository" } },
            WorkTaskAction.WriteFiles, "src/Foundry.Workflow/file.cs"));
        Assert.Throws<UnauthorizedAccessException>(() => Demand(registry, capability,
            task with { Lease = task.Lease! with { AgentId = "agent-b" } },
            WorkTaskAction.WriteFiles, "src/Foundry.Workflow/file.cs"));
        Assert.Throws<UnauthorizedAccessException>(() => Demand(registry, capability,
            task with { Lease = task.Lease! with { LeaseId = Guid.NewGuid() } },
            WorkTaskAction.WriteFiles, "src/Foundry.Workflow/file.cs"));
        Assert.Throws<UnauthorizedAccessException>(() => Demand(registry, capability, task,
            WorkTaskAction.WriteFiles, "src/Foundry.Workflow/file.cs", branch: "work/other"));
        Assert.Throws<UnauthorizedAccessException>(() => Demand(registry, capability, task,
            WorkTaskAction.WriteFiles, "src/Foundry.Workflow/file.cs", candidateSha: new string('c', 40)));
        Assert.Throws<UnauthorizedAccessException>(() => Demand(registry, capability, task,
            WorkTaskAction.CreateDraftPullRequest));
        Assert.Throws<UnauthorizedAccessException>(() => Demand(registry, capability,
            task with { Status = WorkTaskStatus.Stopped, Lease = null }, WorkTaskAction.WriteFiles,
            "src/Foundry.Workflow/file.cs"));
    }

    [Fact]
    public void CapabilityRejectsEscapingPathsAndScopeExpansion()
    {
        var registry = new WorkTaskCapabilityRegistry();
        var task = ActiveTask();
        Assert.Throws<ArgumentException>(() => registry.Grant(task, "work/task-8", CandidateSha,
            new[] { "../secrets" }, new[] { WorkTaskAction.WriteFiles }, Now, TimeSpan.FromMinutes(5)));
        Assert.Throws<ArgumentException>(() => registry.Grant(task, "work/task-8", CandidateSha,
            new[] { "docs" }, new[] { WorkTaskAction.WriteFiles }, Now, TimeSpan.FromMinutes(5)));

        var capability = Grant(registry, task);
        Assert.Throws<UnauthorizedAccessException>(() => Demand(registry, capability, task,
            WorkTaskAction.WriteFiles, "src/Foundry.Workflow/../../secrets.txt"));
    }

    [Fact]
    public void CapabilityExpiresAndCanBeRevoked()
    {
        var registry = new WorkTaskCapabilityRegistry();
        var task = ActiveTask();
        var capability = Grant(registry, task, TimeSpan.FromMinutes(5));

        Assert.Throws<UnauthorizedAccessException>(() => Demand(registry, capability, task,
            WorkTaskAction.WriteFiles, "src/Foundry.Workflow/file.cs", Now.AddMinutes(5)));

        registry.Revoke(capability.CapabilityId);
        Assert.Throws<UnauthorizedAccessException>(() => Demand(registry, capability, task,
            WorkTaskAction.WriteFiles, "src/Foundry.Workflow/file.cs"));
        registry.Revoke(capability.CapabilityId);
    }

    [Fact]
    public void CapabilityCannotOutliveItsOwningAgentLease()
    {
        var registry = new WorkTaskCapabilityRegistry();
        var task = ActiveTask(TimeSpan.FromMinutes(2));
        var capability = Grant(registry, task, TimeSpan.FromMinutes(30));

        Assert.Equal(task.Lease!.ExpiresAt, capability.ExpiresAt);
        Assert.Throws<UnauthorizedAccessException>(() => Demand(registry, capability, task,
            WorkTaskAction.WriteFiles, "src/Foundry.Workflow/file.cs", task.Lease.ExpiresAt));
    }

    [Fact]
    public void CapabilityCannotBeIssuedWithoutAnActiveLease()
    {
        var registry = new WorkTaskCapabilityRegistry();
        var queued = new WorkTaskCoordinator().Submit(Request());
        Assert.Throws<UnauthorizedAccessException>(() => Grant(registry, queued));
    }

    [Fact]
    public void CapabilityRejectsCanonicalBranchesUnknownActionsAndExcessiveLifetime()
    {
        var registry = new WorkTaskCapabilityRegistry();
        var task = ActiveTask();
        Assert.Throws<ArgumentException>(() => registry.Grant(task, "development", CandidateSha,
            new[] { "src/Foundry.Workflow" }, new[] { WorkTaskAction.WriteFiles },
            Now, TimeSpan.FromMinutes(5)));
        Assert.Throws<ArgumentException>(() => registry.Grant(task, "work/task-8", CandidateSha,
            new[] { "src/Foundry.Workflow" }, new[] { (WorkTaskAction)99 },
            Now, TimeSpan.FromMinutes(5)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Grant(registry, task, TimeSpan.FromHours(2)));
    }
}
