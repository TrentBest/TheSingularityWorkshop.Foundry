<!-- The Singularity Workshop moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# TheSingularityWorkshop.Foundry

[![CI](https://github.com/TrentBest/TheSingularityWorkshop.Foundry/actions/workflows/foundry-fsm-api-probe.yml/badge.svg?branch=development)](https://github.com/TrentBest/TheSingularityWorkshop.Foundry/actions/workflows/foundry-fsm-api-probe.yml)
[![License: MIT](https://img.shields.io/github/license/TrentBest/TheSingularityWorkshop.Foundry)](LICENSE)

**Foundry is the host-neutral execution pipeline for The Singularity Workshop Hub.** It accepts explicit work, validates it before side effects, coordinates execution, and reports authoritative lifecycle state and structured outcomes. GUI may present that state when the Hub chooses to appear; GUI is not the executor.

> **Implementation status:** the sequential workflow runner, preflight validation, structured outcomes, and an in-process retained lifecycle coordinator are implemented on \`development\`. The retained lifecycle slice is verified by [development CI run #84](https://github.com/TrentBest/TheSingularityWorkshop.Foundry/actions/runs/40546957980). No package publication or deployment is part of this work.

## 01 — Why Foundry exists

Workflow meaning should not be buried in a desktop window, browser component, or CI YAML. Foundry provides a small, inspectable execution boundary that hosts can use without depending on GUI or a particular platform. Workflow definitions are data; hosts explicitly register the capabilities that definitions may invoke.

## 02 — Ownership boundaries

- **Foundry executes:** validates workflows; accepts and starts runs; coordinates lifecycle and cancellation; exposes status snapshots and structured outcomes.
- **The / Architect owns the Hub experience:** decides what work to request and when the Hub should present itself.
- **GUI presents:** renders the Hub workbench and maps explicit user intents to supported commands. It does not own execution state or the authoritative loop.
- **FSM_COS composes:** resolves MicroBundles and returns a \`RuntimeAssembly\`. Composition is not workflow execution.
- **Hosts provide capabilities:** headless applications, CI, WebPage, and desktop hosts adapt their environment and register permitted handlers.
- **FSM_API supplies FSM mechanics** where a concrete integration consumes it. Foundry does not reimplement FSM_API.

Foundry deliberately does not force workflow steps, WebPage process-group ticks, and FSM_COS \`RuntimeAssembly\` into one abstraction. They remain distinct until a source-backed, tested integration demonstrates the correct seam.

## 03 — See it in a minute

Start with the [version-1 workflow definition](src/Foundry.Workflow/WorkflowDefinition.cs), [sequential runner](src/Foundry.Workflow/WorkflowRunner.cs), and [retained execution coordinator](src/Foundry.Workflow/WorkflowExecutionCoordinator.cs).

A host can:
1. Register allowed operations in \`WorkflowHandlerRegistry\`.
2. Submit a typed workflow definition and receive an \`Accepted\` run snapshot.
3. Start that run once and observe its lifecycle through \`GetSnapshot\`.
4. Request cancellation through \`Cancel\`.
5. Read the terminal \`WorkflowResult\` and per-step outcomes.

The first version executes named handlers sequentially. It validates the complete definition before invoking any handler, stops after failure, distinguishes cancellation from failure, and records remaining steps as skipped. The coordinator retains snapshots only in memory.

The separate [FSM_API probe](src/Foundry.FsmApiProbe/Program.cs) and [fox/sheepdog scenario](src/Foundry.FoxSheepdog/Program.cs) remain deterministic state-execution demonstrations. They exercise real FSM_API behavior and generate reproducible evidence; they are not automatically treated as Hub workflows.

## 04 — Contracts, tests, and boundaries

- [Workflow semantics](docs/WORKFLOW_SEMANTICS.md) — validation, execution, lifecycle, cancellation, and outcome rules.
- [Domain and ownership](docs/DOMAIN.md) — what Foundry owns and what it does not.
- [Architecture](docs/ARCHITECTURE.md) — subsystem boundaries and integration decisions.
- [Contract inspection ledger](docs/CONTRACT_INSPECTION.md) — source-backed findings and unresolved questions.
- [Roadmap and readiness](docs/ROADMAP.md) — milestones and verification gates.
- [Documentation index](DOCUMENTATION_INDEX.md) — documentation entry points.
- [UML documentation](docs/UML/README.md) — source-grounded diagram policy and inventory.

The focused xUnit suite covers ordering, invalid definitions, failure, cancellation, and retained run lifecycle. The [Foundry CI workflow](.github/workflows/foundry-fsm-api-probe.yml) runs those tests alongside the FSM_API and fox/sheepdog evidence checks.

## 05 — Limits and next steps

The current contract is in-memory and sequential. It does not provide durable run history, persisted JSON/YAML manifests, retries, compensation, parallel/distributed execution, enforced handler timeouts, or recovery after process loss. Cancellation is cooperative: handlers must honor their token.

The next gate is a current green CI run for the coordinator and runner tests. After that, trace WebPage's real Hub callers and tests to identify which behaviors are workflows, host-scheduled work, or legacy process-group ticks. Preserve those distinctions, add only a proven adapter, and then stabilize the minimal presentation-facing contract for GUI. Broader host migration, generated UML, and optional article workflows follow evidence rather than precede it.

Publication, release, and deployment remain separate actions and require Trent's explicit approval.

---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop avatar" width="200" /></p>
