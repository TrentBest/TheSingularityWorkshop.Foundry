using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace TheSingularityWorkshop.Foundry;

/// <summary>
/// A normalized, already-authenticated artifact-ready notice. The HTTP adapter must
/// pass the exact received UTF-8 request body; the ledger computes and retains its digest.
/// This record intentionally contains no URL, credential, or artifact bytes.
/// </summary>
public sealed record CiAiArtifactReadyNotice(
    string SenderIdentity,
    string NoticeId,
    string IdempotencyKey,
    string PayloadUtf8)
{
    public string PayloadSha256 => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(PayloadUtf8 ?? string.Empty))).ToLowerInvariant();

    public CiAiArtifactReadyNotice Validate()
    {
        ValidateText(SenderIdentity, 128, nameof(SenderIdentity));
        ValidateText(NoticeId, 128, nameof(NoticeId));
        ValidateText(IdempotencyKey, 128, nameof(IdempotencyKey));
        if (string.IsNullOrEmpty(PayloadUtf8) || Encoding.UTF8.GetByteCount(PayloadUtf8) > 64 * 1024)
            throw new ArgumentException("The exact UTF-8 notice body must be non-empty and no larger than 64 KiB.", nameof(PayloadUtf8));
        try
        {
            using var document = JsonDocument.Parse(PayloadUtf8);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("noticeId", out var noticeId) || noticeId.GetString() != NoticeId ||
                !root.TryGetProperty("manifest", out var manifest) || manifest.ValueKind != JsonValueKind.Object ||
                !manifest.TryGetProperty("idempotencyKey", out var idempotencyKey) || idempotencyKey.GetString() != IdempotencyKey)
                throw new ArgumentException("The exact notice body must bind noticeId and manifest.idempotencyKey to the normalized receipt identity.", nameof(PayloadUtf8));
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("The exact notice body must be valid UTF-8 JSON.", nameof(PayloadUtf8), exception);
        }
        return this;
    }

    private static void ValidateText(string value, int maximumLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength ||
            value.Any(character => char.IsControl(character) || char.IsWhiteSpace(character)))
            throw new ArgumentException("Notice identity fields must be bounded, non-empty, and contain no whitespace or control characters.", name);
    }
}

public enum CiAiArtifactNoticeReceiptOutcome
{
    Accepted,
    Duplicate,
    Conflict
}

public sealed record CiAiArtifactNoticeReceipt(
    int SchemaVersion,
    string ReceiptId,
    string SenderIdentity,
    string NoticeId,
    string IdempotencyKey,
    string PayloadSha256,
    string PayloadUtf8,
    DateTimeOffset AcceptedAt)
{
    public const int CurrentSchemaVersion = 1;
}

/// <summary>
/// Append-only durable receipt ledger for authenticated and normalized ready notices.
/// It persists before returning Accepted and returns the original receipt for exact
/// idempotent retries. This implementation is single-process; it is not a distributed
/// or cross-process ingress lock.
/// </summary>
public sealed class JsonlCiAiArtifactNoticeReceiptLedger
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly object _gate = new();
    private readonly string _path;
    private readonly Dictionary<string, CiAiArtifactNoticeReceipt> _bySenderAndKey = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CiAiArtifactNoticeReceipt> _bySenderAndNotice = new(StringComparer.Ordinal);

    public JsonlCiAiArtifactNoticeReceiptLedger(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        if (File.Exists(_path))
            Replay();
    }

    public (CiAiArtifactNoticeReceiptOutcome Outcome, CiAiArtifactNoticeReceipt Receipt) Accept(
        CiAiArtifactReadyNotice notice, DateTimeOffset acceptedAt)
    {
        ArgumentNullException.ThrowIfNull(notice);
        notice.Validate();
        if (acceptedAt == default)
            throw new ArgumentException("A receipt acceptance timestamp is required.", nameof(acceptedAt));

        lock (_gate)
        {
            var key = Scope(notice.SenderIdentity, notice.IdempotencyKey);
            var noticeKey = Scope(notice.SenderIdentity, notice.NoticeId);

            if (_bySenderAndKey.TryGetValue(key, out var existingByKey))
            {
                if (!string.Equals(existingByKey.PayloadSha256, notice.PayloadSha256, StringComparison.OrdinalIgnoreCase))
                    return (CiAiArtifactNoticeReceiptOutcome.Conflict, existingByKey);
                if (_bySenderAndNotice.TryGetValue(noticeKey, out var existingByNotice) &&
                    existingByNotice != existingByKey)
                    return (CiAiArtifactNoticeReceiptOutcome.Conflict, existingByKey);
                return (CiAiArtifactNoticeReceiptOutcome.Duplicate, existingByKey);
            }

            if (_bySenderAndNotice.TryGetValue(noticeKey, out var existingByNoticeId))
                return (CiAiArtifactNoticeReceiptOutcome.Conflict, existingByNoticeId);

            var receipt = new CiAiArtifactNoticeReceipt(
                CiAiArtifactNoticeReceipt.SchemaVersion,
                Guid.NewGuid().ToString("N"),
                notice.SenderIdentity,
                notice.NoticeId,
                notice.IdempotencyKey,
                notice.PayloadSha256,
                notice.PayloadUtf8,
                acceptedAt);
            ValidateReceipt(receipt);
            WriteLine(receipt);
            _bySenderAndKey.Add(key, receipt);
            _bySenderAndNotice.Add(noticeKey, receipt);
            return (CiAiArtifactNoticeReceiptOutcome.Accepted, receipt);
        }
    }

    public IReadOnlyList<CiAiArtifactNoticeReceipt> GetAll()
    {
        lock (_gate)
            return Array.AsReadOnly(_bySenderAndKey.Values
                .OrderBy(receipt => receipt.AcceptedAt)
                .ThenBy(receipt => receipt.ReceiptId, StringComparer.Ordinal)
                .ToArray());
    }

    private void Replay()
    {
        using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > 0)
        {
            stream.Position = stream.Length - 1;
            if (stream.ReadByte() != 10)
                throw new InvalidDataException("CiAi ingress receipt ledger has an incomplete final record; refusing to discard receipt history.");
            stream.Position = 0;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        string? line;
        var lineNumber = 0;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                throw new InvalidDataException($"CiAi ingress receipt ledger contains an empty record at line {lineNumber}.");

            CiAiArtifactNoticeReceipt receipt;
            try
            {
                receipt = JsonSerializer.Deserialize<CiAiArtifactNoticeReceipt>(line, JsonOptions)
                    ?? throw new InvalidDataException($"CiAi ingress receipt record {lineNumber} is null.");
                ValidateReceipt(receipt);
            }
            catch (Exception exception) when (exception is JsonException or ArgumentException)
            {
                throw new InvalidDataException($"CiAi ingress receipt record {lineNumber} is invalid.", exception);
            }

            var key = Scope(receipt.SenderIdentity, receipt.IdempotencyKey);
            var noticeKey = Scope(receipt.SenderIdentity, receipt.NoticeId);
            if (_bySenderAndKey.ContainsKey(key) || _bySenderAndNotice.ContainsKey(noticeKey) ||
                _bySenderAndKey.Values.Any(existing => existing.ReceiptId == receipt.ReceiptId))
                throw new InvalidDataException($"CiAi ingress receipt history contains a duplicate identity at line {lineNumber}.");
            _bySenderAndKey.Add(key, receipt);
            _bySenderAndNotice.Add(noticeKey, receipt);
        }
    }

    private void WriteLine(CiAiArtifactNoticeReceipt receipt)
    {
        var json = JsonSerializer.Serialize(receipt, JsonOptions);
        using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.WriteLine(json);
        writer.Flush();
        stream.Flush(flushToDisk: true);
    }

    private static void ValidateReceipt(CiAiArtifactNoticeReceipt receipt)
    {
        if (receipt.SchemaVersion != CiAiArtifactNoticeReceipt.SchemaVersion ||
            string.IsNullOrWhiteSpace(receipt.ReceiptId) || receipt.ReceiptId.Length != 32 ||
            !Guid.TryParseExact(receipt.ReceiptId, "N", out _) ||
            receipt.AcceptedAt == default)
            throw new ArgumentException("Ingress receipt schema, identifier, or timestamp is invalid.");
        new CiAiArtifactReadyNotice(receipt.SenderIdentity, receipt.NoticeId,
            receipt.IdempotencyKey, receipt.PayloadUtf8).Validate();
        if (!string.Equals(receipt.PayloadSha256, new CiAiArtifactReadyNotice(receipt.SenderIdentity,
            receipt.NoticeId, receipt.IdempotencyKey, receipt.PayloadUtf8).PayloadSha256, StringComparison.Ordinal))
            throw new ArgumentException("Ingress receipt payload digest does not match its exact stored bytes.");
    }

    private static string Scope(string sender, string identity) => sender + "\0" + identity;
}
