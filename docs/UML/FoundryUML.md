<!-- The Singularity Workshop moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# Foundry Architecture UML

> **Diagram status: CONCEPTUAL TARGET — not a diagram of implemented classes or a verified runtime.** No source types are inferred from this drawing.

## Target component flow

```mermaid
flowchart TD
    Author[Workflow author] --> Definition[Versioned workflow definition]
    Definition --> Validate[Foundry validation boundary]
    Validate -->|valid| Execute[Workflow execution coordinator]
    Validate -->|invalid| Reject[Validation result / non-zero CI outcome]
    Execute --> Adapter[Capability adapter]
    Adapter -. only where supported by actual contracts .-> FSM[FSM_API]
    Adapter -. composition where verified .-> COS[FSM_COS]
    Execute --> Result[Step results and final outcome]
    Result --> CI[GitHub Actions job result and artifacts]
    Host[AnyApp / WebApp host] -. future hosting option .-> Execute
```

Solid arrows show the intended control flow. Dashed links are integration options that must be confirmed against the real APIs before implementation. This drawing does not claim those adapters or the execution coordinator exist.

## Use cases

```mermaid
flowchart LR
    Author[Workflow author] --> UC1((Define a workflow))
    CI[GitHub Actions] --> UC2((Run a workflow))
    CI --> UC3((Collect result and artifacts))
    Maintainer[Maintainer] --> UC4((Inspect a failed step))
    Maintainer --> UC5((Verify repeatability))
    Security[Repository security boundary] --> UC6((Reject unsafe or invalid input))
```

These are intended goals for the first experiment. Validate and refine them as runnable behavior and tests are added.

## Questions to settle from source evidence

- What public FSM_API surface is appropriate for expressing or executing workflow states?
- What does FSM_COS currently compose, and what does it not yet do?
- Is a dedicated Foundry execution runtime justified, or is a thin adapter enough?
- How should step outputs, failure causes, cancellation, and timeouts be represented?
- Which pieces belong in a host, a reusable package, and the CI workflow?

Do not add class diagrams until actual source types exist. Once they do, create a separate `[TypeName]UML.md` neighborhood page for each significant type and companion documentation for source files according to the Workshop standard.

---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop avatar" width="200" /></p>
