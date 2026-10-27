<!-- The Singularity Workshop moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# Foundry Domain and Ownership

**Status: implemented first runtime slice; its latest CI verification remains pending.** This page describes Foundry's domain and distinguishes implemented behavior from future capabilities.

## The problem Foundry owns

A workflow has identifiable steps, ordering, inputs, outcomes, failure paths, and evidence explaining what happened. When that meaning is buried in CI YAML or host-specific code, it is harder to validate before execution, reuse across hosts, test independently, and explain after failure.

**Foundry owns the definition, validation, coordinated execution, and reporting of explicit workflows.** A CI provider or application is an execution host, not the owner of the workflow's domain semantics.

## Core invariant

> **A workflow definition describes work; Foundry validates and coordinates its execution; the host supplies capabilities and decides what to do with the result.**

A definition is data, not permission to execute arbitrary code. Its meaning should be inspectable before execution, and a run should return structured outcomes rather than relying only on console text or a host-specific exit code.

## Implemented domain slice

The current runtime provides a version-1, in-memory workflow definition with ordered steps and handler keys; validation before any handler is invoked; a host-owned handler registry; sequential execution; and structured step/workflow outcomes. The focused test suite covers declared execution order, rejection of invalid definitions before side effects, handled failure with later steps skipped, cancellation, pre-cancelled runs, and duplicate handler registration.

The implementation is deliberately narrower than a general workflow platform. The contract is typed and in-memory; a persisted JSON/YAML format has not been selected. The current CI workflow includes the runtime tests, but the repository documentation records verification as pending until a run demonstrates that these tests pass.

## Domain concepts and current limits

| Concept | Meaning | Current status |
|---|---|---|
| Workflow definition | Named, versioned description of intended work | Implemented as an in-memory version-1 contract |
| Step | Identified unit of work with a handler key | Implemented for sequential execution |
| Capability | Host-registered operation a step may invoke | Implemented through an explicit handler registry |
| Validation | Check the complete definition before invoking handlers | Implemented |
| Step outcome | Recorded result for one step | Implemented, including skipped work after failure/cancellation |
| Workflow outcome | Aggregate terminal result | Implemented for the current sequential runtime |
| Run identity and durable history | Correlate and retain execution attempts | Not implemented as durable history |
| Dependency graph / parallel execution | Execute work based on graph dependencies | Not implemented |
| Retry, compensation, recovery | Policy for repeating or undoing side effects | Not implemented |
| Persisted manifest | Stable external representation and compatibility rules | Not selected or implemented |
| Timeout enforcement | Bound execution time across handlers | Not implemented by the core runtime |
| Evidence provenance and retention | Durable, queryable evidence model | Only the existing CI proof artifacts are demonstrated |

## What Foundry owns

- The workflow-definition contract and validation rules.
- Execution ordering and the semantics of success, failure, cancellation, and skipped steps within the supported runtime.
- Coordination between a definition and explicitly registered capabilities.
- Structured run outcomes that hosts can interpret.
- Host-neutral behavior testable without a desktop UI or live CI provider.

## What Foundry does not own

- **Finite-state-machine primitives.** FSM_API owns those mechanics where a component directly uses that package.
- **MicroBundle composition.** FSM_COS owns its composition boundary. A Foundry workflow is not an FSM_COS RuntimeManifest, and RuntimeAssembly is not a workflow result.
- **Universal serialization.** FSM_Serialization is not presumed to provide a universal workflow schema.
- **The host environment.** GitHub Actions, AnyApp, WebApp, or another host supplies triggers, permissions, process lifecycle, and external capabilities.
- **Arbitrary script evaluation.** Definitions cannot grant themselves capabilities; any future executable capability needs an explicit contract, permission boundary, and tests.
- **Publication or deployment approval.** A successful run must not publish a NuGet package, deploy an application, or publish an article automatically.
- **Presentation.** Trace files, SVGs, GUI views, and article renderings are views of evidence, not the source of truth.

## Relationship to neighboring Workshop technologies

A host or CI provider supplies triggers, permissions, environment, and registered capabilities. Foundry defines, validates, coordinates, and reports a workflow. Explicit adapters may use FSM_API for state mechanics or other host-provided capabilities, but integration claims require a concrete test against the actual public contract.

FSM_COS remains a separate composition boundary when a workflow genuinely needs MicroBundle composition; it is not assumed to be the workflow executor. Foundry's current sequential runtime does not depend on FSM_COS, and no cross-repository integration should be added without a demonstrated requirement.

## Safety and correctness invariants

- Invalid definitions are rejected before any handler is invoked.
- Every step handler must be registered by the host.
- Definitions cannot grant themselves environmental permissions.
- Failure and cancellation are distinct; later work is skipped after either terminates the sequential run.
- The runtime does not report success when required work fails or does not run.
- Failure reporting avoids exposing exception messages that might contain secrets.
- Determinism claims are limited to artifacts and values actually compared in tests.
- External publication and deployment remain disabled unless explicitly authorized.

These invariants are scoped to the current implementation and its tests. Timeout enforcement, durable run isolation, retries, compensation, and cross-process orchestration remain outside the implemented contract.

## Next verification and theory work

The immediate gate is to obtain a current CI result for the workflow runtime tests and resolve any failures before raising the readiness estimate. After that, broaden adversarial tests and align the README, architecture, roadmap, and coverage ledger to the same evidence.

Theory documentation should grow alongside tested contracts, not ahead of them. The next useful topics are execution lifecycle and isolation, capability security, failure/cancellation semantics, evidence provenance, and a persisted representation only when a concrete interchange use case justifies it. Each page should explain what the concept is, why it exists, how it works, what it cannot guarantee, and which source or tests verify it.

## Related documents

- [README](../README.md) — product orientation and current proof.
- [Architecture](ARCHITECTURE.md) — subsystem boundaries and integration choices.
- [Contract inspection ledger](CONTRACT_INSPECTION.md) — source-backed findings and open questions.
- [Roadmap and readiness](ROADMAP.md) — staged implementation and evidence gates.
- [UML policy and inventory](UML/README.md) — diagram policy and current evidence status.

---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop avatar" width="200" /></p>
