# Foundry Gallery

Foundry is intended to demonstrate manifest-defined, state-driven computation using The Singularity Workshop's own technologies.

**Current state:** both the basic FSM_API CI probe and the fox/sheepdog state-trace scenario are verified. The latter produces repeatable trace/SVG artifacts from real FSM_API execution. The broader workflow demonstrations below remain planned and must not be treated as implemented behavior.

## Planned demonstrations

### 01 — Real FSM_API exercised by CI (first proof)

**Status:** Verified on `development`.

**Recipe:** [Probe source](src/Foundry.FsmApiProbe/Program.cs) · [CI workflow](.github/workflows/foundry-fsm-api-probe.yml) · [Successful run](https://github.com/TrentBest/TheSingularityWorkshop.Foundry/actions/runs/40021885286)

The workflow restores and builds a .NET 8 console project against the published `TheSingularityWorkshop.FSM_API` 1.0.13 package, creates an FSM definition, verifies its initial `Ready` state, calls the public update path, and asserts transition to `Complete`. It cleans up only the definition it owns and uploads the output artifact.

**Scope limit:** this is a thin API integration probe, not a manifest-driven Foundry workflow runtime. No FSM_API global reset is used.

**Evidence:** [latest workflow runs](https://github.com/TrentBest/TheSingularityWorkshop.Foundry/actions/workflows/foundry-fsm-api-probe.yml), successful build and execution, identical output from two fresh processes, four PASS lines per run, and the `foundry-fsm-api-probe` artifact.

### 02 — Fox and sheepdog state trace

**Status:** Verified in CI.

**Recipe:** [Scenario source](src/Foundry.FoxSheepdog/Program.cs) · [UML](docs/UML/FoundryFoxSheepdogUML.md) · [Source notes](src/Foundry.FoxSheepdog/Program.md) · [Workflow](.github/workflows/foundry-fsm-api-probe.yml) · [Successful CI run](https://github.com/TrentBest/TheSingularityWorkshop.Foundry/actions/runs/40025709384)

The bounded scenario runs real FSM_API definitions for fox states Walking/Jumping/Fleeing/Mangled and dog states Sleeping/Awake/Chasing/Mangling. The dog requests the fox's terminal state through the fox FSM handle. Assertions fail the process if expected states are not observed. CI runs it twice and compares the trace and generated SVG byte-for-byte. This passed in [run 40025709384](https://github.com/TrentBest/TheSingularityWorkshop.Foundry/actions/runs/40025709384).

**Evidence boundary:** sensor inputs are scripted to make the run deterministic; this is not the original interactive SimpleDemos loop. The SVG is generated from recorded C# state-entry events, not an animation that independently invents behavior.

### 03 — Explicit failure and recovery

**Goal:** A task failure follows a declared transition, produces an inspectable result, and does not silently report success.

**Recipe:** To be added after the basic runner exists.

**Evidence required:** deterministic test for the failure edge and the final CI exit status.

### 04 — Source-derived documentation

**Goal:** A declared task produces documentation from actual source evidence and verifies generated output.

**Recipe:** Future experiment; no Roslyn or GUI.SVG integration is claimed today.

**Evidence required:** source identity and spans, generated Markdown/SVG, stale-output check, and a test proving that a relevant source change updates only affected outputs.

### 05 — Article with code-excerpt images (optional)

**Goal:** Build a technical article from a pinned source revision, with selected code excerpts rendered as readable images and the same code available as selectable text.

**Recipe:** To be added after the first executable workflow and excerpt renderer exist.

**Evidence required:** article preview artifact, exact repository/ref/path/line provenance for every excerpt, image readability/contrast check, text alternative, and repeatable output from the same source revision.

**Boundary:** article generation and preview should work with publishing disabled. External publication is a separate opt-in action; no destination or credentials are assumed.

## Gallery rules

- Pictures and diagrams must represent real execution or be explicitly labelled conceptual.
- Each demonstration links to a detailed recipe.
- Never label static mock data as live behavior.
- Record the commit and verification evidence for each living demonstration.
- Generated code images must remain traceable to source and must not replace selectable text.
