# Documentation Index

**Repository:** TheSingularityWorkshop.Foundry  
**Purpose:** Navigate design, implementation evidence, diagrams, demonstrations, and readiness.

## Entry points

- [README](README.md) — product intent and five-section overview.
- [Gallery](Gallery.md) — demonstrations and recipes.
- [Architecture](docs/ARCHITECTURE.md) — system boundaries and evidence labels.
- [Roadmap and readiness](docs/ROADMAP.md) — milestones and verification gates.
- [UML policy and inventory](docs/UML/README.md) — diagrams, source provenance, and coverage.

## Coverage ledger

| Area | Current status | Evidence / next action |
|---|---|---|
| Product vision and boundaries | Documented | README and architecture note |
| Runnable workflow definition | Not implemented | Define only after inspecting actual dependency APIs |
| FSM_API integration | Source API inspected; adapter not implemented | Build a small compile/test proof before choosing final runner shape |
| FSM_COS integration | Boundary inspected; Foundry integration not implemented | Only use for composition if a concrete workflow requires it |
| FSM_Serialization integration | Responsibility inspected; format not selected | Keep domain semantics in the owning contract |
| CI execution | Not implemented | Add safe build/test workflow after runnable project exists |
| Source-derived UML | Not implemented | Add diagrams after source types exist |
| Living Gallery | Planned | Demonstrate actual runtime output, not mockups |
| Optional article publishing | Design documented; not implemented | Build source-grounded article preview and excerpt images; keep external publishing opt-in |
| Tests and abuse cases | Not implemented | Happy path, invalid input, failure path, repeatability, cancellation and timeout behavior |
| Release/publication | Not in scope | Requires explicit approval |

## Update rule

Every claimed capability must point to implementation, tests, a workflow run, or a reproducible artifact. Keep proposed architecture separate from implemented behavior. Update this ledger when evidence changes.
