using System.Text.Json;
using TheSingularityWorkshop.Foundry;
using Xunit;

namespace Foundry.Workflow.Tests;

public sealed class WorkflowExecutionContextTests
{
    [Fact]
    public void ConstructorClonesInputBeforeCallerDisposesJsonDocument()
    {
        WorkflowExecutionContext context;
        using (var document = JsonDocument.Parse("\"task-42\""))
        {
            context = new WorkflowExecutionContext(
                new Dictionary<string, JsonElement>
                {
                    ["taskId"] = document.RootElement
                });
        }

        Assert.Equal("task-42", context.GetInput<string>("taskId"));
    }

    [Fact]
    public void ConstructorRejectsMoreThanMaximumEntryCount()
    {
        using var document = JsonDocument.Parse("0");
        var inputs = Enumerable.Range(0, WorkflowExecutionContext.MaximumEntries + 1)
            .ToDictionary(index => $"key-{index}", _ => document.RootElement);

        Assert.Throws<ArgumentException>(() => new WorkflowExecutionContext(inputs));
    }

    [Fact]
    public void ConstructorRejectsAggregateInputBytesBeyondBudget()
    {
        using var document = JsonDocument.Parse("\"" + new string('x', 60_000) + "\"");
        var inputs = Enumerable.Range(0, 18)
            .ToDictionary(index => $"input-{index}", _ => document.RootElement);

        Assert.Throws<ArgumentException>(() => new WorkflowExecutionContext(inputs));
    }

    [Fact]
    public void OutputsEnforcePerValueAndAggregateBudgets()
    {
        var context = new WorkflowExecutionContext();

        Assert.Throws<ArgumentException>(() =>
            context.SetOutput("oversized", new string('x', WorkflowExecutionContext.MaximumValueBytes)));

        var payload = new string('x', 60_000);
        for (var index = 0; index < 17; index++)
            context.SetOutput($"output-{index}", payload);

        Assert.Throws<InvalidOperationException>(() =>
            context.SetOutput("aggregate-overflow", new string('y', 30_000)));
    }

    [Fact]
    public void ContextKeysMustBeBoundedAndControlFree()
    {
        var context = new WorkflowExecutionContext();

        Assert.Throws<ArgumentException>(() => context.SetOutput("", true));
        Assert.Throws<ArgumentException>(() => context.SetOutput(new string('k', 129), true));
        Assert.Throws<ArgumentException>(() => context.SetOutput("bad\nkey", true));
    }
}
