# 2.3.1

Stabilization release addressing regressions and output-correctness defects affecting Xml2Doc 2.3.0.

## Release highlights

- Resolve bare `<inheritdoc />` through a unique conventional interface type when unrelated aggregate members expose the same signature.
- Remove configuration-scoped normal and aggregate Xml2Doc incremental state during `dotnet clean`.
- Preserve generated Markdown and incremental state belonging to other configurations during cleanup.
- Render structured XML documentation bullet lists as valid Markdown while preserving inline markup and paragraph boundaries.
- Preserve existing single-project, aggregation, configuration, and output contracts.

## Included issues

- #118 — Multi-input aggregation makes bare `<inheritdoc />` ambiguous across unrelated member signatures
- #119 — Xml2Doc.MSBuild incremental state survives `dotnet clean` and can leave generated Markdown stale
- #120 — Xml2Doc flattens XML documentation bullet lists into malformed Markdown

## BUG, AREA:CORE

* issue-118: Multi-input aggregation makes bare <inheritdoc /> ambiguous across unrelated member signatures

## NO LABEL

* issue-120: Xml2Doc flattens XML documentation bullet lists into malformed Markdown
* issue-119: Xml2Doc.MSBuild incremental state survives dotnet clean and can leave generated Markdown stale

