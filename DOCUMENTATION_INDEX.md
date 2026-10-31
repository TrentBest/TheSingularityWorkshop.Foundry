<!-- The Singularity Workshop moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# Documentation Index

**Repository:** TheSingularityWorkshop.Foundry  
**Purpose:** Navigate product theory, implementation evidence, diagrams, demonstrations, and readiness.

## Entry points

- [README](README.md) — product intent and five-section overview.
- [Agent onboarding](AGENTS.md) — repository-specific contribution, safety, branch, release, and documentation rules.
- [Gallery](Gallery.md) — demonstrations and recipes.
- [Domain and ownership](docs/DOMAIN.md) — proposed Foundry domain, ownership boundaries, safety invariants, and first release slice.
- [Workflow semantics](docs/WORKFLOW_SEMANTICS.md) — version-1 execution model, validation, outcomes, and explicit limitations.
- [Governed work task lifecycle](docs/WORK_TASK_LIFECYCLE.md) — task envelopes, fenced leases, local journals, capabilities, CODE RED notices, and exact-CI repair decisions.
- [CiAi artifact callback contract](docs/CIAI_ARTIFACT_CALLBACK_CONTRACT.md) — Foundry-owned v1 HTTPS ingress, authentication, durable idempotency receipts, immutable artifact pull, and provider acknowledgement.
- [Architecture](docs/ARCHITECTURE.md) — system boundaries and evidence labels.
- [Contract inspection ledger](docs/CONTRACT_INSPECTION.md) — source-backed findings from the Workshop stack and unresolved decisions.
- [Roadmap and readiness](docs/ROADMAP.md) — milestones and verification gates.
- [UML policy and inventory](docs/UML/README.md) — diagrams, source provenance, and coverage.

## Coverage ledger

| Area | Current status | Evidence / next action |
|---|---|---|
| Product vision and boundaries | Domain and ownership documented against the implemented version-1 runtime | [Domain and ownership](docs/DOMAIN.md); validate proposed concepts against a working contract and tests |
| Source contract exploration | First pass recorded | [Contract inspection ledger](docs/CONTRACT_INSPECTION.md); deeper tests and WebApp source remain |
| Runnable workflow definition | Version-1 in-memory sequential contract implemented and covered by CI | [Definition](src/Foundry.Workflow/WorkflowDefinition.cs), [validator](src/Foundry.Workflow/WorkflowValidator.cs), [runner](src/Foundry.Workflow/WorkflowRunner.cs); persisted workflow-definition format remains open |
| FSM_API integration | Basic real-package probe passes; expanded state scenario added | [Probe source](src/Foundry.FsmApiProbe/Program.cs), [fox/sheepdog scenario](src/Foundry.FoxSheepdog/Program.cs), [CI workflow](.github/workflows/foundry-fsm-api-probe.yml), and [successful expanded CI run](https://github.com/TrentBest/TheSingularityWorkshop.Foundry/actions/runs/40025709384) |
| FSM_COS integration | Boundary inspected; Foundry integration not implemented | Only use for composition if a concrete workflow requires it |
| FSM_Serialization integration | Responsibility inspected; format not selected | Keep domain semantics in the owning contract; do not assume arbitrary-object serialization |
| Governed work lifecycle | Implemented on PR #10 feature branch; not yet merged or connected to live services | [Work-task lifecycle](docs/WORK_TASK_LIFECYCLE.md); local journals are single-process, not GitHub-backed or distributed |
| CiAi artifact ingress | Host-owned v1 callback contract documented; no live receiver or remote transport yet | [CiAi artifact callback contract](docs/CIAI_ARTIFACT_CALLBACK_CONTRACT.md); ready notice + immutable pull + durable receipt boundary |
| CI execution | PR #10 head passed workflow-runtime tests, builds, FSM_API probe, repeatable fox/sheepdog evidence, and documentation checks | [Run #196](https://github.com/TrentBest/TheSingularityWorkshop.Foundry/actions/runs/41146291917) on commit `e85fd23fd5f897faaf1f28ab6d2141baeed657dd`; reverify after later commits |
| Source-derived UML | Not implemented | Add diagrams after domain types exist |
| Living Gallery | Basic probe and trace-derived SVG scenario verified | [Gallery](Gallery.md) and [successful expanded CI run](https://github.com/TrentBest/TheSingularityWorkshop.Foundry/actions/runs/40025709384) |
| Optional article publishing | Design documented; not implemented | Build source-grounded article preview and excerpt images; keep external publishing opt-in |
| Tests and abuse cases | Workflow and governed-task tests cover ordering, invalid input, failure, cancellation, bounded task/lease/audit metadata, strict journal replay, lease fencing, and repair-policy provenance | CI run #186 passed the current PR head; add timeout semantics and deterministic cross-service integration tests next |
| Release/publication | Not in scope | Requires explicit approval |

## Update rule

Every claimed capability must point to implementation, tests, a workflow run, or a reproducible artifact. Keep proposed architecture separate from implemented behavior. Update this ledger when evidence changes.

---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop avatar" width="200" /></p>
