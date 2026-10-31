using System.Text.Json;

namespace TheSingularityWorkshop.Foundry;

/// <summary>
/// Request identity emitted by the registered trusted workflow into a run-scoped
/// artifact. Parsing validates the wire shape; the host must still retrieve it
/// from the exact GitHub run/attempt after independently validating run metadata.
/// </summary>
public sealed class CiAiDispatchRunMarker
{
    private CiAiDispatchRunMarker(string requestId, string operationId, string idempotencyKey, long runId, int runAttempt)
    {
        RequestId = requestId;
        OperationId = operationId;
        IdempotencyKey = idempotencyKey;
        RunId = runId;
        RunAttempt = runAttempt;
    }

    public const string Contract = "foundry.ciai-dispatch-marker.v1";
    public const int MaximumPayloadBytes = 4 * 1024;

    public string RequestId { get; }
    public string OperationId { get; }
    public string IdempotencyKey { get; }
    public long RunId { get; }
    public int RunAttempt { get; }

    private static readonly HashSet<string> PropertyNames = new(StringComparer.Ordinal)
    {
        "contract", "requestId", "operationId", "idempotencyKey", "runId", "runAttempt"
    };

    /// <summary>
    /// Parses the exact UTF-8 marker payload. Unknown/duplicate fields, oversized
    /// content, invalid identities, and unsupported contract versions fail closed.
    /// </summary>
    public static CiAiDispatchRunMarker Parse(ReadOnlyMemory<byte> utf8Json)
    {
        if (utf8Json.Length is 0 or > MaximumPayloadBytes)
            throw new FormatException("Dispatch marker payload is empty or exceeds the 4 KiB limit.");

        try
        {
            using var document = JsonDocument.Parse(utf8Json, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 4
            });

            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new FormatException("Dispatch marker must be a JSON object.");

            var values = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var property in root.EnumerateObject())
            {
                if (!PropertyNames.Contains(property.Name) || !values.TryAdd(property.Name, property.Value))
                    throw new FormatException("Dispatch marker contains an unknown or duplicate property.");
            }

            if (values.Count != PropertyNames.Count ||
                !values.TryGetValue("contract", out var contract) ||
                contract.ValueKind != JsonValueKind.String ||
                contract.GetString() != Contract)
                throw new FormatException("Dispatch marker contract or required properties are invalid.");

            var requestId = ReadIdentity(values, "requestId");
            var operationId = ReadIdentity(values, "operationId");
            var idempotencyKey = ReadIdentity(values, "idempotencyKey");
            if (!values["runId"].TryGetInt64(out var runId) || runId <= 0 ||
                !values["runAttempt"].TryGetInt32(out var runAttempt) || runAttempt <= 0)
                throw new FormatException("Dispatch marker run identity must contain positive integer values.");

            return new CiAiDispatchRunMarker(requestId, operationId, idempotencyKey, runId, runAttempt);
        }
        catch (JsonException exception)
        {
            throw new FormatException("Dispatch marker is not valid bounded UTF-8 JSON.", exception);
        }
    }

    /// <summary>
    /// Combines the marker with run metadata independently obtained from GitHub.
    /// The caller is responsible for proving that these bytes came from this exact
    /// run/attempt's artifact or trusted job evidence.
    /// </summary>
    public CiAiDispatchRunObservation Bind(CiAiDispatchRunBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (binding.RunId != RunId || binding.RunAttempt != RunAttempt)
            throw new InvalidOperationException("Dispatch marker run identity does not match the independently observed GitHub run.");
        return new CiAiDispatchRunObservation(binding, this);
    }

    private static string ReadIdentity(IReadOnlyDictionary<string, JsonElement> values, string name)
    {
        var element = values[name];
        if (element.ValueKind != JsonValueKind.String)
            throw new FormatException($"Dispatch marker '{name}' must be a string.");
        var value = element.GetString();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 256 || value.Any(char.IsControl))
            throw new FormatException($"Dispatch marker '{name}' is empty or outside its allowed bounds.");
        return value;
    }
}
