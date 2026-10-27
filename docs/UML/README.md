<!-- The Singularity Workshop moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# Foundry UML and Source Documentation

**Status:** source-grounded UML policy and manual views exist for the initial probe and the retained workflow lifecycle coordinator. Automated generation remains absent.

## Principles

1. A diagram must represent source or runtime evidence, or be visibly labelled **Conceptual / Target**.
2. Each new C# source file should have a companion Markdown document explaining its purpose, contract, and relevant UML view.
3. Each significant type should have a separate `[TypeName]UML.md` neighborhood page in this directory: focus type centered, its users above, dependencies below, and relationships explicitly labelled.
4. Use cases describe user goals and are kept distinct from source structure.
5. Generated evidence should preserve stable symbol identity, source spans, provenance, and generation inputs.
6. Do not invent types, methods, dependencies, or runtime behavior to make a diagram look complete.

## Planned structure

- `docs/UML/README.md` — policy, coverage, and navigation.
- `docs/UML/INVENTORY.md` — source files, types, and view coverage once inventory is generated.
- `docs/UML/Standard/` — reusable diagram and evidence conventions as they mature.
- `docs/UML/[TypeName]UML.md` — type neighborhood diagrams grounded in actual declarations.

## View types

- Class/type neighborhoods and explicit dependency relationships.
- Sequence diagrams for real interaction paths, including failure and cancellation.
- Activity/control-flow diagrams for decision paths.
- State lifecycle diagrams for stateful objects.
- Use-case diagrams for user-visible goals.
- Architecture/component and deployment diagrams.
- Data models/lineage and requirement/test traceability.

Only add a view when there is sufficient evidence. Mark unimplemented flows as target designs.

## Current inventory

- [WorkflowExecutionCoordinator neighborhood](WorkflowExecutionCoordinatorUML.md) — retained run lifecycle and its relationship to the existing runner.
- [Coordinator source companion](../../src/Foundry.Workflow/WorkflowExecutionCoordinator.md) — purpose, public commands, limits, and tests.
- [FSM_API probe companion](../../src/Foundry.FsmApiProbe/Program.md) and [probe neighborhood](FoundryFsmApiProbeUML.md) — the original state-execution proof.

The workflow runtime views are maintained manually and are not generated output. The broader type inventory, source-span provenance, and stale-view detection remain future work.

## Validation expectations

The future CI check should detect missing required companion documents, stale generated views, broken internal links, and diagrams whose referenced source identity no longer exists. A generator or Roslyn integration is not present merely because this policy describes it.

---

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop avatar" width="200" /></p>
