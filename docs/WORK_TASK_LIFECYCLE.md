# Governed Work Task Lifecycle

## Scope and status

The task coordinator and CODE RED incident value contract are in-process reference components. `JsonlWorkTaskStore` adds an append-only, fsync-backed local journal of complete task snapshots with strict replay validation. `PersistentWorkTaskCoordinator` restores journaled tasks at startup and persists each accepted submit/claim/transition before returning it to the caller. `WorkTaskCoordinator.Restore` rehydrates validated snapshots while preserving event sequence and lease identity so an expired lease can be reclaimed with a new fencing token. This is durable across process restarts on the same host, but is not a distributed store, GitHub-backed store, or connected live GitHub/HeadlessAi/CiAi integration.

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


## Host-issued action capabilities

`WorkTaskCapabilityRegistry` issues opaque, short-lived capabilities only when the host explicitly calls `Grant`. Each grant requires a live task lease and is bound to the task/repository, owning agent and fencing token, isolated branch, base SHA, exact candidate SHA, a subset of the task's allowed paths, and an explicit action allowlist. File reads/writes require a path inside that scope; commit/CI/branch/draft-PR operations require their own named action. Grants expire within one hour and can be revoked immediately.

The registry is an in-process host boundary, not a security sandbox. The host must keep the registry and capability IDs out of model context, pass only the minimum capability to the operation adapter, and reissue after restart only after revalidating task and policy. Merge, release, deployment, package publication, and arbitrary shell/process execution are not grantable actions.

## CODE RED incident record

`CodeRedIncident` is a versioned, credential-free operational record containing repository/task/issue/correlation identity, agent and role, current action, isolated branch and exact candidate SHA, latest CI run and attempt, severity/reason, retry budget, timestamps, and human-escalation requirement. `JsonlCodeRedIncidentStore` persists every create/update/clear snapshot in an append-only, fsync-backed journal, validates immutable incident identity and monotonic history on replay, and rejects mutation after clear.

Updates preserve the incident start time and reject backward timestamps. Clearing is explicit, records a clear timestamp, and clears the human-escalation requirement. A cleared incident cannot be updated again. Canonical branches and partial CI run/attempt identities are rejected.

This is the incident data contract, not yet a durable incident service or notification dispatcher. No credential/token field exists in the record.

## Current limitations

- `JsonlWorkTaskStore` and `PersistentWorkTaskCoordinator` persist task snapshots and event history locally. The adapter serializes its own operations and restores journal state before accepting new work. If a journal write fails after an in-memory mutation, it permanently fails closed for that process; restart and restore from the journal before dispatch resumes. This remains single-process only: separate adapter/store instances or processes are not coordinated, and this is not a distributed atomic claim.
- CODE RED incident records have their own append-only local journal; this is single-process storage and is not yet a GitHub-backed or distributed incident service.
- The coordinator does not claim GitHub issues, create branches, inspect CI, call models, or create PRs.
- Lease arbitration is atomic only within one coordinator instance; it is not a cross-process lock. The local journal does not turn that lease into a distributed claim.
- CODE RED human notification, secure re-authentication, and automatic status clear publication remain unimplemented.
- Exact-SHA CiAi evidence evaluation, duplicate/out-of-order callback reconciliation, secure re-authentication, bounded model repair, and draft-PR handoff are future slices. Host-issued action capabilities are implemented as an in-memory, short-lived authorization boundary.

## Verification

See [WorkTaskCoordinatorTests](../tests/Foundry.Workflow.Tests/WorkTaskCoordinatorTests.cs), [JsonlWorkTaskStoreTests](../tests/Foundry.Workflow.Tests/JsonlWorkTaskStoreTests.cs) (including persistent-adapter restart and fail-closed tests), [CodeRedIncidentTests](../tests/Foundry.Workflow.Tests/CodeRedIncidentTests.cs), and [JsonlCodeRedIncidentStoreTests](../tests/Foundry.Workflow.Tests/JsonlCodeRedIncidentStoreTests.cs), and [WorkTaskCapabilityRegistryTests](../tests/Foundry.Workflow.Tests/WorkTaskCapabilityRegistryTests.cs) for lifecycle, fencing, retry, incident provenance, CI identity, timestamp, clear behavior, and capability scope/lease/expiry/revocation. CI status is linked from the pull request implementing these slices.
