# Foundry Roadmap and Readiness

**Current readiness: 10%.** This is a bootstrap estimate, not a release claim. No executable behavior or CI run has been verified.

## Milestones

### M0 — Repository and documentation bootstrap
- [x] Establish repository identity and MIT license.
- [x] State intended boundaries and distinguish target from implementation.
- [x] Add documentation navigation, Gallery, and roadmap.
- [ ] Verify repository branch policy and protect intended development flow.

### M1 — Inspect the actual Workshop contracts
- [ ] Inspect FSM_API public APIs and tests.
- [ ] Inspect FSM_COS composition contracts and current limitations.
- [ ] Inspect AnyApp/WebApp manifest and hosting contracts.
- [ ] Decide whether Foundry needs its own runtime or only a workflow definition and thin adapter.
- [ ] Record decisions and evidence in architecture/UML documentation.

### M2 — First executable workflow
- [ ] Add a minimal buildable project and focused tests.
- [ ] Define and validate one versioned workflow definition.
- [ ] Demonstrate deterministic state transitions and terminal results.
- [ ] Add happy-path, invalid-definition, and handled-failure tests.

### M3 — CI proof
- [ ] Run the same workflow in GitHub Actions.
- [ ] Verify output artifacts and exit-code mapping.
- [ ] Exercise timeout/cancellation and repeat-run behavior.
- [ ] Harden permissions for untrusted pull requests.
- [ ] Publish workflow logs and a concise evidence summary.

### M4 — Living documentation
- [ ] Generate diagrams from real source/runtime evidence.
- [ ] Include stable type identity, source spans, and provenance.
- [ ] Add stale-output verification and a focused change test.
- [ ] Add Gallery recipes linked to successful runs.

### M5 — Host and language expansion
- [ ] Evaluate AnyApp/WebApp as hosts only after the CI proof works.
- [ ] Evaluate ProtocolAi/GrammarAi adapters with explicit contracts.
- [ ] Add additional languages only when the first cross-boundary proof is testable.

## Readiness rubric

| Gate | Weight | Current evidence |
|---|---:|---|
| Purpose and dependency boundaries | 15% | Initial docs |
| Contracts and implementation | 20% | Not inspected/implemented in this repo |
| Automated tests | 20% | None |
| CI execution and security | 20% | None verified |
| Demonstration and reproducibility | 15% | Planned |
| Documentation/UML completeness | 10% | Bootstrap docs added; source-derived UML awaits code |

**No release recommendation yet.** Readiness rises when gates are supported by verifiable implementation and test evidence, not merely by adding documents.