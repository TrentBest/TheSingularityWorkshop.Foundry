# Foundry Architecture

**Status:** one real FSM_API integration probe passes in GitHub Actions; Foundry's workflow definition/runtime remains a target design.

## Purpose

Explore a reusable, inspectable way to define and execute stateful workflows so that CI scripts and host applications invoke the workflow rather than contain all of its business sequencing.

## Responsibility boundaries

| System | Intended responsibility | Current evidence |
|---|---|---|
| Foundry | Workflow definition/validation, execution coordination, result reporting | Thin .NET 8 probe exists; no workflow-manifest runtime yet |
| FSM_API | FSM mechanics when directly consumed | Published 1.0.13 package restored, built, and exercised by the passing probe; no reusable adapter yet |
| FSM_COS | Runtime composition when supported by its implementation | Source confirms composition boundary; Foundry integration not implemented |
| FSM_Serialization | Binary representation boundary for domain-owned values | Existing package contract inspected; no Foundry format selected |
| ProtocolAi | Potential protocol/contract interoperability | Deterministic identity vocabulary; Foundry integration is future work |
| GrammarAi | Potential language grammar/representation capabilities | Deterministic grammar structure; Foundry integration is future work |
| AnyApp / WebApp | Potential workflow hosts | AnyApp host path inspected; Foundry integration not verified |
| GitHub Actions | Trigger, runner, permissions, timeout, cancellation, job status | `.github/workflows/foundry-fsm-api-probe.yml` passes on `development`; output artifact uploaded |

## Verified first slice and next vertical slice

**Verified now:** the CI workflow restores and builds a .NET 8 console probe against the published FSM_API 1.0.13 package, creates a named FSM with `Ready` and `Complete` states, checks the initial state, executes the public update path, asserts the transition, performs scoped cleanup, and uploads a log artifact. See the [successful run](https://github.com/TrentBest/TheSingularityWorkshop.Foundry/actions/runs/40021885286).

**Not proven yet:** a versioned Foundry workflow definition, manifest validation, step-result model, failure/recovery semantics, invalid-definition rejection, cancellation/timeout behavior, or cross-repository orchestration.

Next, design the workflow contract from the inspected Workshop interfaces, then add success, invalid-input, failure/recovery, repeat-run, and artifact assertions. Keep FSM_COS as a composition layer, not the executor. Never use FSM_API's known-defective global reset as probe cleanup; the probe destroys only its own named definition.

The manifest format and runtime API remain open design decisions until the existing FSM_API, FSM_COS, and representation contracts have been inspected in sufficient depth. FSM_API's builder API is a concrete candidate for state mechanics; FSM_COS must remain a composition layer rather than being treated as the executor.

## Optional article publishing capability

Foundry may offer an opt-in article-production workflow for technical articles, release notes, tutorials, and implementation walkthroughs. This is an optional capability, not a required dependency of the core workflow runner.

A source-grounded publishing flow should:

1. Select an article outline and explicitly identified repository revision.
2. Gather claims and code excerpts from real files at that revision.
3. Preserve provenance for every excerpt: repository, commit/ref, path, and line range.
4. Generate readable code-excerpt images with syntax highlighting, strong contrast, legible font sizes, and wrapping/line-number policy.
5. Include equivalent selectable code text and accessible alt text; images supplement rather than replace text.
6. Build a preview and exportable article artifact before any external publication.
7. Publish only through an explicitly enabled destination adapter with separately supplied credentials and an explicit publish action.

The article content should distinguish source-verified facts, test evidence, and proposed interpretation. Generated excerpt images must be reproducible from the recorded source revision and rendering settings. A changed source revision should invalidate affected excerpts rather than silently retaining stale images.

### Publishing boundary

| Concern | Owner |
|---|---|
| Article outline, claim/evidence links, excerpt selection, build result | Foundry publishing workflow |
| Syntax-aware excerpt rendering and image artifact | Optional renderer capability |
| Destination-specific authentication and publishing API | Optional publisher adapter |
| Preview, review, approval, and final publish decision | User/host policy |
| Source of truth for code | Repository revision, not the generated image |

The core runner must still build and execute when publishing is disabled and without publishing credentials. No CMS, social platform, or remote publishing provider is selected yet. Do not add a provider SDK until a concrete destination is chosen.

## Safety boundaries

- Workflow input is data, not executable code.
- Do not evaluate arbitrary scripts or expressions from an untrusted manifest.
- Use least-privilege CI permissions and explicit timeouts.
- Do not expose secrets to untrusted pull-request execution.
- Publication and deployment remain separate actions requiring explicit approval.
- Article generation must not execute code snippets merely because they appear in an article.
- Do not publish repository content, secrets, or unreviewed claims automatically.

## Cross-language direction

C#, Java, Rust, and other languages may eventually be supported through explicit language adapters and stable data contracts. A shared definition alone does not make runtimes interoperable: each execution boundary needs a defined transport/call mechanism, data representation, error model, cancellation semantics, and security model. This is a future direction, not a current feature.

## Architectural decision rule

Prefer reusing Workshop technologies where their real interfaces fit. Introduce a new reusable runtime abstraction only when a working test demonstrates a capability that cannot reasonably live in a thin host/adapter. Avoid duplicating FSM_API or pretending that FSM_COS provides capabilities not verified in source.
