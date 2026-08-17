# ADR-009 — Structured Diagnostics

Status: Accepted

## Context

String-only warnings and exceptions are not sufficient for CI, CLI, and MSBuild consumers that need stable identifiers, severity, source/member context, and consistent failure behavior.

## Decision

Core emits structured diagnostics with stable `XML2DOC###` identifiers. Host integrations preserve diagnostic meaning while mapping it to their native surfaces:

- CLI writes stable diagnostic text to standard error and uses defined process exit codes.
- MSBuild maps warnings/errors through MSBuild logging.

Diagnostics cover unresolved references, duplicate anchors, malformed XML, missing summaries, unresolved inheritance, and deterministic aggregation ownership failures.

## Consequences

- Diagnostic IDs are part of the compatibility contract and should not be repurposed.
- New failure classes should receive new IDs rather than overloading existing meanings.
- Tests should validate both Core diagnostic identity and host mapping where applicable.

Implemented in the `2.2.0` diagnostics/pipeline release and extended by `2.3.0` aggregation diagnostics.
