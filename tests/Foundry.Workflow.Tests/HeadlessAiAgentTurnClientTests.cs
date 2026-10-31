using System.Net;
using System.Text;
using TheSingularityWorkshop.HeadlessAi;
using Xunit;

namespace TheSingularityWorkshop.Foundry.Tests;

public sealed class HeadlessAiAgentTurnClientTests
{
    [Fact]
    public async Task Prepared_snapshot_is_invoked_once_and_normalized_result_is_bound_to_fingerprint()
    {
        var now = new DateTimeOffset(2026, 10, 31, 16, 0, 0, TimeSpan.Zero);
        var handler = new CapturingHandler();
        using var httpClient = new HttpClient(handler);
        var profile = new HeadlessAiProfile(
            "test-profile",
            new Uri("https://api.openai.com/v1/responses"),
            settings: new Dictionary<string, string> { ["model"] = "test-model" });
        var template = new HeadlessAiAgentTemplate(profile, new OpenAiResponsesAdapter(), new TestHeaderProvider());
        var executor = new HeadlessAiAgentTurnExecutor(template, httpClient, new FixedTimeProvider(now));
        var client = new HeadlessAiAgentTurnClient(executor);
        var request = CreateRequest(now);

        var prepared = client.Prepare(request, now);
        var result = await client.ExecuteAsync(request, prepared);

        Assert.Equal(prepared.RequestFingerprint, result.RequestFingerprint);
        Assert.Equal(request.TaskId, result.TaskId);
        Assert.Equal(request.CandidateCommitSha, result.CandidateCommitSha);
        Assert.Equal("openai-responses", result.ProviderId);
        Assert.Equal("test-model", result.ModelId);
        Assert.Equal("The build failure is caused by the missing registration.", result.Diagnosis);
        Assert.Equal("Investigating the failing registration path.", result.CurrentAction);
        Assert.Equal("needs-review", result.OperationalStatus);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal("host-managed-test-secret", handler.AuthorizationParameter);
        Assert.DoesNotContain("host-managed-test-secret", handler.RequestBody);
    }

    [Fact]
    public async Task Prepared_snapshot_cannot_be_reused_with_a_different_request_instance()
    {
        var now = new DateTimeOffset(2026, 10, 31, 16, 0, 0, TimeSpan.Zero);
        var handler = new CapturingHandler();
        using var httpClient = new HttpClient(handler);
        var profile = new HeadlessAiProfile(
            "test-profile",
            new Uri("https://api.openai.com/v1/responses"),
            settings: new Dictionary<string, string> { ["model"] = "test-model" });
        var template = new HeadlessAiAgentTemplate(profile, new OpenAiResponsesAdapter());
        var executor = new HeadlessAiAgentTurnExecutor(template, httpClient, new FixedTimeProvider(now));
        var client = new HeadlessAiAgentTurnClient(executor);
        var original = CreateRequest(now);
        var prepared = client.Prepare(original, now);
        var differentInstance = original with { CanonicalPrompt = "A different prompt." };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.ExecuteAsync(differentInstance, prepared));

        Assert.Equal(0, handler.CallCount);
    }

    private static FoundryHeadlessAiTurnRequest CreateRequest(DateTimeOffset now)
        => new()
        {
            TaskId = "task-123",
            CorrelationId = "correlation-123",
            Repository = "TrentBest/Example",
            AuthorizedBranch = "work/host-adapter-smoke",
            BaseCommitSha = new string('a', 40),
            CandidateCommitSha = new string('b', 40),
            IssueUrl = "https://github.com/TrentBest/Example/issues/7",
            IssueNumber = 7,
            Role = "implementation-agent",
            Instructions = "Inspect the supplied CI result and propose a bounded correction.",
            CanonicalPrompt = "Explain the failure and propose the next safe step.",
            ResponseSchema = "{ diagnosis, patchIntent, operationalStatus }",
            ExpiresAt = now.AddMinutes(5),
            IdempotencyKey = "foundry-host-adapter-test-123"
        };

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class TestHeaderProvider : IHeadlessAiRequestHeaderProvider
    {
        public ValueTask<IReadOnlyDictionary<string, string>> GetHeadersAsync(
            HeadlessAiProfile profile,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string> { ["Authorization"] = "Bearer host-managed-test-secret" });
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public string? AuthorizationParameter { get; private set; }
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            const string responseJson = """
                {
                  "id": "resp_test_123",
                  "model": "test-model",
                  "status": "completed",
                  "output": [
                    {
                      "type": "message",
                      "content": [
                        {
                          "type": "output_text",
                          "text": "{\"diagnosis\":\"The build failure is caused by the missing registration.\",\"patchIntent\":\"Add the missing registration with a focused test.\",\"operationalStatus\":\"needs-review\",\"currentAction\":\"Investigating the failing registration path.\"}"
                        }
                      ]
                    }
                  ]
                }
                """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        }
    }
}
