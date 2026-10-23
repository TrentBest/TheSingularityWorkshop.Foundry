# TheSingularityWorkshop.Foundry

**A manifest-driven workflow and computation experiment for The Singularity Workshop.**

Foundry explores whether complex, stateful workflows can be described as data and executed through the Workshop's own technologies—rather than embedding all orchestration logic in CI scripts or a host application.

> **Status: repository bootstrap / design experiment.** No Foundry runtime, manifest contract, FSM execution adapter, or end-to-end CI proof is implemented yet. This README describes the intended direction, not existing behavior.

## 01 — Vision

Define a workflow once, make its steps and transitions inspectable, and run it in different hosts. The first proof should be deliberately small: a deterministic FSM-driven workflow executed by GitHub Actions, with observable success and failure outcomes.

Foundry is not intended to replace the Workshop's technologies or become a competing application framework.

## 02 — Architectural boundaries

- **FSM_API** — state-machine mechanics where a component actually consumes FSM_API.
- **FSM_COS** — runtime composition and capability coordination, subject to what its current implementation supports.
- **ProtocolAi / GrammarAi** — possible future language-neutral contracts and language-specific grammar capabilities; not assumed to be integrated.
- **AnyApp / WebApp** — potential hosts for manifest-driven workflows; hosting is a later proof, not a prerequisite for the first CI experiment.
- **GitHub Actions** — trigger, runner, permissions, timeout, cancellation, and job-result boundary.
- **Foundry** — the candidate workflow definition, validation, execution coordination, and result-reporting layer to be designed and tested here.

A package name must reflect actual dependencies. Foundry deliberately has no FSM_ prefix unless its own package identity and direct dependency relationship later justify it.

## 03 — First proof

The initial experiment should demonstrate one real workflow—not a decorative sample:

1. Read and validate a versioned workflow definition.
2. Execute a small, deterministic state machine with explicit transitions.
3. Record step outcomes and a final result that CI can interpret.
4. Demonstrate success, a handled failure path, and an invalid-definition rejection.
5. Verify repeatability and artifact/output correctness.
6. Keep publication and deployment outside the experiment and behind explicit approval.

The implementation choice must follow repository evidence. Prefer reusing FSM_API/FSM_COS where their real contracts fit; do not build a parallel runtime or claim integration before it works.

## 04 — See it in a minute

**Not available yet.** The repository currently has no runnable Foundry workflow engine or end-to-end CI demonstration. The first milestone is to add the smallest executable proof and its automated tests; this section should then link to that proof and show its actual output.

## 05 — Documentation and readiness

- [Gallery](Gallery.md) — planned living demonstrations and recipes.
- [Documentation index](DOCUMENTATION_INDEX.md) — documentation entry points and coverage.
- [Architecture](docs/ARCHITECTURE.md) — boundaries, evidence, and non-goals.
- [Roadmap and readiness](docs/ROADMAP.md) — staged implementation and verification gates.
- [UML documentation](docs/UML/README.md) — diagram policy and current evidence status.

Readiness is **10% (bootstrap only)**: repository identity and initial design boundaries are documented; no executable behavior has been verified. The percentage will increase only when implementation and tests provide evidence.
