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
   disable index generation unless they are the explicit repository aggregation owner.
3. A project-scoped invocation owns only the paths in its deterministic output plan. Existing files
   discovered in the output directory are not implicitly owned by that invocation.
4. Stale-output pruning uses an explicit, invocation-scoped manifest. It may delete only paths
   recorded by the same manifest identity, and only after successful generation.
5. Manifest and output replacement are atomic. Reports and dry runs list planned writes and
   deletions in ordinal order.
6. Full multi-input aggregation consumes all participating XML documentation in one logical Core
   operation and produces one canonically ordered index. Concurrent last-writer-wins merging is not
   a supported aggregation strategy.
7. MSBuild repository aggregation has one explicit owner. The owner gathers XML from project
   references and explicit `Xml2Doc_AggregateXml` items, then calls Core aggregation once after the
   repository build. Referenced Xml2Doc projects that still claim the same `index.md` fail validation
   with `XML2DOC007` before normal project-reference builds begin.

## Consequences

- Existing single-project behavior is unchanged.
- Shared-directory consumers can keep the safe `GenerateIndex=false` mitigation or move to native
  repository aggregation with one owner.
- Parallel and serial project scheduling cannot change aggregate content because input ordering and
  rendering order are canonicalized before output is written.
- Host configuration remains a projection of Core behavior rather than host-specific rendering.
- Safe stale-file cleanup remains scoped to explicit ownership and does not treat hand-authored or
  other-project files as implicitly owned.
