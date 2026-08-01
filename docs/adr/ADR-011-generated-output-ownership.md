# ADR‑011 — Generated Output Ownership and Lifecycle

## Status

Accepted

## Context

Per-type rendering writes type pages and `index.md` into one output directory. When independent
CLI or MSBuild invocations share that directory, each invocation has only its own XML model and
cannot deterministically aggregate the other projects' types. Concurrent builds therefore race to
replace `index.md`, and project-scoped cleanup cannot safely infer ownership of neighboring files.

Generated Markdown is a public output contract. Index ownership and deletion boundaries must be
explicit before shared-directory aggregation or stale-output pruning can be considered safe.

## Decision

1. Core owns whether per-type rendering generates `index.md` through
   `RendererOptions.GenerateIndex`. The default remains `true` for backward compatibility.
2. CLI and MSBuild expose the same option. Independent invocations sharing an output directory must
   disable index generation and delegate the repository-level index to a separate aggregation step.
3. A project-scoped invocation owns only the paths in its deterministic output plan. Existing files
   discovered in the output directory are not implicitly owned by that invocation.
4. Future stale-output pruning must use an explicit, invocation-scoped manifest. It may delete only
   paths recorded by the same manifest identity, and only after successful generation.
5. Manifest and output replacement must be atomic. Reports and dry runs must list planned writes and
   deletions in ordinal order.
6. Full multi-input aggregation is a separate capability. It must consume all inputs in one logical
   operation and produce one canonically ordered index; concurrent last-writer-wins merging is not a
   supported aggregation strategy.

## Consequences

- Existing single-project behavior is unchanged.
- Shared-directory consumers gain a deterministic configuration that prevents index corruption, but
  must provide their own aggregation step when a combined index is required.
- Host configuration remains a projection of Core behavior rather than host-specific rendering.
- Safe stale-file cleanup can be implemented without treating hand-authored or other-project files as
  Xml2Doc-owned content.
