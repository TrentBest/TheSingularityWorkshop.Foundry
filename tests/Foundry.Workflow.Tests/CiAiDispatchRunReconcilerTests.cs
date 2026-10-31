using TheSingularityWorkshop.Foundry;
using Xunit;

namespace Foundry.Workflow.Tests;

public sealed class CiAiDispatchRunReconcilerTests
{
    private static readonly DateTimeOffset Created = new(2026, 10, 31, 0, 0, 0, TimeSpan.Zero);

    private static CiAiRequestTaskMapping Mapping() =>
        new(1, "request-1", "operation-1", "idempotency-1", "task-1", "correlation-1",
            "TrentBest/TheSingularityWorkshop.Foundry", "refs/heads/work/task-1",
            new string('a', 40), ".github/workflows/build.yml", "refs/heads/development",
            new string('b', 40), "build", "Resolve candidate", "Verify candidate",
            "ciai-production", "https://artifacts.example.test/",
            "https://ciai.example.test/api/v1/integrations/ciai/artifact-receipts",
            "ciai-ack-profile", Created, Created.AddHours(2));

    private static CiAiDispatchRunBinding Binding(long runId = 12345) =>
        new(runId, 1, "TrentBest/TheSingularityWorkshop.Foundry",
            ".github/workflows/build.yml", "refs/heads/development", new string('b', 40),
            "refs/heads/work/task-1", new string('a', 40), "workflow_dispatch", Created.AddMinutes(1));

    private static CiAiDispatchRunObservation Observation(long runId = 12345) =>
        new(Binding(runId), "request-1", "operation-1", "idempotency-1");

    [Fact]
    public void ReconcilesOnlyOneExactRunWithTrustedRequestIdentity()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-reconcile-{Guid.NewGuid():N}.jsonl");
        try
        {
            var journal = new JsonlCiAiRequestTaskMappingJournal(path);
            journal.CreateDispatchIntent(Mapping());

            var result = CiAiDispatchRunReconciler.Reconcile(
                journal, "request-1",
                new[] { Observation(12344) with { RequestId = "other-request" }, Observation() },
                Created.AddMinutes(2));

            Assert.Equal(CiAiDispatchReconciliationOutcome.Bound, result.Outcome);
            Assert.Equal(12345, result.Binding!.RunId);
            Assert.Equal(12345, journal.GetActionable("request-1", Created.AddMinutes(2)).RunBinding!.RunId);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void NoIdentityMatchLeavesIntentUnbound()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-reconcile-{Guid.NewGuid():N}.jsonl");
        try
        {
            var journal = new JsonlCiAiRequestTaskMappingJournal(path);
            journal.CreateDispatchIntent(Mapping());

            var result = CiAiDispatchRunReconciler.Reconcile(
                journal, "request-1",
                new[] { Observation() with { IdempotencyKey = "wrong-key" } },
                Created.AddMinutes(2));

            Assert.Equal(CiAiDispatchReconciliationOutcome.NoMatch, result.Outcome);
            Assert.Null(journal.Get("request-1").RunBinding);
            Assert.Throws<InvalidOperationException>(() =>
                journal.GetActionable("request-1", Created.AddMinutes(2)));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void MultipleExactRunsAreAmbiguousAndNeverGuessed()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-reconcile-{Guid.NewGuid():N}.jsonl");
        try
        {
            var journal = new JsonlCiAiRequestTaskMappingJournal(path);
            journal.CreateDispatchIntent(Mapping());

            var result = CiAiDispatchRunReconciler.Reconcile(
                journal, "request-1",
                new[] { Observation(12345), Observation(12346) },
                Created.AddMinutes(2));

            Assert.Equal(CiAiDispatchReconciliationOutcome.Ambiguous, result.Outcome);
            Assert.Null(journal.Get("request-1").RunBinding);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void FutureObservationIsNotEligibleForReconciliation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-reconcile-{Guid.NewGuid():N}.jsonl");
        try
        {
            var journal = new JsonlCiAiRequestTaskMappingJournal(path);
            journal.CreateDispatchIntent(Mapping());

            var result = CiAiDispatchRunReconciler.Reconcile(
                journal, "request-1",
                new[] { new CiAiDispatchRunObservation(
                    Binding() with { BoundAt = Created.AddMinutes(3) },
                    "request-1", "operation-1", "idempotency-1") },
                Created.AddMinutes(2));

            Assert.Equal(CiAiDispatchReconciliationOutcome.NoMatch, result.Outcome);
            Assert.Null(journal.Get("request-1").RunBinding);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void ExactReconciliationRetryIsIdempotent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-reconcile-{Guid.NewGuid():N}.jsonl");
        try
        {
            var journal = new JsonlCiAiRequestTaskMappingJournal(path);
            journal.CreateDispatchIntent(Mapping());

            Assert.Equal(CiAiDispatchReconciliationOutcome.Bound,
                CiAiDispatchRunReconciler.Reconcile(journal, "request-1",
                    new[] { Observation() }, Created.AddMinutes(2)).Outcome);
            Assert.Equal(CiAiDispatchReconciliationOutcome.Duplicate,
                CiAiDispatchRunReconciler.Reconcile(journal, "request-1",
                    new[] { Observation() }, Created.AddMinutes(2)).Outcome);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
