using System.Text.Json;
using TheSingularityWorkshop.Foundry;
using Xunit;

namespace Foundry.Workflow.Tests;

public sealed class WorkflowExecutionContextBoundaryTests
{
    [Fact]
    public void EntryLimitAppliesAcrossInputsAndOutputs()
    {
        using var document = JsonDocument.Parse("0");
        var inputs = Enumerable.Range(0, WorkflowExecutionContext.MaximumEntries - 1)
            .ToDictionary(index => $"input-{index}", _ => document.RootElement);
        var context = new WorkflowExecutionContext(inputs);

        context.SetOutput("last-entry", true);

        Assert.Throws<InvalidOperationException>(() =>
            context.SetOutput("over-limit", true));
    }
}
