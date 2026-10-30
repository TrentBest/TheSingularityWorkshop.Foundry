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

    [Fact]
    public void CapabilityAuthorizesOnlyExactTaskProvenanceActionsAndPaths()
    {
        var registry = new WorkTaskCapabilityRegistry();
        var capability = registry.Grant(Request(), "work/task-8", CandidateSha,
            new[] { "src/Foundry.Workflow" },
            new[] { WorkTaskAction.ReadRepository, WorkTaskAction.WriteFiles, WorkTaskAction.CreateCommit },
            Now, TimeSpan.FromMinutes(10));

        registry.Demand(capability.CapabilityId, "task-8", Request().Repository, "work/task-8",
            BaseSha, CandidateSha, WorkTaskAction.ReadRepository,
            "src/Foundry.Workflow/WorkTaskCoordinator.cs", Now.AddMinutes(1));
        registry.Demand(capability.CapabilityId, "task-8", Request().Repository, "work/task-8",
            BaseSha, CandidateSha, WorkTaskAction.WriteFiles,
            "src/Foundry.Workflow/WorkTaskCoordinator.cs", Now.AddMinutes(1));
        registry.Demand(capability.CapabilityId, "task-8", Request().Repository, "work/task-8",
            BaseSha, CandidateSha, WorkTaskAction.CreateCommit, null, Now.AddMinutes(1));

        Assert.Throws<UnauthorizedAccessException>(() => registry.Demand(capability.CapabilityId,
            "other-task", Request().Repository, "work/task-8", BaseSha, CandidateSha,
            WorkTaskAction.WriteFiles, "src/Foundry.Workflow/file.cs", Now.AddMinutes(1)));
        Assert.Throws<UnauthorizedAccessException>(() => registry.Demand(capability.CapabilityId,
            "task-8", "Other/Repository", "work/task-8", BaseSha, CandidateSha,
            WorkTaskAction.WriteFiles, "src/Foundry.Workflow/file.cs", Now.AddMinutes(1)));
        Assert.Throws<UnauthorizedAccessException>(() => registry.Demand(capability.CapabilityId,
            "task-8", Request().Repository, "work/other", BaseSha, CandidateSha,
            WorkTaskAction.WriteFiles, "src/Foundry.Workflow/file.cs", Now.AddMinutes(1)));
        Assert.Throws<UnauthorizedAccessException>(() => registry.Demand(capability.CapabilityId,
            "task-8", Request().Repository, "work/task-8", BaseSha, new string('c', 40),
            WorkTaskAction.WriteFiles, "src/Foundry.Workflow/file.cs", Now.AddMinutes(1)));
        Assert.Throws<UnauthorizedAccessException>(() => registry.Demand(capability.CapabilityId,
            "task-8", Request().Repository, "work/task-8", BaseSha, CandidateSha,
            WorkTaskAction.CreateDraftPullRequest, null, Now.AddMinutes(1)));
    }

    [Fact]
    public void CapabilityRejectsEscapingPathsAndScopeExpansion()
    {
        var registry = new WorkTaskCapabilityRegistry();
        Assert.Throws<ArgumentException>(() => registry.Grant(Request(), "work/task-8", CandidateSha,
            new[] { "../secrets" }, new[] { WorkTaskAction.WriteFiles }, Now, TimeSpan.FromMinutes(5)));
        Assert.Throws<ArgumentException>(() => registry.Grant(Request(), "work/task-8", CandidateSha,
            new[] { "docs" }, new[] { WorkTaskAction.WriteFiles }, Now, TimeSpan.FromMinutes(5)));

        var capability = registry.Grant(Request(), "work/task-8", CandidateSha,
            new[] { "src/Foundry.Workflow" }, new[] { WorkTaskAction.WriteFiles },
            Now, TimeSpan.FromMinutes(5));
        Assert.Throws<UnauthorizedAccessException>(() => registry.Demand(capability.CapabilityId,
            "task-8", Request().Repository, "work/task-8", BaseSha, CandidateSha,
            WorkTaskAction.WriteFiles, "src/Foundry.Workflow/../../secrets.txt", Now.AddMinutes(1)));
    }

    [Fact]
    public void CapabilityExpiresAndCanBeRevoked()
    {
        var registry = new WorkTaskCapabilityRegistry();
        var capability = registry.Grant(Request(), "work/task-8", CandidateSha,
            new[] { "src/Foundry.Workflow" }, new[] { WorkTaskAction.WriteFiles },
            Now, TimeSpan.FromMinutes(5));

        Assert.Throws<UnauthorizedAccessException>(() => registry.Demand(capability.CapabilityId,
            "task-8", Request().Repository, "work/task-8", BaseSha, CandidateSha,
            WorkTaskAction.WriteFiles, "src/Foundry.Workflow/file.cs", Now.AddMinutes(5)));

        registry.Revoke(capability.CapabilityId);
        Assert.Throws<UnauthorizedAccessException>(() => registry.Demand(capability.CapabilityId,
            "task-8", Request().Repository, "work/task-8", BaseSha, CandidateSha,
            WorkTaskAction.WriteFiles, "src/Foundry.Workflow/file.cs", Now.AddMinutes(1)));
        registry.Revoke(capability.CapabilityId);
    }

    [Fact]
    public void CapabilityRejectsCanonicalBranchesUnknownActionsAndExcessiveLifetime()
    {
        var registry = new WorkTaskCapabilityRegistry();
        Assert.Throws<ArgumentException>(() => registry.Grant(Request(), "development", CandidateSha,
            new[] { "src/Foundry.Workflow" }, new[] { WorkTaskAction.WriteFiles },
            Now, TimeSpan.FromMinutes(5)));
        Assert.Throws<ArgumentException>(() => registry.Grant(Request(), "work/task-8", CandidateSha,
            new[] { "src/Foundry.Workflow" }, new[] { (WorkTaskAction)99 },
            Now, TimeSpan.FromMinutes(5)));
        Assert.Throws<ArgumentOutOfRangeException>(() => registry.Grant(Request(), "work/task-8", CandidateSha,
            new[] { "src/Foundry.Workflow" }, new[] { WorkTaskAction.WriteFiles },
            Now, TimeSpan.FromHours(2)));
    }
}
