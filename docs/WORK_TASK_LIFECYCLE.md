# Governed Work Task Lifecycle

## Scope and status

The task coordinator and CODE RED incident value contract are implemented as in-process reference components. They are not durable, distributed, or connected to live GitHub/HeadlessAi/CiAi operations. GitHub remains the intended durable provenance and transport boundary; these components do not claim to provide persistence.

## Task envelope

`WorkTaskRequest` is schema-versioned and binds a task to:
- a stable task ID and correlation ID;
- an owner/repository identity and GitHub issue reference;
- the full 40-character base commit SHA;
- a non-empty, repository-relative allowed-path scope;
- a bounded attempt count.

The issue is untrusted input. A valid task envelope is provenance and validation, not authorization to mutate a repository.

## Lifecycle

`WorkTaskCoordinator` currently validates these transitions:

```text
Queued -> Claimed -> Investigating -> Implementing -> Validating
                                      ^                    |
                                      |                    +-> ReviewReady -> HandedOff
                                      +-- RepairRequired <-+
```

`Blocked`, `CodeRed`, `Stopped`, and `Failed` are explicit states with constrained recovery/terminal transitions. An exhausted attempt budget converts a requested `RepairRequired` transition into `Blocked`.

## Claims and audit

Claims have an agent identity, unique lease fencing token, and bounded expiry. A live lease prevents a second claim. After expiry, another agent can claim; the prior lease token is rejected for all subsequent transitions. Every accepted transition appends a monotonically sequenced event containing prior/current status, agent, lease ID, timestamp, and reason. Snapshots expose immutable event collections.

## CODE RED incident record

`CodeRedIncident` is a versioned, credential-free operational record containing repository/task/issue/correlation identity, agent and role, current action, isolated branch and exact candidate SHA, latest CI run and attempt, severity/reason, retry budget, timestamps, and human-escalation requirement.

Updates preserve the incident start time and reject backward timestamps. Clearing is explicit, records a clear timestamp, and clears the human-escalation requirement. A cleared incident cannot be updated again. Canonical branches and partial CI run/attempt identities are rejected.

This is the incident data contract, not yet a durable incident service or notification dispatcher. No credential/token field exists in the record.

## Current limitations

- Task state, event history, and incident records are currently in process memory/value objects; they are lost unless a host persists them.
- The coordinator does not claim GitHub issues, create branches, inspect CI, call models, or create PRs.
- Lease arbitration is atomic only within one coordinator instance; it is not a cross-process lock.
- CODE RED human notification, durable incident history, secure re-authentication, and automatic status clear publication remain unimplemented.
- Authorization capabilities, exact-SHA CiAi evidence evaluation, duplicate/out-of-order callback reconciliation, and bounded model repair are future slices.

## Verification

See [WorkTaskCoordinatorTests](../tests/Foundry.Workflow.Tests/WorkTaskCoordinatorTests.cs) and [CodeRedIncidentTests](../tests/Foundry.Workflow.Tests/CodeRedIncidentTests.cs) for lifecycle, fencing, retry, incident provenance, CI identity, timestamp, and clear behavior. CI status is linked from the pull request implementing these slices.
