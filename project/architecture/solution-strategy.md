# Solution Strategy

Xml2Doc keeps one rendering implementation in Core and thin host integrations around it.

## Core-first design

Core is responsible for:

- parsing primary and reference XML documentation;
- deterministic single-input and multi-input model construction;
- inheritance/reference resolution;
- structured diagnostics;
- rendering services and Markdown generation;
- output planning, deterministic parallel rendering, and lifecycle-aware execution.

CLI and MSBuild translate host configuration into this shared behavior instead of reimplementing rendering rules.

## Explicit aggregation before rendering

For multi-project documentation, aggregation happens at the model boundary, not by merging independently generated Markdown. Primary XML inputs are canonicalized and loaded into one model, then one renderer owns the combined output set.

MSBuild makes repository ownership explicit through `Xml2Doc_AggregateEnabled=true`. This avoids nondeterministic shared-index writes while preserving normal project generation as the default.

## Determinism as an architectural constraint

Stable links, anchors, ordering, line endings, file naming, and diagnostics are regression-protected contracts. Bounded parallelism is allowed only where it preserves byte-equivalent output.

## Lifecycle safety

Output ownership is invocation-scoped. Stale pruning is authorized by persisted manifest identity, and incremental MSBuild targets validate recorded outputs before deciding generation is up to date.

## Release evolution

- `2.0.x` hardened hosting, line endings, ownership, inheritance, and package layout.
- `2.1.0` introduced replaceable rendering services.
- `2.2.0` completed structured diagnostics and the runner/pipeline model.
- `2.3.0` adds deterministic multi-project aggregation across Core, CLI, and MSBuild.

Canonical architecture decisions are maintained under [`../../docs/adr/`](../../docs/adr/).
