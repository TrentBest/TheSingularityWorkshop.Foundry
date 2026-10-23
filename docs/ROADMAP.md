# Foundry Roadmap and Readiness

**Current readiness: 20% pending expanded CI verification.** The original real-package FSM_API probe passes. A second fox/sheepdog scenario has been added to exercise multiple states, a cross-FSM transition, and trace-derived SVG output; readiness will be recalculated only after the updated workflow passes.

## Milestones

### M0 — Repository and documentation bootstrap
- [x] Establish repository identity and MIT license.
- [x] State intended boundaries and distinguish target from implementation.
- [x] Add documentation navigation, Gallery, and roadmap.
- [ ] Verify repository branch policy and protect intended development flow.

### M1 — Inspect the actual Workshop contracts
- [x] Inspect FSM_API public builder/context APIs and its CI workflow.
- [x] Inspect FSM_COS composition contract and runtime boundary.
- [x] Inspect the FSM_Serialization representation boundary.
- [x] Inspect ProtocolAi/GrammarAi responsibility boundaries and AnyApp's host path at README/project/source level.
- [ ] Inspect deeper tests and WebApp implementation before selecting an integration seam.
- [ ] Decide whether Foundry needs its own runtime or only a workflow definition and thin adapter.
- [ ] Record final decisions and source-derived UML.

### M2 — First executable workflow
- [x] Add a minimal buildable .NET 8 probe project (focused test suite still pending).
- [ ] Define and validate one versioned workflow definition using a representation chosen from evidence.
- [x] Execute and assert a deterministic state transition through the actual published FSM_API API in a probe.
- [x] Demonstrate a deterministic terminal state in the API probe; workflow-level terminal-result model remains pending.
- [x] Add a deterministic multi-state fox/sheepdog scenario with explicit state assertions (expanded CI run pending).
- [ ] Add invalid-definition and handled-failure tests.

### M3 — CI proof
- [x] Run the FSM_API probe in GitHub Actions; the manifest-driven workflow itself remains pending.
- [x] Verify CI exit-code mapping and upload a probe-output artifact; workflow result/artifact contract remains pending.
- [x] Compare deterministic output from two fresh basic-probe processes.
- [x] Add a second fresh-process comparison for fox/sheepdog TSV and SVG evidence (verification pending).
- [ ] Prove workflow-level timeout/cancellation behavior.
- [ ] Harden permissions for untrusted pull requests.
- [ ] Publish workflow logs and a concise evidence summary.

### M4 — Living documentation and optional article production
- [ ] Generate diagrams from real source/runtime evidence.
- [ ] Include stable type identity, source spans, and provenance.
- [ ] Add stale-output verification and a focused change test.
- [ ] Add Gallery recipes linked to successful runs.
- [ ] Add an opt-in article build that selects real source excerpts and records repository revision/path/line range.
- [ ] Generate high-contrast, readable code-excerpt images with equivalent selectable text and accessible descriptions.
- [ ] Produce a local/CI preview artifact without requiring publishing credentials.
- [ ] Define a publisher adapter only after selecting a destination; keep actual publication disabled unless explicitly approved.

### M5 — Host and language expansion
- [ ] Evaluate AnyApp/WebApp as hosts only after the CI proof works.
- [ ] Evaluate ProtocolAi/GrammarAi adapters with explicit contracts.
- [ ] Add additional languages only when the first cross-boundary proof is testable.

## Readiness rubric

| Gate | Weight | Current evidence |
|---|---:|---|
| Purpose and dependency boundaries | 15% | Initial docs plus source-level contract inspection |
| Contracts and implementation | 20% | Thin FSM_API probe implemented; workflow contract/runtime absent |
| Automated tests | 20% | No Foundry tests |
| CI execution and security | 20% | Pinned-action workflow passes with read-only contents permission and 10-minute timeout; broader threat cases pending |
| Demonstration and reproducibility | 15% | Successful run, output artifact, and byte-for-byte comparison across two fresh probe processes |
| Documentation/UML completeness | 10% | Bootstrap docs and source-inspection notes; source-derived Foundry UML awaits code |

**No release recommendation yet.** Readiness rises when gates are supported by verifiable implementation and test evidence, not merely by adding documents. Documentation-only progress does not by itself increase readiness. The 20% estimate reflects the verified probe, not a release-ready workflow system.
