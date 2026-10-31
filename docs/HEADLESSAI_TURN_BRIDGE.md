<!-- The Singularity Workshop moniker -->
<p align="center"><a href="https://github.com/TrentBest"><img src="https://raw.githubusercontent.com/TrentBest/TheSingularityWorkshop.Visualize/development/docs/assets/moniker.svg" alt="The Singularity Workshop animated moniker" width="720" /></a></p>

# Foundry–HeadlessAi Turn Bridge

## Purpose

Foundry owns the governed task lifecycle. HeadlessAi owns one provider invocation and returns a normalized, provenance-bound proposal. The turn bridge connects those responsibilities without giving model output repository authority.

A turn may be requested only for a task that is in `Investigating` and has the exact active, unexpired Foundry lease. The request is bound to the task, repository, issue, isolated feature branch, base commit, candidate commit, prior turn, and any supplied CI evidence.

## Invoke one turn

The host creates the bridge with its persistent task coordinator, a concrete `IHeadlessAiAgentTurnClient` adapter, and a durable `JsonlHeadlessAiTurnJournal`:

```csharp
var turnJournal = new JsonlHeadlessAiTurnJournal(
    Path.Combine(dataDirectory, "headlessai-turns.jsonl"));

var turnCoordinator = new FoundryHeadlessAiTurnCoordinator(
    persistentTasks,
    headlessAiClient,
    turnJournal);

var result = await turnCoordinator.InvokeAsync(
    taskId,
    lease.LeaseId,
    isolatedBranch,
    candidateCommitSha,
    role,
    instructions,
    canonicalPrompt,
    responseSchema,
    evidence,
    priorTurnId,
    priorCandidateCommitSha,
    cancellationToken: cancellationToken);
```

The task must already be in `Investigating`. The branch must be an isolated Git-valid feature branch; `master` and `development` are rejected. The candidate SHA must be a full commit SHA. CI evidence must identify the same repository and current or prior candidate SHA. Resumable turns must provide both `PriorTurnId` and `PriorCandidateCommitSha`, matching HeadlessAi envelope validation.

## Implement the host adapter

Foundry intentionally has no compile-time dependency on the HeadlessAi assembly. The host implements `IHeadlessAiAgentTurnClient` and maps the Foundry request to HeadlessAi's `HeadlessAiAgentTurnEnvelope`.

| Foundry request | HeadlessAi envelope |
|---|---|
| `TaskId`, `CorrelationId` | Same identities; the host may use `TaskId` as the stable `ConversationId` and `foundry.work-task.v1` as `TaskVersion` |
| `Repository`, `AuthorizedBranch` | Same repository and authorized branch |
| `BaseCommitSha`, `CandidateCommitSha` | Same commit provenance |
| `IssueUrl` | `IssueUrl` |
| `Role`, `Instructions`, `CanonicalPrompt` | Same role and prompt fields |
| `ResponseSchema`, `ExpiresAt` | Same response schema and expiry |
| `PriorTurnId`, `PriorCandidateCommitSha`, `IdempotencyKey` | Same resumable-turn fields; prior turn ID and prior candidate SHA must be supplied together |
| `Evidence` | Map each item to a `HeadlessAiEvidenceReference` |\n| (No Foundry field) | Use default `HeadlessAiAgentTurnLimits` and an empty `ContextReferences` collection unless the host has separately bounded context references |

The adapter creates a `HeadlessAiAgentTurnEnvelope`, maps the evidence list, and invokes the configured `HeadlessAiAgentTurnExecutor.ExecuteAsync(envelope, cancellationToken)` once. The executor creates the immutable `HeadlessAiAgentTurnRequest` snapshot and returns a `HeadlessAiAgentTurnResult`. Validate the returned result, then map its task/candidate provenance, turn identity, request fingerprint, provider/model identity, timestamps, error/diagnostic, and normalized proposal fields—including `OperationalStatus`—into `FoundryHeadlessAiTurnResult`. Do not copy `RawResponse` into Foundry task state.

Use the host's configured `HeadlessAiAgentTemplate` and shared `HttpClient`. Provider credentials are resolved out of band by the host's request-header provider. Never put provider credentials, GitHub credentials, capability IDs, or authorization grants in the prompt, envelope, model response, or evidence summary.

## Durable invocation behavior

`JsonlHeadlessAiTurnJournal` records a pending intent and flushes it to disk before the provider call. After the returned result passes Foundry's provenance and bounds checks, the journal records the completed result.

- An exact duplicate of a completed invocation returns the recorded result without another provider call.
- Reuse of an idempotency key with different canonical invocation data is rejected.
- A cancellation already requested before dispatch is honored before the pending record is written. If cancellation, transport failure, or process loss occurs after reservation, a pending invocation with no completed result is treated as **indeterminate**. The provider may have received the request before the host stopped, so Foundry does not invoke it again automatically. The host must reconcile the provider outcome or escalate for an explicit decision.
- A failed journal write disables further use of that journal instance. Restart and inspect/recover the durable journal before dispatch resumes.

The JSONL journal is local and single-process. It does not provide distributed locking or cross-host coordination. Keep one journal instance per host process and protect its data directory with the host's normal filesystem access controls.

## Trust and authority

A normalized proposal is still untrusted model output. A diagnosis, patch intent, suggested status, or request for human approval does not change task state and does not grant a capability.

The bridge performs no repository reads or writes, branch creation, commit creation, CI dispatch, reruns, pull-request creation, merge, release, deployment, or package publication. The host must separately validate and authorize each requested action through Foundry's capability boundary, then use its trusted repository adapter to perform only the granted operation.

## Current verification and remaining work

The bridge contract, lease gate, provenance validation, durable completed-result replay, and indeterminate-call stop are covered by Foundry workflow tests. This proves the Foundry side of the boundary with a fake client; it does not yet prove a live provider call through a concrete host adapter.

The first live integration still requires:
1. A host implementation of `IHeadlessAiAgentTurnClient` using the configured HeadlessAi executor.
2. Host-side provider credential provisioning and a real provider invocation.
3. Trusted, isolated GitHub candidate-branch operations with capability checks.
4. Exact-candidate CiAi evidence retrieval, validation, and delivery into Foundry.
5. A bounded red-to-green repair cycle and a safe draft-PR handoff.

<!-- THE SINGULARITY WORKSHOP FOOTER -->
<p align="center"><img src="https://raw.githubusercontent.com/TrentBest/FSM_API/master/Documentation/Branding/TheSingularityWorkshop.png" alt="The Singularity Workshop" width="200" /></p>
