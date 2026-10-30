<!-- The Singularity Workshop moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# WorkflowExecutionCoordinator — Source Companion

## Purpose

`WorkflowExecutionCoordinator` retains each accepted workflow run and exposes its lifecycle to a host without replacing the sequential `WorkflowRunner`. It is the first host-neutral seam for the Hub to submit work, start it, observe state, request cancellation, and wait for cleanup.

## Why it exists

The runner is a single awaited invocation. A Hub presentation or headless host needs a run identity and snapshots while a handler is still running. The coordinator owns that lifecycle state; the runner continues to own workflow validation, ordered step invocation, and step/workflow outcomes.

## Public contract

- `Submit(WorkflowDefinition)` accepts work and returns an `Accepted` snapshot without invoking handlers.
- `StartAsync(runId)` starts an accepted run once without a workflow-level deadline.
- `StartAsync(runId, timeout)` applies an optional positive timeout; expiration requests cooperative cancellation and produces `TimedOut`, distinct from caller-requested `Cancelled`. Caller cancellation wins if both signals race.
- `GetSnapshot(runId)` returns a point-in-time immutable snapshot.
- `Cancel(runId)` requests cooperative cancellation, including cancellation before start.
- `DisposeAsync()` cancels outstanding work and waits for its completion.

The coordinator is in-process and in-memory. It does not provide durable run history, a persisted queue, retries, or a cross-process transport. A timeout is cooperative and cannot forcibly stop a handler that ignores its token. Handlers must honor cancellation for timely shutdown.

## Verification

See [WorkflowExecutionCoordinatorTests](../../tests/Foundry.Workflow.Tests/WorkflowExecutionCoordinatorTests.cs) for lifecycle, caller cancellation, timeout classification, invalid timeout values, invalid-workflow, and single-start behavior. The baseline lifecycle tests passed in [development CI run #84](https://github.com/TrentBest/TheSingularityWorkshop.Foundry/actions/runs/40546957980); the timeout-specific tests passed [PR CI run #102](https://github.com/TrentBest/TheSingularityWorkshop.Foundry/actions/runs/41123855392).

## UML

See the [WorkflowExecutionCoordinator neighborhood](../../docs/UML/WorkflowExecutionCoordinatorUML.md).

---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop avatar" width="200" /></p>
