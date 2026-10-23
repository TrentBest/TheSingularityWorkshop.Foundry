# Program.cs — FSM_API CI probe

**Author moniker:** Trent Best

## Purpose

A deliberately small executable integration probe for Foundry's first CI experiment. It consumes the published `TheSingularityWorkshop.FSM_API` 1.0.13 package, creates a named two-state definition, verifies the initial state, steps the FSM through the public update API, and asserts the terminal state.

The `finally` block destroys only the probe-owned definition. It intentionally avoids `ResetAPI`, because the soft-reset behavior is known to remove definitions and is not a safe cleanup boundary for this experiment.

## UML class card

```text
Program
--------------------------------
+ Main() : int
- FsmName : string
- ProcessingGroup : string
--------------------------------
Creates definition and instance
Checks Ready -> Complete
Cleans up its own definition
```

See [FoundryFsmApiProbeUML.md](../../docs/UML/FoundryFsmApiProbeUML.md) for the manually maintained neighborhood view.

## Verification

- [CI workflow](../../.github/workflows/foundry-fsm-api-probe.yml)
- [Successful run](https://github.com/TrentBest/TheSingularityWorkshop.Foundry/actions/runs/40021885286)

This is an API probe, not a manifest-driven workflow engine. It does not prove failure/recovery semantics, manifest validation, or cross-repository orchestration.
