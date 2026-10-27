<!-- The Singularity Workshop moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# Foundry Domain and Ownership

**Status: domain proposal for the next implementation slice.** This document defines the intended boundary to guide implementation and tests. It does not claim that the workflow contract or runtime already exists.

## The problem Foundry owns

A workflow is more than a list of commands. It has named steps, ordering and state transitions, inputs, outcomes, failure paths, and evidence that lets a person or another system understand what happened. When that meaning is buried inside a CI YAML file or host-specific application code, it becomes difficult to validate before execution, reuse across hosts, test independently, or explain after failure.

**Foundry's domain is the definition, validation, coordinated execution, and reporting of explicit workflows.** The workflow is the domain object; a CI provider or application host is an execution environment that invokes it.

Foundry should let a host ask: *What workflow is requested? Is its definition valid? What happened when it ran? What evidence supports that result?*

## The core invariant

> **A workflow definition describes what should happen; Foundry validates and coordinates the run; the host supplies environmental capabilities and decides what to do with the result.**

The definition must not silently become executable code. A workflow's meaning should be inspectable before execution, and a run should produce an explicit result rather than relying only on console text or a host-specific exit code.

## Domain concepts

These are the proposed conceptual concepts for the first vertical slice. They are not yet implemented public API types; names and shapes must be validated against the prototype before being committed as package contracts.

| Concept | Meaning | Boundary |
|---|---|---|
| Workflow definition | A named, versioned description of an intended sequence or graph of work | Domain-owned; separate from host configuration |
| Step | One explicitly identified unit of work or stateful operation | Must use a declared capability; no implicit arbitrary code execution |
| Dependency / ordering | A declared condition that determines when a step may be considered | Validation must detect unsupported or contradictory relationships |
| Run | One attempt to execute a particular definition with explicit inputs | Has its own identity and lifecycle; definition and run are not interchangeable |
| Step outcome | A recorded result for one step, including success or a defined failure | Must distinguish “not run” from “ran and failed” |
| Workflow outcome | The terminal result of a run | Must not infer success merely because the host process returned without throwing |
| Evidence | Structured observations supporting the run and its outcomes | Preserve provenance and deterministic ordering where practical |
| Execution capability | A host-provided, explicitly registered operation that a step may invoke | The host owns environmental access; definitions cannot grant themselves permissions |

The first implementation should use the smallest useful subset of these concepts. This table is a domain map, not a promise that every concept belongs in the first release.

## What Foundry owns

- A versioned workflow-definition contract and its validation rules.
- Clear execution semantics: ordering, step lifecycle, terminal states, and outcome aggregation.
- Coordination between a definition and explicitly supplied execution capabilities.
- A structured run result with step outcomes and evidence.
- Predictable handling of invalid definitions and operational failures.
- Host-neutral behavior that can be exercised in automated tests without requiring a desktop UI or a live CI provider.

## What Foundry does not own

- **Finite-state-machine primitives.** FSM_API owns those mechanics when a Foundry component directly uses that package.
- **MicroBundle composition.** FSM_COS owns its composition boundary. A Foundry workflow definition is not an FSM_COS RuntimeManifest, and Foundry must not treat RuntimeAssembly as a workflow result.
- **Universal serialization.** A representation must be selected from actual contracts and needs; FSM_Serialization is not presumed to be a universal workflow schema.
- **The host environment.** GitHub Actions, AnyApp, WebApp, or another host supplies its own triggers, permissions, process lifecycle, and external capabilities.
- **Arbitrary script evaluation.** Workflow input is data, not trusted code. Any future executable capability requires an explicit contract, permission boundary, and tests.
- **Publication or deployment approval.** Building an artifact or producing a successful run must not publish a NuGet package, deploy an application, or publish an article automatically.
- **Presentation.** Trace files, SVGs, GUI views, and article renderings may explain evidence, but they are views of the domain and must not become its source of truth.

## Relationship to neighboring Workshop technologies

A host or CI provider supplies triggers, permissions, environment, and registered capabilities. Foundry defines, validates, coordinates, and reports a workflow. Explicit, tested adapters may use FSM_API for state mechanics or other host-provided capabilities.

FSM_COS remains a separate composition boundary when a workflow actually needs MicroBundle composition; it is not the workflow executor. This is a conceptual boundary description, not source-derived UML or a claim that all adapters exist. Each adapter must be justified by a concrete test and the real public contract it consumes.

## First release slice

The next useful milestone is not a general-purpose automation platform. It is a small, testable vertical slice that proves the domain end to end:

1. Define one versioned workflow using a representation chosen after comparing the typed-contract and persisted-definition approaches.
2. Validate a valid definition and reject at least one invalid definition with actionable diagnostics.
3. Execute a bounded set of explicitly supported steps through a small host-provided capability seam.
4. Produce structured step and workflow outcomes for both success and handled failure.
5. Demonstrate deterministic results across fresh processes where the contract promises determinism.
6. Exercise the same workflow path in CI and upload evidence that can be inspected without rerunning the job.

FSM_API remains a candidate for stateful step mechanics because the existing probe already demonstrates its real published API. The next design must establish whether that adapter is genuinely useful to a workflow definition or merely duplicates the existing state-machine sample. Reuse is earned by the test, not assumed because both domains use the word “state.”

## Safety and correctness invariants

- Invalid definitions fail before side effects begin.
- Every step kind is known to the runtime or explicitly supplied by the host.
- A definition cannot request permissions or capabilities that the host did not provide.
- Failure, cancellation, timeout, skipped work, and successful completion are distinct outcomes.
- The runtime does not report success when required work failed or never ran.
- Each run is isolated from other runs to the extent guaranteed by the chosen capability adapters.
- Deterministic output claims are limited to artifacts and values actually compared in tests.
- External publication and deployment remain disabled unless a user explicitly authorizes them.

These invariants are design requirements to turn into tests; they are not claims of current implementation.

## Theory to document as contracts become real

The domain theory should grow alongside the code and tests, with one authoritative explanation for each subject:

- **Workflow semantics:** definition identity, versioning, step graph/order, validation, and compatibility.
- **Execution semantics:** run lifecycle, state transitions, outcome aggregation, and isolation.
- **Failure theory:** invalid input versus execution failure, recovery policy, cancellation, timeout, retry, and idempotency.
- **Capability boundary:** how hosts provide operations, permissions, inputs, and environmental services without giving definitions arbitrary execution power.
- **Evidence model:** event identity, ordering, provenance, determinism, retention, and artifact association.
- **Representation:** how a workflow is persisted or exchanged, with explicit version and compatibility rules.
- **Host integration:** mapping Foundry outcomes to CI status or application behavior without confusing host behavior with core semantics.
- **Security model:** trust assumptions, untrusted definitions, least privilege, secret exposure, and side-effect boundaries.

Do not fill these sections with abstract prose alone. Each theory page must state what the concept is, why it exists, how it works, what it cannot guarantee, and which tests or source files verify it.

## Verification status

**Verified:** the current development branch contains a real FSM_API integration probe and a fox/sheepdog state scenario. CI runs the scenarios in separate processes and compares selected output artifacts.

**Not yet verified:** a Foundry-owned workflow definition, validation contract, general workflow runtime, structured workflow result, invalid-definition handling, failure/recovery semantics, or host-independent execution API.

The implementation and roadmap must be updated only when evidence changes. Documentation progress alone does not increase readiness.

## Related documents

- [README](../README.md) — product orientation and current proof.
- [Architecture](ARCHITECTURE.md) — subsystem boundaries and integration choices.
- [Contract inspection ledger](CONTRACT_INSPECTION.md) — source-backed findings and open questions.
- [Roadmap and readiness](ROADMAP.md) — milestones and evidence gates.
- [UML policy and inventory](UML/README.md) — source-grounded diagrams and coverage.

---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop avatar" width="200" /></p>
