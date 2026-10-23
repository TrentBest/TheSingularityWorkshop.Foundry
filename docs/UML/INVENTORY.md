# UML and Source Documentation Inventory

**Inventory date:** 2026-10-23  
**Branch:** development  
**Scope:** initial repository bootstrap.

## Source inventory

No implementation source files have been verified in this repository. The following entries are deliberately marked as not yet applicable rather than inventing classes or relationships.

| Source area | Source files | Companion Markdown | Neighborhood UML | Status |
|---|---:|---:|---:|---|
| Foundry runtime | 0 verified | N/A | N/A | Not implemented |
| Workflow definition/manifest | 0 verified | N/A | N/A | Contract undecided |
| Execution adapter | 0 verified | N/A | N/A | Integration not verified |
| CI runner integration | 0 verified | N/A | N/A | Not implemented |

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

Replace this initial inventory with a source-derived inventory when implementation begins. Every generated view should identify its source revision and evidence. A conceptual diagram must never be counted as source-derived coverage.