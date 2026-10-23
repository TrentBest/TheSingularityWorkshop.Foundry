# Foundry Gallery

Foundry is intended to demonstrate manifest-defined, state-driven computation using The Singularity Workshop's own technologies.

**Current state:** no executable demonstration exists yet. Do not treat the concepts below as implemented behavior.

## Planned demonstrations

### 01 — A workflow executed by CI

**Goal:** GitHub Actions invokes a Foundry workflow definition, captures a structured outcome, and reports success or failure.

**Recipe:** To be added with the first runnable implementation.

**Evidence required:** source commit, test results, workflow run, emitted result/artifact, and repeat-run comparison.

### 02 — Explicit failure and recovery

**Goal:** A task failure follows a declared transition, produces an inspectable result, and does not silently report success.

**Recipe:** To be added after the basic runner exists.

**Evidence required:** deterministic test for the failure edge and the final CI exit status.

### 03 — Source-derived documentation

**Goal:** A declared task produces documentation from actual source evidence and verifies generated output.

**Recipe:** Future experiment; no Roslyn or GUI.SVG integration is claimed today.

**Evidence required:** source identity and spans, generated Markdown/SVG, stale-output check, and a test proving that a relevant source change updates only affected outputs.

## Gallery rules

- Pictures and diagrams must represent real execution or be explicitly labelled conceptual.
- Each demonstration links to a detailed recipe.
- Never label static mock data as live behavior.
- Record the commit and verification evidence for each living demonstration.