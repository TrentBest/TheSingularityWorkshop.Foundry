# Documentation Index

**Repository:** TheSingularityWorkshop.Foundry  
**Purpose:** Navigate product theory, implementation evidence, diagrams, demonstrations, and readiness.

## Entry points

- [README](README.md) — product intent and five-section overview.
- [Gallery](Gallery.md) — demonstrations and recipes.
- [Domain and ownership](docs/DOMAIN.md) — proposed Foundry domain, ownership boundaries, safety invariants, and first release slice.
- [Workflow semantics](docs/WORKFLOW_SEMANTICS.md) — version-1 execution model, validation, outcomes, and explicit limitations.
- [Architecture](docs/ARCHITECTURE.md) — system boundaries and evidence labels.
- [Contract inspection ledger](docs/CONTRACT_INSPECTION.md) — source-backed findings from the Workshop stack and unresolved decisions.
- [Roadmap and readiness](docs/ROADMAP.md) — milestones and verification gates.
- [UML policy and inventory](docs/UML/README.md) — diagrams, source provenance, and coverage.

## Coverage ledger

| Area | Current status | Evidence / next action |
|---|---|---|
| Product vision and boundaries | Domain and ownership documented against the implemented version-1 runtime | [Domain and ownership](docs/DOMAIN.md); validate proposed concepts against a working contract and tests |
| Source contract exploration | First pass recorded | [Contract inspection ledger](docs/CONTRACT_INSPECTION.md); deeper tests and WebApp source remain |
| Runnable workflow definition | Version-1 in-memory sequential contract implemented; latest CI verification pending | [Definition](src/Foundry.Workflow/WorkflowDefinition.cs), [validator](src/Foundry.Workflow/WorkflowValidator.cs), [runner](src/Foundry.Workflow/WorkflowRunner.cs); persisted format remains open |
| FSM_API integration | Basic real-package probe passes; expanded state scenario added | [Probe source](src/Foundry.FsmApiProbe/Program.cs), [fox/sheepdog scenario](src/Foundry.FoxSheepdog/Program.cs), [CI workflow](.github/workflows/foundry-fsm-api-probe.yml), and [successful expanded CI run](https://github.com/TrentBest/TheSingularityWorkshop.Foundry/actions/runs/40025709384) |
| FSM_COS integration | Boundary inspected; Foundry integration not implemented | Only use for composition if a concrete workflow requires it |
| FSM_Serialization integration | Responsibility inspected; format not selected | Keep domain semantics in the owning contract; do not assume arbitrary-object serialization |
| CI execution | Earlier FSM_API/fox-sheepdog proof passes; workflow-runtime test result still needs confirmation | [Workflow](.github/workflows/foundry-fsm-api-probe.yml); verify the current run before raising readiness |
| Source-derived UML | Not implemented | Add diagrams after domain types exist |
| Living Gallery | Basic probe and trace-derived SVG scenario verified | [Gallery](Gallery.md) and [successful expanded CI run](https://github.com/TrentBest/TheSingularityWorkshop.Foundry/actions/runs/40025709384) |
| Optional article publishing | Design documented; not implemented | Build source-grounded article preview and excerpt images; keep external publishing opt-in |
| Tests and abuse cases | Runtime tests cover order, invalid definitions, failure, cancellation, pre-cancelled runs, and duplicate handler keys; latest CI verification pending | Add timeout semantics, broader adversarial inputs, and recovery/retry only after contracts are defined |
| Release/publication | Not in scope | Requires explicit approval |

## Update rule

Every claimed capability must point to implementation, tests, a workflow run, or a reproducible artifact. Keep proposed architecture separate from implemented behavior. Update this ledger when evidence changes.
