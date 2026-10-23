# TheSingularityWorkshop.Foundry

**A manifest-driven workflow and computation experiment for The Singularity Workshop.**

Foundry explores whether complex, stateful workflows can be described as data and executed through the Workshop's own technologies—rather than embedding all orchestration logic in CI scripts or a host application.

> **Status: first CI probe passes; fox/sheepdog proof added and awaiting CI verification.** Foundry now has a second .NET 8 executable that exercises the fox and sheepdog state contracts through FSM_API, asserts the cross-FSM transition, and emits a TSV trace plus SVG visualization. The workflow runs both executables and compares the fox/sheepdog evidence across fresh processes. The Foundry workflow-manifest/runtime contract remains unimplemented.

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

The verified CI slice is deliberately narrower than the eventual workflow engine. It proves that Foundry's CI can compile and exercise the real FSM_API package without a mock. A second scenario extends the proof to the SimpleDemos fox/sheepdog state pattern and emits evidence intended for a future GUI:

1. Restore and build the probe against `TheSingularityWorkshop.FSM_API` 1.0.13.
2. Create a uniquely named FSM definition and instance.
3. Verify `Ready` → `Complete` through the public FSM_API update path.
4. Clean up the probe-owned definition; never call the known-defective global reset.
5. Run the probe twice in separate processes, compare exact output, and upload both outputs as a short-lived CI artifact.

The basic probe's repeatability check runs twice in fresh processes and compares output byte-for-byte. The fox/sheepdog scenario is also run twice; CI compares its trace and SVG artifacts byte-for-byte. Its scripted tick inputs are deliberate: this verifies real FSM_API state execution and evidence generation without pretending to run the original interactive browser animation. The larger workflow proof still needs a versioned definition contract, validation, and explicit failure/recovery. Publication and deployment remain outside the experiment and behind explicit approval.

The implementation choice must follow repository evidence. Prefer reusing FSM_API/FSM_COS where their real contracts fit; do not build a parallel runtime or claim integration before it works. See the [source contract inspection ledger](docs/CONTRACT_INSPECTION.md) for the first pass through actual repositories and the remaining unknowns.

## 04 — See it in a minute

**Start with the [FSM_API probe](src/Foundry.FsmApiProbe/Program.cs) and [fox/sheepdog scenario](src/Foundry.FoxSheepdog/Program.cs).** The updated [CI workflow](.github/workflows/foundry-fsm-api-probe.yml) builds both projects, executes the fox/dog scenario twice, checks state assertions and deterministic output, then uploads a `fox-sheepdog.trace` and a `fox-sheepdog.svg` generated from the observed events. The fox/dog addition is awaiting its first successful CI verification. This remains a state-execution proof, not yet a manifest-driven Foundry workflow engine.

## 05 — Documentation and readiness

- [Gallery](Gallery.md) — planned living demonstrations and recipes, including optional article production.
- [Documentation index](DOCUMENTATION_INDEX.md) — documentation entry points and coverage.
- [Architecture](docs/ARCHITECTURE.md) — boundaries, evidence, and non-goals.
- [Contract inspection ledger](docs/CONTRACT_INSPECTION.md) — source-backed findings and unresolved decisions.
- [Roadmap and readiness](docs/ROADMAP.md) — staged implementation and verification gates.
- [UML documentation](docs/UML/README.md) — diagram policy and current evidence status.

Readiness remains **20% until the expanded workflow is verified**. The basic probe has passed; the fox/sheepdog state trace, SVG generation, cross-FSM transition, and repeatability check must pass CI before readiness is recalculated. No Foundry workflow manifest, failure/recovery behavior, invalid-definition rejection, or cross-repository orchestration is claimed.
