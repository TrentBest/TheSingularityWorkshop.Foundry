# TheSingularityWorkshop.Foundry

**A manifest-driven workflow and computation experiment for The Singularity Workshop.**

Foundry explores whether complex, stateful workflows can be described as data and executed through the Workshop's own technologies—rather than embedding all orchestration logic in CI scripts or a host application.

> **Status: first CI probe passes.** A small .NET 8 console probe now consumes the published FSM_API 1.0.13 package, creates an FSM, verifies its initial state, executes a transition, and cleans up only its own definition. The Foundry workflow-manifest/runtime contract remains unimplemented.

## 01 — Vision

Define a workflow once, make its steps and transitions inspectable, and run it in different hosts. The first proof should be deliberately small: a deterministic FSM-driven workflow executed by GitHub Actions, with observable success and failure outcomes.

Foundry is not intended to replace the Workshop's technologies or become a competing application framework.

## 02 — Architectural boundaries

- **FSM_API** — state-machine mechanics where a component actually consumes FSM_API.
- **FSM_COS** — runtime composition and capability coordination, subject to what its current implementation supports.
- **FSM_Serialization** — binary representation infrastructure; it is not assumed to provide a universal workflow schema.
- **ProtocolAi / GrammarAi** — possible future language-neutral contracts and language-specific grammar capabilities; not assumed to be integrated.
- **AnyApp / WebApp** — potential hosts for manifest-driven workflows; hosting is a later proof, not a prerequisite for the first CI experiment.
- **GitHub Actions** — trigger, runner, permissions, timeout, cancellation, and job-result boundary.
- **Foundry** — the candidate workflow definition, validation, execution coordination, and result-reporting layer to be designed and tested here.
- **Optional article publishing** — a future capability to build source-grounded technical articles and readable code-excerpt images; disabled by default and separate from the core runner.

A package name must reflect actual dependencies. Foundry deliberately has no FSM_ prefix unless its own package identity and direct dependency relationship later justify that choice.

## 03 — First proof

The first verified CI slice is deliberately narrower than the eventual workflow engine. It proves that Foundry's CI can compile and exercise the real FSM_API package without a mock:

1. Restore and build the probe against `TheSingularityWorkshop.FSM_API` 1.0.13.
2. Create a uniquely named FSM definition and instance.
3. Verify `Ready` → `Complete` through the public FSM_API update path.
4. Clean up the probe-owned definition; never call the known-defective global reset.
5. Upload the execution output as a short-lived CI artifact.

The larger workflow proof still needs a versioned definition contract, validation, explicit failure/recovery, and repeatability tests. Publication and deployment remain outside the experiment and behind explicit approval.

The implementation choice must follow repository evidence. Prefer reusing FSM_API/FSM_COS where their real contracts fit; do not build a parallel runtime or claim integration before it works. See the [source contract inspection ledger](docs/CONTRACT_INSPECTION.md) for the first pass through actual repositories and the remaining unknowns.

## 04 — See it in a minute

**The first CI probe passes.** [Open the GitHub Actions run](https://github.com/TrentBest/TheSingularityWorkshop.Foundry/actions/runs/40021885286) or inspect the [probe source](src/Foundry.FsmApiProbe/Program.cs) and [workflow](.github/workflows/foundry-fsm-api-probe.yml). The run restored and built the .NET 8 project, executed the real FSM_API transition, and uploaded `foundry-fsm-api-probe`. This is an API integration probe—not yet a manifest-driven Foundry workflow engine.

## 05 — Documentation and readiness

- [Gallery](Gallery.md) — planned living demonstrations and recipes, including optional article production.
- [Documentation index](DOCUMENTATION_INDEX.md) — documentation entry points and coverage.
- [Architecture](docs/ARCHITECTURE.md) — boundaries, evidence, and non-goals.
- [Contract inspection ledger](docs/CONTRACT_INSPECTION.md) — source-backed findings and unresolved decisions.
- [Roadmap and readiness](docs/ROADMAP.md) — staged implementation and verification gates.
- [UML documentation](docs/UML/README.md) — diagram policy and current evidence status.

Readiness is **20% (first executable API probe and successful CI run)**. This proves the package restore/build and one real state transition. It does not yet prove a Foundry workflow manifest, failure/recovery behavior, invalid-definition rejection, or cross-repository orchestration.
