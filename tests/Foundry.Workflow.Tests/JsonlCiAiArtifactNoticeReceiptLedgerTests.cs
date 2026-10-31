using TheSingularityWorkshop.Foundry;
using Xunit;

namespace Foundry.Workflow.Tests;

public sealed class JsonlCiAiArtifactNoticeReceiptLedgerTests
{
    private static CiAiArtifactReadyNotice Notice(string id = "notice-1", string key = "request-artifact-1", char digest = 'a') =>
        new("ciai-sender", id, key, new string(digest, 64));

    [Fact]
    public void AcceptsOnceAndReturnsTheSameDurableReceiptForAnExactRetryAfterRestart()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundry-ciai-receipts-{Guid.NewGuid():N}.jsonl");
        try
        {
            var acceptedAt = new DateTimeOffset(2026, 10, 31, 1, 0, 0, TimeSpan.Zero);
            var ledger = new JsonlCiAiArtifactNoticeReceiptLedger(path);
            var first = ledger.Accept(Notice(), acceptedAt);
            Assert.Equal(CiAiArtifactNoticeReceiptReceiptOutcome.Accepted, first.Outcome);

            var duplicate = new JsonlCiAiArtifactNoticeReceiptLedger(path).Accept(Notice(), acceptedAt.AddMinutes(1));
            Assert.Equal(CiAiArtifactNoticeReceiptReceiptOutcome.Duplicate, duplicate.Outcome);
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
            var changedPayload = ledger.Accept(Notice(digest: 'b'), acceptedAt.AddSeconds(1));
            var changedKey = ledger.Accept(Notice(key: "other-key"), acceptedAt.AddSeconds(2));

            Assert.Equal(CiAiArtifactNoticeReceiptReceiptOutcome.Accepted, first.Outcome);
            Assert.Equal(CiAiArtifactNoticeReceiptReceiptOutcome.Conflict, changedPayload.Outcome);
            Assert.Equal(CiAiArtifactNoticeReceiptReceiptOutcome.Conflict, changedKey.Outcome);
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
            Assert.Equal(CiAiArtifactNoticeReceiptReceiptOutcome.Accepted,
                ledger.Accept(Notice(), acceptedAt).Outcome);
            Assert.Equal(CiAiArtifactNoticeReceiptReceiptOutcome.Accepted,
                ledger.Accept(Notice() with { SenderIdentity = "other-sender" }, acceptedAt).Outcome);
            Assert.Throws<ArgumentException>(() => ledger.Accept(Notice() with { NoticeId = "bad id" }, acceptedAt));
            Assert.Throws<ArgumentException>(() => ledger.Accept(Notice() with { PayloadSha256 = "bad" }, acceptedAt));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void RejectsTornAndDuplicateJournalRecordsOnReplay()
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
