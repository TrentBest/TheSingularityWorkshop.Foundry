using TheSingularityWorkshop.Foundry;
using Xunit;

namespace Foundry.Workflow.Tests;

public sealed class JsonlCiAiArtifactNoticeReceiptLedgerTests
{
    private static CiAiArtifactReadyNotice Notice(string id = "notice-1", string key = "request-artifact-1") =>
        new("ciai-sender", id, key,
            System.Text.Json.JsonSerializer.Serialize(new { noticeId = id, manifest = new { idempotencyKey = key } }));

    [Fact]
    public void AcceptsOnceAndReturnsTheSameDurableReceiptForAnExactRetryAfterRestart()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-ciai-receipts-{Guid.NewGuid():N}.jsonl");
        try
        {
            var acceptedAt = new DateTimeOffset(2026, 10, 31, 1, 0, 0, TimeSpan.Zero);
            var ledger = new JsonlCiAiArtifactNoticeReceiptLedger(path);
            var first = ledger.Accept(Notice(), acceptedAt);
            Assert.Equal(CiAiArtifactNoticeReceiptOutcome.Accepted, first.Outcome);

            var duplicate = new JsonlCiAiArtifactNoticeReceiptLedger(path).Accept(Notice(), acceptedAt.AddMinutes(1));
            Assert.Equal(CiAiArtifactNoticeReceiptOutcome.Duplicate, duplicate.Outcome);
            Assert.Equal(first.Receipt, duplicate.Receipt);
            Assert.Single(new JsonlCiAiArtifactNoticeReceiptLedger(path).GetAll());
            Assert.Single(File.ReadAllLines(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void RejectsChangedPayloadForAnExistingKeyAndReusedNoticeId()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-ciai-receipts-{Guid.NewGuid():N}.jsonl");
        try
        {
            var ledger = new JsonlCiAiArtifactNoticeReceiptLedger(path);
            var acceptedAt = DateTimeOffset.UtcNow;
            var first = ledger.Accept(Notice(), acceptedAt);
            var changedPayload = ledger.Accept(Notice(id: "notice-2"), acceptedAt.AddSeconds(1));
            var changedKey = ledger.Accept(Notice(key: "other-key"), acceptedAt.AddSeconds(2));

            Assert.Equal(CiAiArtifactNoticeReceiptOutcome.Accepted, first.Outcome);
            Assert.Equal(CiAiArtifactNoticeReceiptOutcome.Conflict, changedPayload.Outcome);
            Assert.Equal(CiAiArtifactNoticeReceiptOutcome.Conflict, changedKey.Outcome);
            Assert.Single(ledger.GetAll());
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ScopesIdempotencyBySenderAndValidatesBoundedMetadata()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-ciai-receipts-{Guid.NewGuid():N}.jsonl");
        try
        {
            var ledger = new JsonlCiAiArtifactNoticeReceiptLedger(path);
            var acceptedAt = DateTimeOffset.UtcNow;
            Assert.Equal(CiAiArtifactNoticeReceiptOutcome.Accepted,
                ledger.Accept(Notice(), acceptedAt).Outcome);
            Assert.Equal(CiAiArtifactNoticeReceiptOutcome.Accepted,
                ledger.Accept(Notice() with { SenderIdentity = "other-sender" }, acceptedAt).Outcome);
            Assert.Throws<ArgumentException>(() => ledger.Accept(Notice() with { NoticeId = "bad id" }, acceptedAt));
            Assert.Throws<ArgumentException>(() => ledger.Accept(Notice() with { PayloadUtf8 = "not-json" }, acceptedAt));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void RejectsTornJournalOnReplay()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-ciai-receipts-{Guid.NewGuid():N}.jsonl");
        try
        {
            var ledger = new JsonlCiAiArtifactNoticeReceiptLedger(path);
            ledger.Accept(Notice(), DateTimeOffset.UtcNow);
            File.AppendAllText(path, "{");
            Assert.Throws<InvalidDataException>(() => new JsonlCiAiArtifactNoticeReceiptLedger(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
