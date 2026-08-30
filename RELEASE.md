# 2.3.1 — Stabilization and Output Correctness

Correct regressions and output defects affecting the `2.3.0` multi-project aggregation release while preserving its public configuration and output contracts.

## Scope

- Resolve bare `<inheritdoc />` through a unique conventional interface type in aggregate inputs.
- Remove configuration-scoped normal and aggregate incremental state during `dotnet clean`.
- Preserve generated Markdown and other configurations' incremental state during cleanup.
- Render structured XML documentation bullet lists as valid Markdown lists.
- Preserve deterministic output and existing single-project behavior.

## Issues

- [x] #118 — Multi-input aggregation makes bare `<inheritdoc />` ambiguous across unrelated member signatures
- [x] #119 — Xml2Doc.MSBuild incremental state survives `dotnet clean` and can leave generated Markdown stale
- [x] #120 — Xml2Doc flattens XML documentation bullet lists into malformed Markdown

## Completion criteria

- Core and CLI aggregation regressions resolve unique conventional-interface inheritance without secondary missing-summary or unresolved-inheritance diagnostics.
- `dotnet clean` removes only Xml2Doc-owned derived state for the selected configuration.
- A post-clean build regenerates required state and preserves unchanged-build no-op behavior.
- Bullet lists preserve item boundaries, inline markup, surrounding paragraphs, and deterministic bytes.
- Packaged MSBuild integration and the full test suite pass.
- User, component, architecture, roadmap, changelog, and release documentation describe `2.3.1`.
- All GitHub Actions checks pass.

## Fixed

- #118: Bare aggregate `<inheritdoc />` resolution across unrelated same-signature members.
- #119: Configuration-scoped MSBuild clean lifecycle for normal and aggregate incremental state.
- #120: Structured XML documentation bullet-list rendering.
