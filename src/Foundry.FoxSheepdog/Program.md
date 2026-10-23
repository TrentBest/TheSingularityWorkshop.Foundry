# Program.cs — Fox/sheepdog CI execution

**Author:** Trent Best  
**Purpose:** Run a bounded, deterministic fox/sheepdog state scenario through the published FSM_API package, assert state transitions including a cross-FSM request, and emit a TSV trace plus an SVG view derived from the observed events.

## Class card

| Element | Contract |
|---|---|
| `Program` | Owns scenario setup, fixed tick inputs, assertions, evidence files, and scoped cleanup |
| `FoxContext` | FSM_API context for the fox state machine and its current position/tick |
| `DogContext` | FSM_API context for the sheepdog state machine and its current position/tick |
| `TraceEvent` | One observed state-entry or world-observation record, serialized as TSV |
| Inputs | Optional output directory argument; deterministic six-tick scenario |
| Outputs | `fox-sheepdog.trace` and `fox-sheepdog.svg` |
| Failure contract | Assertion or execution exception prints `FAIL` and returns exit code 1 |
| Dependency | Published `TheSingularityWorkshop.FSM_API` 1.0.13 |

## Behavior and evidence

The scenario uses actual FSM_API definitions and instances. The fox exercises Walking, Jumping, Fleeing, and Mangled; the dog exercises Sleeping, Awake, Chasing, and Mangling. The dog's Mangling enter callback requests the fox's Mangled state through the fox's FSM handle.

The sensor inputs are intentionally scripted so CI has a bounded, reproducible path without keyboard input or wall-clock sleeps. The SVG is generated from recorded state-entry events; it is a trace visualization, not a live GUI and not the original interactive browser playground.

## Run

```bash
dotnet run --project src/Foundry.FoxSheepdog/Foundry.FoxSheepdog.csproj -- artifacts
```

The CI workflow runs this scenario twice in separate processes, compares the TSV and SVG outputs byte-for-byte, and uploads the first run's evidence.
