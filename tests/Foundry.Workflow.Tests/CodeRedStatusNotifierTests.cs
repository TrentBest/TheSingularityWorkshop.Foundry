using TheSingularityWorkshop.Foundry;
using Xunit;

namespace Foundry.Workflow.Tests;

public sealed class CodeRedStatusNotifierTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 20, 0, 0, TimeSpan.Zero);
    private static readonly string Sha = new('a', 40);

    private static WorkTaskSnapshot ActiveTask()
    {
        var request = new WorkTaskRequest(1, "task-8", "correlation-8",
            "TrentBest/TheSingularityWorkshop.Foundry", 8,
            "https://github.com/TrentBest/TheSingularityWorkshop.Foundry/issues/8",
            Sha, new[] { "src/Foundry.Workflow" }, 3);
        var coordinator = new WorkTaskCoordinator();
        coordinator.Submit(request);
        var lease = coordinator.Claim("task-8", "agent-a", TimeSpan.FromMinutes(5), Now).Lease!;
        return coordinator.Transition("task-8", lease.LeaseId, WorkTaskStatus.CodeRed,
            Now.AddSeconds(1), "CI blocked");
    }

    private static CodeRedIncident Incident() => new(1, "incident-8",
        "TrentBest/TheSingularityWorkshop.Foundry", "task-8", 8, "correlation-8",
        "agent-a", "implementation", "Investigating CI failure", "work/task-8",
        Sha, 1234, 1, CodeRedSeverity.Critical, "Required workflow failed",
        3, Now, Now, null, true);

    [Fact]
    public async Task NotifierPublishesValidatedActiveAndClearedNotices()
    {
        var publisher = new FakePublisher();
        var notifier = new CodeRedStatusNotifier(publisher);

        await notifier.PublishActiveAsync(Incident(), ActiveTask(), Now.AddMinutes(1), Now.AddMinutes(3));
        var cleared = Incident().Clear(Now.AddMinutes(4));
        await notifier.PublishClearedAsync(cleared);

        Assert.Equal(2, publisher.Notices.Count);
        Assert.Equal(CodeRedNoticeStatus.Active, publisher.Notices[0].Status);
        Assert.Equal(CodeRedNoticeStatus.Cleared, publisher.Notices[1].Status);
    }

    [Fact]
    public async Task NotifierDoesNotPublishAnActiveNoticeWhenTheTaskIsNotInCodeRed()
    {
        var publisher = new FakePublisher();
        var notifier = new CodeRedStatusNotifier(publisher);
        var task = ActiveTask();
        var investigating = new WorkTaskCoordinator();
        var request = task.Request;
        investigating.Submit(request);
        var lease = investigating.Claim("task-8", "agent-a", TimeSpan.FromMinutes(5), Now).Lease!;
        var nonCodeRed = investigating.Transition("task-8", lease.LeaseId,
            WorkTaskStatus.Investigating, Now.AddSeconds(1), "investigating");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await notifier.PublishActiveAsync(Incident(), nonCodeRed, Now.AddMinutes(1), Now.AddMinutes(2)));
        Assert.Empty(publisher.Notices);
    }

    [Fact]
    public async Task NotifierPropagatesHostDeliveryFailure()
    {
        var notifier = new CodeRedStatusNotifier(new FakePublisher
        {
            Failure = new IOException("notification transport unavailable")
        });

        await Assert.ThrowsAsync<IOException>(async () =>
            await notifier.PublishClearedAsync(Incident().Clear(Now.AddMinutes(2))));
    }

    private sealed class FakePublisher : ICodeRedStatusPublisher
    {
        public List<CodeRedStatusNotice> Notices { get; } = new();
        public Exception? Failure { get; init; }

        public ValueTask PublishAsync(CodeRedStatusNotice notice, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Failure is not null) throw Failure;
            Notices.Add(notice);
            return ValueTask.CompletedTask;
        }
    }
}
