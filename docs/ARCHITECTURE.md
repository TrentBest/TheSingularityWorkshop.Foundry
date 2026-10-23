# Foundry Architecture

**Status:** target architecture and experiment plan; implementation not yet established.

## Purpose

Explore a reusable, inspectable way to define and execute stateful workflows so that CI scripts and host applications invoke the workflow rather than contain all of its business sequencing.

## Responsibility boundaries

| System | Intended responsibility | Current evidence |
|---|---|---|
| Foundry | Workflow definition/validation, execution coordination, result reporting | Repository is bootstrap-only |
| FSM_API | FSM mechanics when directly consumed | Must verify actual package/API integration |
| FSM_COS | Runtime composition when supported by its implementation | Integration not verified |
| ProtocolAi | Potential protocol/contract interoperability | Future possibility only |
| GrammarAi | Potential language grammar/representation capabilities | Future possibility only |
| AnyApp / WebApp | Potential workflow hosts | Host integration not verified |
| GitHub Actions | Trigger, runner, permissions, timeout, cancellation, job status | Intended first execution environment |

## First vertical slice

1. Define a small, versioned workflow format.
2. Validate it before execution; reject unknown or malformed constructs.
3. Execute a deterministic state graph with explicit terminal outcomes.
4. Capture step-level outcomes and a machine-readable final status.
5. Make CI fail when the workflow fails, is invalid, or its expected artifacts are absent.
6. Test cancellation, timeouts, repeatability, and untrusted pull-request boundaries before expanding scope.

The manifest format and runtime API remain open design decisions until the existing FSM_API and FSM_COS contracts have been inspected.

## Safety boundaries

- Workflow input is data, not executable code.
- Do not evaluate arbitrary scripts or expressions from an untrusted manifest.
- Use least-privilege CI permissions and explicit timeouts.
- Do not expose secrets to untrusted pull-request execution.
- Publication and deployment remain separate actions requiring explicit approval.

## Cross-language direction

C#, Java, Rust, and other languages may eventually be supported through explicit language adapters and stable data contracts. A shared definition alone does not make runtimes interoperable: each execution boundary needs a defined transport/call mechanism, data representation, error model, cancellation semantics, and security model. This is a future direction, not a current feature.

## Architectural decision rule

Prefer reusing Workshop technologies where their real interfaces fit. Introduce a new reusable runtime abstraction only when a working test demonstrates a capability that cannot reasonably live in a thin host/adapter. Avoid duplicating FSM_API or pretending that FSM_COS provides capabilities not verified in source.