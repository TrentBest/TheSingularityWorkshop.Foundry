# Documentation Index

**Repository:** TheSingularityWorkshop.Foundry  
**Purpose:** Navigate design, implementation evidence, diagrams, demonstrations, and readiness.

## Entry points

- [README](README.md) — product intent and five-section overview.
- [Gallery](Gallery.md) — demonstrations and recipes.
- [Architecture](docs/ARCHITECTURE.md) — system boundaries and evidence labels.
- [Contract inspection ledger](docs/CONTRACT_INSPECTION.md) — source-backed findings from the Workshop stack and unresolved decisions.
- [Roadmap and readiness](docs/ROADMAP.md) — milestones and verification gates.
- [UML policy and inventory](docs/UML/README.md) — diagrams, source provenance, and coverage.

## Coverage ledger

| Area | Current status | Evidence / next action |
|---|---|---|
| Product vision and boundaries | Documented | README and architecture note |
| Source contract exploration | First pass recorded | [Contract inspection ledger](docs/CONTRACT_INSPECTION.md); deeper tests and WebApp source remain |
| Runnable workflow definition | Not implemented | Compare typed contract and persisted-manifest paths before schema commitment; current probe is not a workflow manifest |
| FSM_API integration | Real-package probe passes | [Probe source](src/Foundry.FsmApiProbe/Program.cs) and [successful CI run](https://github.com/TrentBest/TheSingularityWorkshop.Foundry/actions/runs/40021885286); reusable adapter and repeat-run suite pending |
| FSM_COS integration | Boundary inspected; Foundry integration not implemented | Only use for composition if a concrete workflow requires it |
| FSM_Serialization integration | Responsibility inspected; format not selected | Keep domain semantics in the owning contract; do not assume arbitrary-object serialization |
| CI execution | Probe workflow passes | [Workflow](.github/workflows/foundry-fsm-api-probe.yml); add manifest-level tests, failure cases, cancellation/timeout and repeatability |
| Source-derived UML | Not implemented | Add diagrams after source types exist |
| Living Gallery | First probe demonstrated | [Gallery recipe](Gallery.md#01--real-fsm_api-exercised-by-ci-first-proof); broader live workflow remains planned |
| Optional article publishing | Design documented; not implemented | Build source-grounded article preview and excerpt images; keep external publishing opt-in |
| Tests and abuse cases | One executable happy-path assertion | Add invalid input, handled failure/recovery, repeatability, cancellation and timeout behavior |
| Release/publication | Not in scope | Requires explicit approval |

## Update rule

Every claimed capability must point to implementation, tests, a workflow run, or a reproducible artifact. Keep proposed architecture separate from implemented behavior. Update this ledger when evidence changes.
