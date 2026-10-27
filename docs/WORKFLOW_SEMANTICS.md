<!-- The Singularity Workshop moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# Workflow Semantics — Version 1

**Status:** implementation-backed theory for the first Foundry runtime slice. This page describes the sequential, in-memory contract currently being implemented; it does not describe a persisted manifest, distributed execution, or a general automation engine.

## The execution model

A workflow definition is data. It identifies the workflow, declares schema version 1, and lists ordered steps. Each step names a handler key; it does not contain code. The host registers the operations it is willing to expose through a WorkflowHandlerRegistry.

```mermaid
flowchart TD
    A[Workflow definition] --> B[Validate whole definition]
    B -->|Invalid| X[Invalid result; no handlers invoked]
    B -->|Valid| C[Execute steps in declared order]
    C --> D{Step outcome}
    D -->|Succeeded| E{More steps?}
    E -->|Yes| C
    E -->|No| S[Workflow succeeded]
    D -->|Failed| F[Record failure]
    F --> K[Skip remaining steps]
    K --> R[Workflow failed]
    D -->|Cancelled| L[Record cancellation]
    L --> M[Skip remaining steps]
    M --> N[Workflow cancelled]
```

## Contract rules

1. **Validate before side effects.** Foundry checks required workflow identity/name, supported schema version, at least one step, unique step IDs, non-empty handler keys, and registration of every referenced handler before execution starts.
2. **The host owns capabilities.** A definition can name only a handler that the host registered. Registering a handler is an explicit host decision; the workflow does not grant itself filesystem, network, process, or publishing permissions.
3. **Order is semantic.** Version 1 executes steps sequentially in the declared order. It does not infer dependencies or run steps in parallel.
4. **Failure stops the sequence.** The failing step is recorded as failed and later steps are recorded as skipped. Foundry does not claim to roll back effects already performed by a handler.
5. **Cancellation is not failure.** A cancellation requested before or during a handler produces a cancelled outcome; remaining steps are skipped.
6. **Invalid is not a run failure.** An invalid definition is rejected before any handler invocation and returns validation diagnostics without step results.
7. **Success is explicit.** The workflow result is successful only when every step succeeds.
8. **Errors are conservative by default.** The runtime records the exception type, not the exception message, because handler messages can accidentally contain secrets. A host may separately provide sanitized diagnostics under a deliberate policy.

## Outcome vocabulary

| Workflow status | Meaning |
|---|---|
| Succeeded | All declared steps completed successfully. |
| Failed | A handler failed; later steps were skipped. |
| Cancelled | Cancellation was requested; the current/remaining execution did not complete. |
| Invalid | Definition validation failed before execution. |

| Step status | Meaning |
|---|---|
| Succeeded | The registered handler returned normally. |
| Failed | The handler threw an exception not classified as requested cancellation. |
| Cancelled | The cancellation token was requested while the step was being considered/executed. |
| Skipped | The step was not invoked because an earlier step failed or execution was cancelled. |

## Deliberate limitations

- The definition is currently a typed in-memory object; there is no JSON/YAML or other persisted schema.
- The runtime has no retry, compensation, recovery, parallelism, timeout enforcement, durable run history, or cross-process orchestration.
- A handler may perform external side effects. Validation prevents invocation for an invalid workflow but cannot make successful prior side effects transactional.
- Cancellation is cooperative: a handler must observe and honor its cancellation token. Foundry cannot forcibly stop a handler that ignores it.
- Determinism is limited to the runner’s declared order and outcome mapping; handler behavior and external systems may be nondeterministic.

## Source and verification

- [Definition contract](../src/Foundry.Workflow/WorkflowDefinition.cs)
- [Validation rules](../src/Foundry.Workflow/WorkflowValidator.cs)
- [Host capability registry](../src/Foundry.Workflow/WorkflowHandlerRegistry.cs)
- [Runner and outcome mapping](../src/Foundry.Workflow/WorkflowRunner.cs)
- [Focused tests](../tests/Foundry.Workflow.Tests/WorkflowRunnerTests.cs)

CI verification of the first runtime slice is tracked by the [Foundry workflow](../.github/workflows/foundry-fsm-api-probe.yml). This document must be revised if the contract or tests change.

---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop avatar" width="200" /></p>
