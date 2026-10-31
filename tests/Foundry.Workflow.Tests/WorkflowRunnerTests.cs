using TheSingularityWorkshop.Foundry;
using Xunit;

namespace Foundry.Workflow.Tests;

public sealed class WorkflowRunnerTests
{
    [Fact]
    public async Task ExecutesValidStepsInDeclaredOrder()
    {
        var order = new List<string>();
        var registry = new WorkflowHandlerRegistry();
        registry.Register("first", _ => { order.Add("first"); return ValueTask.CompletedTask; });
        registry.Register("second", _ => { order.Add("second"); return ValueTask.CompletedTask; });

        var result = await new WorkflowRunner(registry).RunAsync(Definition(
            new WorkflowStep("step-1", "first"), new WorkflowStep("step-2", "second")));

        Assert.Equal(WorkflowStatus.Succeeded, result.Status);
        Assert.Equal(new[] { "first", "second" }, order);
        Assert.All(result.Steps, step => Assert.Equal(StepStatus.Succeeded, step.Status));
    }

    [Fact]
    public async Task RejectsInvalidDefinitionBeforeAnyHandlerRuns()
    {
        var calls = 0;
        var registry = new WorkflowHandlerRegistry();
        registry.Register("known", _ => { calls++; return ValueTask.CompletedTask; });
        var invalid = Definition(new("duplicate", "known"), new("duplicate", "missing"));

        var result = await new WorkflowRunner(registry).RunAsync(invalid);

        Assert.Equal(WorkflowStatus.Invalid, result.Status);
        Assert.Contains(result.Diagnostics, item => item.Code == "WF007");
        Assert.Contains(result.Diagnostics, item => item.Code == "WF009");
        Assert.Equal(0, calls);
        Assert.Empty(result.Steps);
    }

    [Fact]
    public async Task FailureIsRecordedAndRemainingStepsAreSkipped()
    {
        var calls = 0;
        var registry = new WorkflowHandlerRegistry();
        registry.Register("fail", _ => ValueTask.FromException(new InvalidOperationException("expected")));
        registry.Register("later", _ => { calls++; return ValueTask.CompletedTask; });

        var result = await new WorkflowRunner(registry).RunAsync(Definition(
            new WorkflowStep("step-1", "fail"), new WorkflowStep("step-2", "later")));

        Assert.Equal(WorkflowStatus.Failed, result.Status);
        Assert.Equal(StepStatus.Failed, result.Steps[0].Status);
        Assert.Contains("InvalidOperationException", result.Steps[0].Error!);
        Assert.Equal(StepStatus.Skipped, result.Steps[1].Status);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CancellationIsDistinctAndSkipsRemainingSteps()
    {
        using var source = new CancellationTokenSource();
        var registry = new WorkflowHandlerRegistry();
        registry.Register("cancel", token =>
        {
            source.Cancel();
            return ValueTask.FromCanceled(token.IsCancellationRequested ? token : new CancellationToken(true));
        });
        registry.Register("later", _ => ValueTask.CompletedTask);

        var result = await new WorkflowRunner(registry).RunAsync(Definition(
            new WorkflowStep("step-1", "cancel"), new WorkflowStep("step-2", "later")), source.Token);

        Assert.Equal(WorkflowStatus.Cancelled, result.Status);
        Assert.Equal(StepStatus.Cancelled, result.Steps[0].Status);
        Assert.Equal(StepStatus.Skipped, result.Steps[1].Status);
    }

    [Fact]
    public async Task PreCancelledRunDoesNotInvokeHandlers()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var calls = 0;
        var registry = new WorkflowHandlerRegistry();
        registry.Register("known", _ => { calls++; return ValueTask.CompletedTask; });

        var result = await new WorkflowRunner(registry).RunAsync(Definition(
            new WorkflowStep("step-1", "known")), source.Token);

        Assert.Equal(WorkflowStatus.Cancelled, result.Status);
        Assert.Equal(0, calls);
        Assert.Equal(StepStatus.Cancelled, result.Steps[0].Status);
    }

    [Fact]
    public async Task HandlerRegistrationRejectsDuplicateKeys()
    {
        var registry = new WorkflowHandlerRegistry();
        registry.Register("same", _ => ValueTask.CompletedTask);
        Assert.Throws<InvalidOperationException>(() =>
            registry.Register("same", _ => ValueTask.CompletedTask));
    }


    [Fact]
    public async Task LegacyRunAsyncDefaultCancellationArgumentRemainsUnambiguous()
    {
        var registry = new WorkflowHandlerRegistry();
        registry.Register("work", _ => ValueTask.CompletedTask);

        var result = await new WorkflowRunner(registry).RunAsync(
            Definition(new WorkflowStep("step-1", "work")), default);

        Assert.Equal(WorkflowStatus.Succeeded, result.Status);
    }

    [Fact]
    public async Task ContextAwareHandlersPassBoundedTypedDataBetweenSteps()
    {
        using var inputDocument = System.Text.Json.JsonDocument.Parse("\"task-42\"");
        var context = new WorkflowExecutionContext(
            new Dictionary<string, System.Text.Json.JsonElement>
            {
                ["taskId"] = inputDocument.RootElement
            });
        var observed = string.Empty;
        var registry = new WorkflowHandlerRegistry();
        registry.Register("validate", (execution, _) =>
        {
            var taskId = execution.GetInput<string>("taskId");
            execution.SetOutput("validatedTaskId", taskId);
            return ValueTask.CompletedTask;
        });
        registry.Register("consume", (execution, _) =>
        {
            observed = execution.GetOutput<string>("validatedTaskId")!;
            return ValueTask.CompletedTask;
        });

        var result = await new WorkflowRunner(registry).RunWithContextAsync(Definition(
            new WorkflowStep("step-1", "validate"),
            new WorkflowStep("step-2", "consume")), context);

        Assert.Equal(WorkflowStatus.Succeeded, result.Status);
        Assert.Equal("task-42", observed);
    }

    [Fact]
    public void ContextRejectsOversizedAndDuplicateOutputs()
    {
        var context = new WorkflowExecutionContext();
        context.SetOutput("result", "first");
        Assert.Throws<InvalidOperationException>(() => context.SetOutput("result", "second"));
        Assert.Throws<ArgumentException>(() =>
            new WorkflowExecutionContext(new Dictionary<string, System.Text.Json.JsonElement>
            {
                ["oversized"] = System.Text.Json.JsonDocument.Parse(
                    "\"" + new string('x', WorkflowExecutionContext.MaximumValueBytes) + "\"")
                    .RootElement
            }));
    }

    private static WorkflowDefinition Definition(params WorkflowStep[] steps) =>
        new("test.workflow.v1", "Test workflow", 1, steps);
}
