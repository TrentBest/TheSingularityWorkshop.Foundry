using TheSingularityWorkshop.Foundry;
using Xunit;

namespace Foundry.Workflow.Tests;

public sealed class CodeRedStatusNoticeTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 30, 20, 0, 0, TimeSpan.Zero);
    private static readonly string Sha = new('a', 40);

    private static WorkTaskSnapshot CodeRedTask()
    {
        var request = new WorkTaskRequest(1, "foundry-8", "correlation-8",
            "TrentBest/TheSingularityWorkshop.Foundry", 8,
            "https://github.com/TrentBest/TheSingularityWorkshop.Foundry/issues/8",
            Sha, new[] { "src/Foundry.Workflow" }, 3);
        var coordinator = new WorkTaskCoordinator();
        coordinator.Submit(request);
        var lease = coordinator.Claim("foundry-8", "agent-a", TimeSpan.FromMinutes(5), Start).Lease!;
        return coordinator.Transition("foundry-8", lease.LeaseId,
            WorkTaskStatus.CodeRed, Start.AddSeconds(1), "required CI failed");
    }

    private static CodeRedIncident Incident() => new(1, "incident-8",
        "TrentBest/TheSingularityWorkshop.Foundry", "foundry-8", 8, "correlation-8",
        "agent-a", "implementation", "Investigating CI failure", "work/code-red-task-lifecycle",
        Sha, 1234, 1, CodeRedSeverity.Critical, "Required workflow failed",
        3, Start, Start, null, true);

    [Fact]
    public void ActiveNoticeRequiresLiveMatchingCodeRedTaskAndIncludesOperationalFacts()
    {
        var task = CodeRedTask();
        var notice = CodeRedStatusNoticeFactory.CreateActive(Incident(), task,
            Start.AddMinutes(1), Start.AddMinutes(3));

        Assert.Equal(CodeRedNoticeStatus.Active, notice.Status);
        Assert.Equal("agent-a", notice.AgentId);
        Assert.Equal(1234, notice.CiRunId);
        Assert.Equal(1, notice.CiAttempt);
        Assert.Contains("currently engaged with a CODE RED", notice.Message);
        Assert.Contains("issue #8", notice.Message);
        Assert.Contains(Sha, notice.Message);
        Assert.Contains("Required workflow failed", notice.Message);
        Assert.Contains("Next update:", notice.Message);
        Assert.True(notice.HumanEscalationRequired);
    }

    [Fact]
    public void ActiveNoticeCannotBePublishedForIdleExpiredOrMismatchedWork()
    {
        var task = CodeRedTask();
        Assert.Throws<InvalidOperationException>(() => CodeRedStatusNoticeFactory.CreateActive(
            Incident(), task, task.Lease!.ExpiresAt, task.Lease.ExpiresAt.AddMinutes(1)));
        var investigatingEvents = task.Events.ToArray();
        investigatingEvents[^1] = investigatingEvents[^1] with { Current = WorkTaskStatus.Investigating };
        var investigating = task with
        {
            Status = WorkTaskStatus.Investigating,
            Events = Array.AsReadOnly(investigatingEvents)
        };
        Assert.Throws<InvalidOperationException>(() => CodeRedStatusNoticeFactory.CreateActive(
            Incident(), investigating, Start.AddMinutes(1), Start.AddMinutes(2)));
        Assert.Throws<ArgumentException>(() => CodeRedStatusNoticeFactory.CreateActive(
            Incident() with { AgentId = "agent-b" }, task,
            Start.AddMinutes(1), Start.AddMinutes(2)));
    }

    [Fact]
    public void ClearedNoticeRequiresExplicitClearAndDoesNotClaimActiveWork()
    {
        var cleared = Incident().Clear(Start.AddMinutes(2));
        var notice = CodeRedStatusNoticeFactory.CreateCleared(cleared);

        Assert.Equal(CodeRedNoticeStatus.Cleared, notice.Status);
        Assert.False(notice.HumanEscalationRequired);
        Assert.Contains("CODE RED cleared", notice.Message);
        Assert.DoesNotContain("currently engaged", notice.Message);
        Assert.Throws<InvalidOperationException>(() =>
            CodeRedStatusNoticeFactory.CreateCleared(Incident()));
    }
}
