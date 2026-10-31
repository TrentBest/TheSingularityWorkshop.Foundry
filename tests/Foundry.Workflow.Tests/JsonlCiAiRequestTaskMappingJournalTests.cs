using TheSingularityWorkshop.Foundry;
using Xunit;

namespace Foundry.Workflow.Tests;

public sealed class JsonlCiAiRequestTaskMappingJournalTests
{
    private static readonly DateTimeOffset Created = new(2026, 10, 31, 0, 0, 0, TimeSpan.Zero);

    private static CiAiRequestTaskMapping Mapping(string requestId = "request-1") =>
        new(1, requestId, "operation-1", "idempotency-1", "task-1", "correlation-1",
            "TrentBest/TheSingularityWorkshop.Foundry", "refs/heads/work/task-1",
            new string('a', 40), ".github/workflows/build.yml", "refs/heads/development",
            new string('b', 40), "build", "Resolve candidate", "Verify candidate",
            "ciai-production", "https://artifacts.example.test/",
            "https://ciai.example.test/api/v1/integrations/ciai/artifact-receipts",
            "ciai-ack-profile", Created, Created.AddHours(2));

    private static CiAiDispatchRunBinding Binding() =>
        new(12345, 1, "TrentBest/TheSingularityWorkshop.Foundry",
            ".github/workflows/build.yml", "refs/heads/development", new string('b', 40),
            "refs/heads/work/task-1", new string('a', 40), "workflow_dispatch", Created.AddMinutes(1));

    [Fact]
    public void DispatchIntentMustBeDurableBeforeExactRunCanBecomeActionable()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-request-map-{Guid.NewGuid():N}.jsonl");
        try
        {
            var journal = new JsonlCiAiRequestTaskMappingJournal(path);
            Assert.Equal(CiAiRequestMappingAppendOutcome.Appended, journal.CreateDispatchIntent(Mapping()));
            Assert.Throws<InvalidOperationException>(() => journal.GetActionable("request-1", Created.AddMinutes(1)));

            var recovered = new JsonlCiAiRequestTaskMappingJournal(path);
            Assert.Null(recovered.Get("request-1").RunBinding);
            Assert.Equal(CiAiRequestMappingAppendOutcome.Appended,
                recovered.BindRun("request-1", Binding(), Created.AddMinutes(1)));
            var actionable = recovered.GetActionable("request-1", Created.AddMinutes(2));
            Assert.Equal(12345, actionable.RunBinding!.RunId);

            var reopened = new JsonlCiAiRequestTaskMappingJournal(path);
            Assert.Equal(actionable, reopened.GetActionable("request-1", Created.AddMinutes(2)));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void ExactBindingIsIdempotentButDifferentBindingCannotReplaceIt()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-request-map-{Guid.NewGuid():N}.jsonl");
        try
        {
            var journal = new JsonlCiAiRequestTaskMappingJournal(path);
            journal.CreateDispatchIntent(Mapping());
            Assert.Equal(CiAiRequestMappingAppendOutcome.Appended,
                journal.BindRun("request-1", Binding(), Created.AddMinutes(1)));
            Assert.Equal(CiAiRequestMappingAppendOutcome.Duplicate,
                journal.BindRun("request-1", Binding(), Created.AddMinutes(1)));
            Assert.Equal(CiAiRequestMappingAppendOutcome.Conflict,
                journal.BindRun("request-1", Binding() with { RunId = 12346 }, Created.AddMinutes(1)));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Theory]
    [InlineData("refs/heads/development")]
    [InlineData("refs/heads/master")]
    public void CanonicalCandidateRefsAreRejected(string candidateRef)
    {
        Assert.Throws<ArgumentException>(() => (Mapping() with { CandidateRef = candidateRef }).Validate());
    }

    [Fact]
    public void RunBindingRejectsWrongWorkflowOrCandidateProvenance()
    {
        var mapping = Mapping();
        Assert.Throws<InvalidOperationException>(() =>
            (Binding() with { WorkflowSha = new string('c', 40) }).ValidateAgainst(mapping));
        Assert.Throws<InvalidOperationException>(() =>
            (Binding() with { CandidateSha = new string('d', 40) }).ValidateAgainst(mapping));
        Assert.Throws<InvalidOperationException>(() =>
            (Binding() with { Event = "push" }).ValidateAgainst(mapping));
    }

    [Fact]
    public void ExpiredOrUnboundRequestCannotAuthorizeCallback()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-request-map-{Guid.NewGuid():N}.jsonl");
        try
        {
            var journal = new JsonlCiAiRequestTaskMappingJournal(path);
            journal.CreateDispatchIntent(Mapping());
            journal.BindRun("request-1", Binding(), Created.AddMinutes(1));
            Assert.Throws<InvalidOperationException>(() =>
                journal.GetActionable("request-1", Created.AddHours(3)));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void JournalRejectsTornFinalRecord()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-request-map-torn-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(
                new CiAiRequestTaskMappingSnapshot(Mapping(), null, false)));
            Assert.Throws<InvalidDataException>(() => new JsonlCiAiRequestTaskMappingJournal(path));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
