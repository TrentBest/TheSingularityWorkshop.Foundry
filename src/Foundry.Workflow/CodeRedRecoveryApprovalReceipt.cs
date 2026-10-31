using System.Text;
using System.Globalization;

namespace TheSingularityWorkshop.Foundry;

public enum CodeRedRecoveryAction
{
    ResumeInvestigation = 1
}

/// <summary>
/// A short-lived host-issued approval for one exact CODE RED recovery transition.
/// The signature covers <see cref="GetSigningPayload"/>; the signature itself is excluded.
/// </summary>
public sealed record CodeRedRecoveryApprovalReceipt(
    int SchemaVersion,
    string ReceiptId,
    string Issuer,
    string Principal,
    string TaskId,
    string IncidentId,
    long TaskEventSequence,
    long IncidentSequence,
    CodeRedRecoveryAction Action,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    string Signature)
{
    public const int CurrentSchemaVersion = 1;
    public static readonly TimeSpan MaximumLifetime = TimeSpan.FromMinutes(10);

    public CodeRedRecoveryApprovalReceipt Validate()
    {
        ValidateText(ReceiptId, nameof(ReceiptId), 128);
        ValidateText(Issuer, nameof(Issuer), 128);
        ValidateText(Principal, nameof(Principal), 128);
        ValidateText(TaskId, nameof(TaskId), 128);
        ValidateText(IncidentId, nameof(IncidentId), 128);
        ValidateText(Signature, nameof(Signature), 8192);
        if (SchemaVersion != CurrentSchemaVersion ||
            TaskEventSequence < 0 || IncidentSequence <= 0 ||
            Action != CodeRedRecoveryAction.ResumeInvestigation ||
            IssuedAt == default || ExpiresAt <= IssuedAt ||
            ExpiresAt - IssuedAt > MaximumLifetime)
            throw new ArgumentException("Recovery receipt schema, scope, action, or validity window is invalid.");
        return this;
    }

    /// <summary>
    /// Canonical UTF-8 signing payload. Every variable-length field is length-prefixed;
    /// timestamps are UTC ticks and the schema/action values are invariant decimal integers.
    /// </summary>
    public byte[] GetSigningPayload()
    {
        Validate();
        var fields = new[]
        {
            "foundry.code-red-recovery-receipt.v1",
            SchemaVersion.ToString(CultureInfo.InvariantCulture),
            ReceiptId, Issuer, Principal, TaskId, IncidentId,
            TaskEventSequence.ToString(CultureInfo.InvariantCulture),
            IncidentSequence.ToString(CultureInfo.InvariantCulture),
            ((int)Action).ToString(CultureInfo.InvariantCulture),
            IssuedAt.UtcDateTime.Ticks.ToString(CultureInfo.InvariantCulture),
            ExpiresAt.UtcDateTime.Ticks.ToString(CultureInfo.InvariantCulture)
        };
        var payload = new StringBuilder();
        foreach (var field in fields)
            payload.Append(Encoding.UTF8.GetByteCount(field).ToString(CultureInfo.InvariantCulture))
                .Append(':').Append(field).Append('\n');
        return Encoding.UTF8.GetBytes(payload.ToString());
    }

    private static void ValidateText(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength ||
            value.Any(char.IsControl))
            throw new ArgumentException($"Receipt field must contain 1 to {maximumLength} non-control characters.",
                parameterName);
    }
}

/// <summary>
/// Host-configured trust boundary. Implementations must recognize only configured trusted
/// issuers and verify the signature over the exact canonical payload supplied by Foundry.
/// </summary>
public interface ICodeRedRecoveryReceiptVerifier
{
    bool IsTrustedIssuer(string issuer);
    bool VerifySignature(string issuer, ReadOnlyMemory<byte> signingPayload, string signature);
}
