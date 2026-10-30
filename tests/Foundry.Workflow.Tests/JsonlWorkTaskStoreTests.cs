using TheSingularityWorkshop.Foundry;
using Xunit;

namespace Foundry.Workflow.Tests;

public sealed class JsonlWorkTaskStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ReopensFromJournalAndRetainsEverySnapshot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-{Guid.NewGuid():N}.jsonl");
        try
        {
            var coordinator = new WorkTaskCoordinator();
            var initial = coordinator.Submit(Request());
            var store = new JsonlWorkTaskStore(path);
            store.Create(initial);

            var claimed = coordinator.Claim("task-1", "agent-a", TimeSpan.FromMinutes(5), Now);
            store.Append(claimed);
            var investigating = coordinator.Transition("task-1", claimed.Lease!.LeaseId,
                WorkTaskStatus.Investigating, Now.AddSeconds(1), "inspect task");
            store.Append(investigating);

            var reopened = new JsonlWorkTaskStore(path);
            var recovered = reopened.Get("task-1");
            Assert.Equal(WorkTaskStatus.Investigating, recovered.Status);
            Assert.Equal("agent-a", recovered.Lease!.AgentId);
            Assert.Equal(2, recovered.Events.Count);
            Assert.Equal(2, File.ReadAllLines(path).Length);
            Assert.Equal(1, reopened.GetAll().Count);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void RejectsDuplicateCreationAndNonAppendOnlyHistory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-{Guid.NewGuid():N}.jsonl");
        try
        {
            var coordinator = new WorkTaskCoordinator();
            var initial = coordinator.Submit(Request());
            var store = new JsonlWorkTaskStore(path);
            store.Create(initial);
            Assert.Throws<InvalidOperationException>(() => store.Create(initial));
            var claimed = coordinator.Claim("task-1", "agent-a", TimeSpan.FromMinutes(5), Now);
            store.Append(claimed);
            Assert.Throws<InvalidOperationException>(() => store.Append(claimed));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void RejectsCorruptJournalInsteadOfSilentlyDroppingHistory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllText(path, "{not-json}\n");
            Assert.Throws<InvalidDataException>(() => new JsonlWorkTaskStore(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static WorkTaskRequest Request() => new(1, "task-1", "correlation-1",
        "TrentBest/TheSingularityWorkshop.Foundry", 8,
        "https://github.com/TrentBest/TheSingularityWorkshop.Foundry/issues/8",
        new string('a', 40), new[] { "src/Foundry.Workflow" }, 3);
}
