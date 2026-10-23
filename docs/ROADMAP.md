# Foundry Roadmap and Readiness

**Current readiness: 10%.** This is a bootstrap estimate, not a release claim. No executable Foundry behavior or end-to-end Foundry CI run has been verified.

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
- [ ] Add a minimal buildable project and focused tests.
- [ ] Define and validate one versioned workflow definition using a representation chosen from evidence.
- [ ] Execute deterministic state transitions through the actual FSM_API API if the focused prototype confirms suitability.
- [ ] Demonstrate deterministic terminal results.
- [ ] Add happy-path, invalid-definition, and handled-failure tests.

### M3 — CI proof
- [ ] Run the same workflow in GitHub Actions.
- [ ] Verify output artifacts and exit-code mapping.
- [ ] Exercise timeout/cancellation and repeat-run behavior.
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
| Contracts and implementation | 20% | Foundry implementation absent |
| Automated tests | 20% | No Foundry tests |
| CI execution and security | 20% | No Foundry CI proof verified |
| Demonstration and reproducibility | 15% | Planned |
| Documentation/UML completeness | 10% | Bootstrap docs and source-inspection notes; source-derived Foundry UML awaits code |

**No release recommendation yet.** Readiness rises when gates are supported by verifiable implementation and test evidence, not merely by adding documents. Documentation-only progress does not by itself increase the 10% estimate.
