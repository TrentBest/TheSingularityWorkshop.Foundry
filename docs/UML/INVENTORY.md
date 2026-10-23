# UML and Source Documentation Inventory

**Inventory date:** 2026-10-23  
**Branch:** development  
**Scope:** basic CI probe and fox/sheepdog state-trace proof added.

## Source inventory

The first executable source file is a thin CI probe, not the Foundry workflow engine. A second executable now proves a bounded fox/sheepdog state sequence and emits trace-derived SVG evidence. The inventory distinguishes these verified proofs from still-undecided workflow contracts.

| Source area | Source files | Companion Markdown | Neighborhood UML | Status |
|---|---:|---:|---:|---|
| FSM_API CI probe | 1 (`Program.cs`) | `Program.md` | `FoundryFsmApiProbeUML.md` | Implemented; CI run passes |
| Fox/sheepdog CI scenario | 1 (`Program.cs`) | `Program.md` | `FoundryFoxSheepdogUML.md` | Implemented; CI run passes with repeatable trace/SVG artifacts |
| Workflow definition/manifest | 0 verified | N/A | N/A | Contract undecided |
| Reusable execution adapter | 0 verified | N/A | N/A | Not implemented; probe is deliberately narrow |
| CI runner integration | 1 workflow | Workflow YAML | N/A | Probe workflow passes |

## Planned view coverage

| View | Status | Evidence needed |
|---|---|---|
| Architecture/component | Conceptual target only | Confirmed contracts and source |
| Class neighborhoods | Pending | Actual declared types |
| Sequence: successful workflow | Pending | Implementation and tests |
| Sequence: failure/recovery | Pending | Failure-path implementation and tests |
| Activity/control flow | Pending | Real decision logic |
| State lifecycle | Pending | Actual FSM and runner lifecycle |
| Use cases | Initial goals in target diagram | User/CI scenarios validated with the owner |
| Data model and lineage | Pending | Actual workflow schema and artifact flow |
| Test traceability | Pending | Tests and requirement identifiers |

## Exit criteria

Update this inventory whenever source changes. Every generated view should identify its source revision and evidence. The probe neighborhood is manual and source-grounded; it must not be described as generated. A conceptual diagram must never be counted as source-derived coverage.