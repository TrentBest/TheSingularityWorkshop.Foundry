using System.Text;
using TheSingularityWorkshop.Foundry;
using Xunit;

namespace Foundry.Workflow.Tests;

public sealed class CiAiDispatchRunMarkerTests
{
    private const string ValidMarker = """
        {
          "contract": "foundry.ciai-dispatch-marker.v1",
          "requestId": "request-1",
          "operationId": "operation-1",
          "idempotencyKey": "idempotency-1",
          "runId": 12345,
          "runAttempt": 2
        }
        """;

    private static CiAiDispatchRunBinding Binding(long runId = 12345, int attempt = 2) =>
        new(runId, attempt, "TrentBest/TheSingularityWorkshop.Foundry",
            ".github/workflows/build.yml", "refs/heads/development", new string('b', 40),
            "refs/heads/work/task-1", new string('a', 40), "workflow_dispatch",
            new DateTimeOffset(2026, 10, 31, 0, 1, 0, TimeSpan.Zero));

    [Fact]
    public void ParsesExactMarkerAndBindsOnlyToMatchingObservedRunAttempt()
    {
        var marker = CiAiDispatchRunMarker.Parse(Encoding.UTF8.GetBytes(ValidMarker));

        Assert.Equal("request-1", marker.RequestId);
        Assert.Equal("operation-1", marker.OperationId);
        Assert.Equal("idempotency-1", marker.IdempotencyKey);
        Assert.Equal(12345, marker.RunId);
        Assert.Equal(2, marker.RunAttempt);

        var observation = marker.Bind(Binding());
        Assert.Equal("request-1", observation.Marker.RequestId);
        Assert.Equal("operation-1", observation.Marker.OperationId);
        Assert.Equal("idempotency-1", observation.Marker.IdempotencyKey);
        Assert.Equal(12345, observation.Binding.RunId);
        Assert.Equal(2, observation.Binding.RunAttempt);
    }

    [Theory]
    [InlineData(12346, 2)]
    [InlineData(12345, 1)]
    public void RejectsMarkerFromAnotherRunOrAttempt(long runId, int attempt)
    {
        var marker = CiAiDispatchRunMarker.Parse(Encoding.UTF8.GetBytes(ValidMarker));

        Assert.Throws<InvalidOperationException>(() => marker.Bind(Binding(runId, attempt)));
    }

    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("{\"contract\":\"wrong\",\"requestId\":\"r\",\"operationId\":\"o\",\"idempotencyKey\":\"k\",\"runId\":1,\"runAttempt\":1}")]
    [InlineData("{\"contract\":\"foundry.ciai-dispatch-marker.v1\",\"requestId\":\"r\",\"requestId\":\"r2\",\"operationId\":\"o\",\"idempotencyKey\":\"k\",\"runId\":1,\"runAttempt\":1}")]
    [InlineData("{\"contract\":\"foundry.ciai-dispatch-marker.v1\",\"requestId\":\"r\",\"operationId\":\"o\",\"idempotencyKey\":\"k\",\"runId\":0,\"runAttempt\":1}")]
    [InlineData("{\"contract\":\"foundry.ciai-dispatch-marker.v1\",\"requestId\":\"r\",\"operationId\":\"o\",\"idempotencyKey\":\"k\",\"runId\":1,\"runAttempt\":0}")]
    [InlineData("{\"contract\":\"foundry.ciai-dispatch-marker.v1\",\"requestId\":\"r\",\"operationId\":\"o\",\"idempotencyKey\":\"k\",\"runId\":1,\"runAttempt\":1,\"unexpected\":true}")]
    public void RejectsMalformedDuplicateUnknownOrUnsupportedMarker(string payload)
    {
        Assert.Throws<FormatException>(() => CiAiDispatchRunMarker.Parse(Encoding.UTF8.GetBytes(payload)));
    }

    [Fact]
    public void RejectsMissingOrUnboundedIdentity()
    {
        var missing = ValidMarker.Replace("\"operationId\": \"operation-1\",", string.Empty, StringComparison.Ordinal);
        var oversized = ValidMarker.Replace("request-1", new string('r', 257), StringComparison.Ordinal);

        Assert.Throws<FormatException>(() => CiAiDispatchRunMarker.Parse(Encoding.UTF8.GetBytes(missing)));
        Assert.Throws<FormatException>(() => CiAiDispatchRunMarker.Parse(Encoding.UTF8.GetBytes(oversized)));
    }

    [Fact]
    public void RejectsPayloadAboveFourKilobytes()
    {
        var payload = Encoding.UTF8.GetBytes(ValidMarker.PadRight(CiAiDispatchRunMarker.MaximumPayloadBytes + 1, ' '));

        Assert.Throws<FormatException>(() => CiAiDispatchRunMarker.Parse(payload));
    }
}
