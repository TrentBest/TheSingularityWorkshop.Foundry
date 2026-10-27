# Foundry Roadmap and Readiness

**Current readiness: 30% pending reassessment.** The real-package FSM_API probe and deterministic fox/sheepdog scenario passed prior CI. A first Foundry-owned workflow runtime and focused tests have now been added on `development`; the current CI run must verify them before readiness is recalculated.

## Milestones

### M0 — Repository and documentation bootstrap
- [x] Establish repository identity and MIT license.
- [x] State intended boundaries and distinguish target from implementation.
- [x] Add documentation navigation, Gallery, and roadmap.
- [ ] Verify repository branch policy and protect intended development flow.

### M1 — Define the domain from real Workshop contracts
- [x] Inspect FSM_API public builder/context APIs and its CI workflow.
- [x] Inspect FSM_COS composition contract and runtime boundary.
- [x] Inspect the FSM_Serialization representation boundary.
- [x] Inspect ProtocolAi/GrammarAi responsibility boundaries and AnyApp's host path at README/project/source level.
- [x] Record an initial Foundry domain and ownership proposal in [DOMAIN.md](DOMAIN.md).
- [ ] Inspect deeper tests and WebApp implementation before selecting an integration seam.
- [ ] Decide whether the first useful runtime is a small workflow coordinator plus adapters, and prove the choice with tests.
- [ ] Choose typed versus persisted workflow-definition representation based on concrete use cases and compatibility needs.
- [ ] Record final contract decisions and source-derived UML.

### M2 — First executable workflow
- [x] Add a minimal buildable .NET 8 probe project (focused test suite still pending).
- [x] Define a versioned in-memory workflow definition contract (persisted format remains open).
- [x] Document version-1 execution semantics and implementation-backed limits in [WORKFLOW_SEMANTICS.md](WORKFLOW_SEMANTICS.md).
- [x] Execute and assert a deterministic state transition through the actual published FSM_API API in a probe.
- [x] Demonstrate a deterministic terminal state in the API probe; workflow-level terminal-result model remains pending.
- [x] Add and verify a deterministic multi-state fox/sheepdog scenario with explicit state assertions.
- [x] Add invalid-definition, handled-failure, ordering, cancellation, and handler-registration tests.
- [x] Produce structured step and workflow outcomes distinct from console output and host exit codes.

### M3 — CI proof
- [x] Run the FSM_API probe in GitHub Actions; the manifest-driven workflow itself remains pending.
- [x] Verify CI exit-code mapping and upload a probe-output artifact; workflow result/artifact contract remains pending.
- [x] Compare deterministic output from two fresh basic-probe processes.
- [x] Run the fox/sheepdog scenario in two fresh processes and compare TSV and SVG evidence byte-for-byte.
- [ ] Prove workflow-level timeout/cancellation behavior.
- [ ] Harden permissions for untrusted pull requests.
- [ ] Publish workflow logs and a concise evidence summary.

### M4 — Living documentation and optional article production
- [ ] Align README and existing docs with the FSM_COS reader journey, moniker, badge, visual, and footer standards.
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
| Purpose and dependency boundaries | 15% | Source-level contract inspection plus an initial domain proposal |
| Contracts and implementation | 20% | Version-1 in-memory workflow contract and sequential runtime added; CI verification pending |
| Automated tests | 20% | Focused xUnit project added; current CI verification pending |
| CI execution and security | 20% | Pinned-action workflow has read-only contents permission and a 15-minute timeout; broader threat cases pending |
| Demonstration and reproducibility | 15% | Successful basic probe plus fox/dog state assertions, cross-FSM transition, repeatable trace/SVG, and uploaded artifact |
| Documentation/UML completeness | 10% | Bootstrap docs and source-inspection notes; source-derived Foundry UML awaits domain types |

**No release recommendation yet.** Readiness rises when gates are supported by verifiable implementation and test evidence, not merely by adding documents. Documentation-only progress does not by itself increase readiness. The 30% estimate reflects two verified CI proofs, not a release-ready workflow system.
