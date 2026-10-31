<!-- The Singularity Workshop moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# Workflow Execution Context

## Purpose

`WorkflowExecutionContext` carries bounded JSON data through one sequential workflow run. It lets registered host handlers exchange typed inputs and outputs without putting application state into workflow definitions or turning workflow data into executable instructions.

The context is a data channel, not an authority channel. A value such as `"delete-branch"` does not authorize that operation. Only a handler explicitly registered by the host can perform an operation, and the handler must apply its own authorization and validation.

## Supply input data

Create the context from JSON values and pass it to the existing runner or coordinator:

```csharp
using System.Text.Json;

using var document = JsonDocument.Parse("\\\"task-42\\\"");
var context = new WorkflowExecutionContext(
    new Dictionary<string, JsonElement>
    {
        ["taskId"] = document.RootElement
    });

var accepted = coordinator.Submit(workflowDefinition, context);
var result = await coordinator.StartAsync(accepted.RunId);
```

The constructor clones each input value. The caller may dispose the source `JsonDocument` after construction; the context retains its own JSON snapshot.

A context-aware handler reads typed input through `GetInput<T>` or `TryGetInput<T>`. Missing required input through `GetInput<T>` throws `KeyNotFoundException`.

## Exchange output between handlers

Register a context-aware handler with the host-owned `WorkflowHandlerRegistry`:

```csharp
registry.Register("validate-task", (context, cancellationToken) =>
{
    var taskId = context.GetInput<string>("taskId");
    context.SetOutput("validatedTaskId", taskId);
    return ValueTask.CompletedTask;
});

registry.Register("consume-task", (context, cancellationToken) =>
{
    var validatedTaskId = context.GetOutput<string>("validatedTaskId");
    // Continue with a host-authorized operation.
    return ValueTask.CompletedTask;
});
```

The same context is passed to each handler in declared workflow order. Outputs are write-once: a key cannot replace an input or an earlier output. Use `GetOutput<T>` or `TryGetOutput<T>` to read values produced by earlier handlers. A missing required output through `GetOutput<T>` throws `KeyNotFoundException`.

The legacy `Register(key, Func<CancellationToken, ValueTask>)` overload remains supported. It adapts the handler to the context-aware registration path and does not require changes to existing handlers.

## Enforced bounds

| Limit | Maximum |
|---|---:|
| Combined input and output entries | 128 |
| One serialized JSON value | 64 KiB |
| Aggregate serialized JSON value bytes | 1 MiB |
| JSON serialization depth | 32 |
| Key length | 128 characters |

Keys must be non-empty, non-whitespace, and control-character-free. Values are serialized as JSON before being stored. Input construction rejects entry-count, per-value, and aggregate-budget violations. Output writes reject duplicate keys and enforce the same entry, per-value, and aggregate budgets.

The byte budgets measure serialized JSON values; they do not represent an exact managed-heap allocation limit. Hosts should still avoid constructing arbitrarily large objects before passing them to `SetOutput<T>`.

## Ownership and safety

- Use a distinct context for each accepted run. The context contains mutable outputs and is intended to be run-scoped.
- Workflow steps remain sequential. The context does not add parallel execution or a synchronization model for parallel handlers.
- Inputs are cloned at construction; outputs are stored as cloned JSON elements. Callers do not retain mutable references to stored JSON.
- Handler registration remains the host's allowlist. Context values do not select arbitrary CLR types, invoke handlers, or grant filesystem, network, process, GitHub, CI, or publishing authority.
- Do not put credentials, access tokens, capability identifiers, or other secrets in workflow context.
- Context is in-memory only. It is not serialized into a durable workflow journal and is not restored after process loss.

## Source and verification

- [Context implementation](../src/Foundry.Workflow/WorkflowExecutionContext.cs)
- [Handler registry](../src/Foundry.Workflow/WorkflowHandlerRegistry.cs)
- [Sequential runner](../src/Foundry.Workflow/WorkflowRunner.cs)
- [Coordinator](../src/Foundry.Workflow/WorkflowExecutionCoordinator.cs)
- [Context tests](../tests/Foundry.Workflow.Tests/WorkflowExecutionContextTests.cs)
- [Runner tests](../tests/Foundry.Workflow.Tests/WorkflowRunnerTests.cs)
- [Coordinator tests](../tests/Foundry.Workflow.Tests/WorkflowExecutionCoordinatorTests.cs)

The context implementation and focused bounds/clone tests are covered by the Foundry PR CI workflow. The context does not change the sequential execution model or the in-memory lifecycle boundary.

---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop" width="200" /></p>
