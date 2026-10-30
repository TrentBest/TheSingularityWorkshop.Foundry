# Governed Work Task Lifecycle

## Scope and status

The first task-lifecycle contract is implemented as an in-process reference coordinator. It is not yet durable, distributed, or connected to live GitHub/HeadlessAi/CiAi operations. GitHub remains the intended durable provenance and transport boundary; this implementation does not claim to provide that persistence.

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

## Current limitations

- State and event history exist only in process memory and are lost on restart.
- The coordinator does not claim GitHub issues, create branches, inspect CI, call models, or create PRs.
- Lease arbitration is atomic only within one coordinator instance; it is not a cross-process lock.
- `CodeRed` is a lifecycle state only. The full incident record, human notification/clear behavior, secure re-authentication, and durable incident history remain unimplemented.
- Authorization capabilities, exact-SHA CiAi evidence evaluation, duplicate/out-of-order callback reconciliation, and bounded model repair are future slices.

## Verification

See [WorkTaskCoordinatorTests](../tests/Foundry.Workflow.Tests/WorkTaskCoordinatorTests.cs) for envelope validation, duplicate task rejection, claim conflict, lease expiry/fencing, validated transitions, audit sequence, retry exhaustion, and handoff lease release. CI status is linked from the pull request implementing this slice.