# Architecture Decisions

Canonical architecture decision records live under [`../../docs/adr/`](../../docs/adr/). This page summarizes the decisions most relevant to the current `2.4.0` architecture.

| ADR | Decision | Status |
| --- | --- | --- |
| ADR-001 | Scope and non-goals | Accepted |
| ADR-002 | Solution structure | Accepted |
| ADR-003 | Markdown output modes | Accepted |
| ADR-004 | Shared configuration model | Accepted |
| ADR-005 | Regression strategy | Accepted |
| ADR-006 | Link and anchor stability | Accepted |
| ADR-007 | MSBuild incremental generation | Accepted |
| ADR-008 | Multi-target compatibility | Accepted |
| ADR-009 | Structured diagnostics | Accepted |
| ADR-010 | Pluggable anchor algorithms | Accepted |
| ADR-011 | Generated output ownership, including repository aggregation ownership | Accepted |
| ADR-012 | Invocation-scoped manifest identity and storage | Accepted |
| ADR-013 | Deterministic Markdown line endings | Accepted |
| ADR-014 | Deterministic document metadata and host parity | Accepted |
| ADR-015 | Authoritative document paths and relative-link routing | Accepted |

## Current implications

- Core, CLI, and MSBuild remain separate components with Core owning rendering semantics.
- Both output modes are supported and covered by regression tests.
- Anchor/link behavior is a compatibility contract, even when alternate built-in/custom anchor generators are selected.
- Diagnostics use stable IDs and are surfaced through host-native output.
- MSBuild generation is incremental and validates recorded outputs before treating a build as up to date.
- Output deletion is authorized only by invocation-scoped ownership state.
- LF is the default deterministic line-ending policy.
- Multi-project repository documentation uses one explicit aggregate owner rather than racing independent index writers.
- Core owns deterministic document identity and caller-metadata composition across all hosts.
- One validated document plan governs multi-document paths, relative links, reports, manifests, pruning, and writes.

When this summary and an ADR differ, the ADR is authoritative and should be updated alongside the implementation decision.
