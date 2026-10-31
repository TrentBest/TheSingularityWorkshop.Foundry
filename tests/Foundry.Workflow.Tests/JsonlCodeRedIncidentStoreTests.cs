using TheSingularityWorkshop.Foundry;
using Xunit;

namespace Foundry.Workflow.Tests;

public sealed class JsonlCodeRedIncidentStoreTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 30, 20, 0, 0, TimeSpan.Zero);

    private static CodeRedIncident Incident() => new(1, "incident-8",
        "TrentBest/TheSingularityWorkshop.Foundry", "foundry-8", 8, "correlation-8",
        "agent-a", "implementation", "Investigating CI failure", "work/code-red-task-lifecycle",
        new string('a', 40), 1234, 1, CodeRedSeverity.Critical, "Required workflow failed",
        3, Start, Start, null, true);

    [Fact]
    public void ReopensLatestIncidentAndRetainsEverySnapshot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-incidents-{Guid.NewGuid():N}.jsonl");
        try
        {
            var store = new JsonlCodeRedIncidentStore(path);
            var created = store.Create(Incident());
            var updated = created.Update("Repairing CI failure", "Test assertion failed",
                new string('b', 40), 1235, 2, Start.AddMinutes(1));
            store.Update(updated);

            var reopened = new JsonlCodeRedIncidentStore(path);
            var recovered = reopened.Get("incident-8");
            Assert.Equal("Repairing CI failure", recovered.CurrentAction);
            Assert.Equal(1235, recovered.LatestCiRunId);
            Assert.Equal(2, recovered.LatestCiAttempt);
            Assert.Equal(2, File.ReadAllLines(path).Length);
            Assert.Single(reopened.GetAll());
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ClearIsDurableAndCannotBeUpdatedOrClearedAgain()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-incidents-{Guid.NewGuid():N}.jsonl");
        try
        {
            var store = new JsonlCodeRedIncidentStore(path);
            var created = store.Create(Incident());
            var cleared = created.Clear(Start.AddMinutes(1));
            store.Clear(cleared);

            var recovered = new JsonlCodeRedIncidentStore(path).Get("incident-8");
            Assert.Equal(CodeRedIncidentStatus.Cleared, recovered.Status);
            Assert.False(recovered.HumanEscalationRequired);
            Assert.Throws<InvalidOperationException>(() => store.Clear(cleared with
            {
                UpdatedAt = Start.AddMinutes(2),
                ClearedAt = Start.AddMinutes(2)
            }));
            Assert.Throws<InvalidOperationException>(() => store.Update(cleared));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void RejectsDuplicateCreationAndMutationOfIncidentIdentity()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-incidents-{Guid.NewGuid():N}.jsonl");
        try
        {
            var store = new JsonlCodeRedIncidentStore(path);
            var created = store.Create(Incident());
            Assert.Throws<InvalidOperationException>(() => store.Create(created));
            var changedIdentity = created with
            {
                Repository = "Other/Repository",
                UpdatedAt = Start.AddMinutes(1)
            };
            Assert.Throws<InvalidOperationException>(() => store.Update(changedIdentity));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void RejectsCorruptIncidentJournal()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-incidents-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllText(path, "{not-json}\n");
            Assert.Throws<InvalidDataException>(() => new JsonlCodeRedIncidentStore(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
