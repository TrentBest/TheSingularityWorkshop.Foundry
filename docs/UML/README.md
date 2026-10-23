# Foundry UML and Source Documentation

**Status:** policy established; the first C# probe source exists. Companion documentation and its source-grounded neighborhood are now recorded manually; automated generation remains absent.

## Principles

1. A diagram must represent source or runtime evidence, or be visibly labelled **Conceptual / Target**.
2. Each C# source file should have a companion Markdown document explaining the author's moniker, the file's purpose, and a UML class card where applicable.
3. Each significant type should have a separate `[TypeName]UML.md` neighborhood page in this directory: focus type centered, its users above, dependencies below, and relationships explicitly labelled.
4. Use cases describe user goals and are kept distinct from source structure.
5. Generated evidence should preserve stable symbol identity, source spans, provenance, and generation inputs.
6. Do not invent types, methods, dependencies, or runtime behavior to make a diagram look complete.

## Planned structure

- `docs/UML/README.md` — policy, coverage, and navigation.
- `docs/UML/INVENTORY.md` — source files, types, and view coverage once source exists.
- `docs/UML/Standard/` — reusable diagram and evidence conventions as they mature.
- `docs/UML/[TypeName]UML.md` — type neighborhood diagrams grounded in actual declarations.

## View types

- Class/type neighborhoods and explicit dependency relationships.
- Sequence diagrams for real interaction paths, including failure and rollback.
- Activity/control-flow diagrams for decision paths.
- State lifecycle diagrams for stateful objects.
- Use-case diagrams for user-visible goals.
- Architecture/component and deployment diagrams.
- Data models/lineage and requirement/test traceability.

Only add a view when there is sufficient evidence. Mark unimplemented flows as target designs.

## Current inventory

The first C# source is `src/Foundry.FsmApiProbe/Program.cs`. See its [companion note](../../src/Foundry.FsmApiProbe/Program.md) and [Program neighborhood](FoundryFsmApiProbeUML.md). The diagram is a manually maintained source-grounded view, not generated output. The workflow manifest/runtime has no source types yet.

## Validation expectations

The future CI check should detect missing required companion documents, stale generated views, broken internal links, and diagrams whose referenced source identity no longer exists. A generator or Roslyn integration is not present merely because this policy describes it.