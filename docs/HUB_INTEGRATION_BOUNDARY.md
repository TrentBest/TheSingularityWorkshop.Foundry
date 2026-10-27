<!-- The Singularity Workshop moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# Foundry and WebPage Hub Integration Boundary

**Status:** investigation complete; no adapter is justified by the current contracts alone.

## Decision

Foundry remains the authority for explicit workflow execution and structured workflow outcomes. WebPage's Hub remains the current reference implementation for composition, process-group lifecycle, and legacy FSM-driven updates. These systems may cooperate through a host adapter, but their current types are not interchangeable and no direct repository dependency should be introduced merely to make the names line up.

## What the WebPage contracts currently mean

The `TrentBest/WebPage` `master` snapshot inspected for this boundary exposes these contracts in `SingularityHub.Abstractions/HubContracts.cs`:

- `WorkDescriptor(ProcessGroupId, WorkId, PathIndex)` describes an identified unit of work associated with a process group and path position.
- `CompletionToken(Value, Completed)` is a compact result that can represent immediate completion or incomplete work.
- `IExecutionProvider.Execute(WorkDescriptor)` is a synchronous host execution boundary returning that token.
- `IProcessGroupHost` tracks registration, activation, completion, and active-group snapshots.
- `ISingularityHub` combines arbitration, warehouse liaison, and process-group hosting. It does not expose the execution provider as a Hub method.

The implementation in `SingularityHub/SingularityHub.cs` confirms the legacy scheduling behavior is name-based: callers register named groups, then `Update()`, `UpdateNestedProcessGroups(...)`, or `UpdateProcessGroup(...)` invokes the supplied update callback. That is not the same operation as executing a data-only Foundry workflow.

## What the current tests prove

The WebPage test surface includes:

- `SingularityHub.Tests/ExecutionTests.cs`: verifies provider contract shape, explicit work identity, token return, input immutability, and incomplete-result reporting.
- `SingularityHub.Tests/HubTests.cs`: verifies Hub coordination contracts, process-group-related state, and that the Hub does not expose an execution-provider method.
- `SingularityHub.Tests/ContractSurfaceTests.cs`: verifies expected Hub contract types remain present.
- `SingularityHub.Tests/WorkshopCompositionTests.cs`: documents composition-level use of ProtocolAi/GrammarAi through FSM_COS; that is composition evidence, not a Foundry workflow integration test.
- Existing application services such as `LivingGuiFsm` and `PageFSM` register and update named process groups around FSM_API behavior. Those calls are legacy state/update scheduling, not Foundry workflow submissions.

These tests establish the current contract shape. They do not establish durable scheduling, async completion polling, retry semantics, cancellation propagation, workflow-to-process-group mapping, or an integration test between Foundry and WebPage.

## Foundry's current contract

Foundry accepts a `WorkflowDefinition` containing sequential steps addressed by host-registered handler keys. `WorkflowRunner` validates the whole definition before handler side effects, runs steps sequentially, and returns a `WorkflowResult`. `WorkflowExecutionCoordinator` adds in-memory acceptance, one-time start, lifecycle snapshots, cooperative cancellation, and disposal around that runner.

The coordinator is deliberately not a durable queue, a process-group scheduler, or an FSM_COS `RuntimeAssembly`. Its lifecycle snapshots are presentation-neutral and can be adapted by a host, but the existing contracts do not say that each `WorkDescriptor` should become a workflow, a workflow step, or a completion token.

## Smallest safe integration seam

Do not add a direct WebPage project reference to Foundry, and do not translate `WorkDescriptor` into `WorkflowDefinition` by guesswork.

When an actual Hub use case requires Foundry, introduce the adapter at the **host boundary**:

1. The host decides that a user or system intent represents a Foundry workflow, rather than a legacy FSM tick or process-group update.
2. The host builds a validated `WorkflowDefinition` using explicit, allowlisted handler keys.
3. The host submits and starts the run through `WorkflowExecutionCoordinator`.
4. The host observes `WorkflowRunSnapshot` and maps only the lifecycle information its caller needs into its own presentation or command response.
5. Cancellation is forwarded only when the host has an explicit run-ID association and a defined caller cancellation policy.

A future adapter contract should state how a caller identifies a workflow, how it observes completion (polling, task await, or transport), how cancellation is correlated, and what happens when the caller disappears. It must not silently reinterpret `CompletionToken.Value` as a Foundry run ID or assume that `Completed = false` means an asynchronous Foundry run exists.

## Proof required before implementation

The next integration slice should be driven by one concrete Hub caller and an executable test that demonstrates all of the following:

- a clear intent that is workflow-shaped rather than an FSM tick;
- explicit mapping to a registered Foundry handler;
- a structured outcome returned to the caller;
- no duplicate execution when the caller retries or re-enters;
- a documented cancellation/abandonment behavior;
- no new coupling from Foundry into Blazor, UI, WebPage, or FSM_COS.

Until such a test exists, the correct implementation is to keep the contracts separate and avoid speculative adapter code.

---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop avatar" width="200" /></p>
