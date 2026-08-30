# Project Roadmap

This roadmap summarizes Xml2Doc's evolution and the architectural themes delivered by each release line. For the active release checklist, see [`../TODO.md`](../TODO.md).

## Foundation — 1.0.0

Initial repository and architecture creation.

Key outcomes:

- Core rendering engine;
- CLI entry point;
- MSBuild integration;
- XML documentation to Markdown transformation;
- per-type documentation generation and index output.

Architectural themes:

- Core / CLI / MSBuild separation;
- deterministic Markdown as the primary output contract.

## Configuration and output modes — 1.1.x to 1.2.x

Key outcomes:

- shared `RendererOptions` model;
- single-file and per-type output modes;
- JSON CLI configuration;
- grouped member rendering;
- broader XML documentation tag support;
- snapshot regression coverage.

Architectural themes:

- host configuration parity;
- repeatable output and regression safety.

## Link, anchor, and signature hardening — 1.3.x

Key outcomes:

- stable member anchors;
- token-aware aliases;
- correct internal links across output modes;
- nested-generic formatting and label fixes;
- stronger anchor/link snapshot coverage.

Architectural themes:

- link stability as an output contract;
- deterministic signature formatting.

## Build and platform maturity — 1.4.x to 2.0.x

Key outcomes:

- Core targets `netstandard2.0`, `net8.0`, and `net9.0`;
- CLI targets `net8.0` and `net9.0`;
- MSBuild task targets `net472` and `net8.0` for Visual Studio and `dotnet` hosts;
- explicit LF/CRLF/native line-ending policy;
- portable output-ownership manifests and stale-output pruning;
- project-reference XML loading for `<inheritdoc />`;
- incremental output ledgers that recreate missing generated files;
- self-contained MSBuild task package layout.

Architectural themes:

- host portability;
- deterministic bytes across operating systems;
- invocation-scoped output ownership;
- safe incremental generation.

Related ADRs include generated-output ownership/lifecycle and multi-target compatibility decisions.

## Rendering extensibility — 2.1.0

Key outcomes:

- `IAnchorGenerator`;
- `IAliasProvider`;
- `ITemplateRenderer` and deterministic front matter;
- `IAutoLinker`;
- `IExternalSymbolResolver` and link policy;
- `ISignatureRenderer` and `SignatureStyle`;
- parity tests across built-in anchor algorithms and output modes.

Architectural themes:

- built-in and consumer-provided rendering services share the same pipeline;
- default output compatibility remains protected while extension points are replaceable.

## Diagnostics and runner pipeline — 2.2.0

Released August 17, 2026.

Key outcomes:

- stable structured diagnostics surfaced through Core, CLI, and MSBuild;
- a runner that coordinates planning, rendering, dry run, diff, reporting, and lifecycle operations;
- bounded parallel per-type rendering with deterministic output;
- incremental writes that skip unchanged files;
- reports that distinguish planned, written, skipped, pruned, and comparison results;
- full CLI exposure for applicable templates, front matter, auto-linking, alias maps, external docs, anchors, reports, parallelism, and lifecycle controls.

Architectural themes:

- one execution pipeline for mutating and non-mutating workflows;
- structured diagnostics as a stable CI contract;
- deterministic parallelism.

## Multi-project aggregation — 2.3.0

Released August 17, 2026.

Key outcomes:

- Core multi-input aggregation with canonical path ordering and deterministic duplicate-member failure;
- repeated CLI `--xml` inputs and JSON `XmlInputs`;
- one aggregate report with canonical `xmlInputs`;
- an opt-in MSBuild repository aggregation owner;
- automatic project-reference XML collection plus explicit `Xml2Doc_AggregateXml` inputs;
- separate aggregate incremental lifecycle files;
- `XML2DOC006` for duplicate primary member ownership;
- `XML2DOC007` for conflicting aggregate index ownership;
- Windows/Linux integration coverage proving parallel and `/m:1` builds produce identical file sets and bytes.

Architectural themes:

- one owner for one aggregate output set;
- aggregation occurs before rendering rather than merging independently generated Markdown;
- compatibility with existing single-project generation and `Xml2Doc_GenerateIndex=false` shared-directory mitigation.

See [`msbuild-repository-aggregation.md`](msbuild-repository-aggregation.md) for the supported MSBuild owner pattern.

## Stabilization and output correctness — 2.3.1

Release preparation is complete for a focused patch to the `2.3.0` aggregation and rendering line.

Key outcomes:

- bare `<inheritdoc />` in an aggregate resolves through a unique conventional interface type when unrelated members expose the same signature;
- normal and aggregate MSBuild incremental state is configuration-scoped and removed by `dotnet clean` without deleting generated Markdown;
- structured XML documentation bullet lists render as valid Markdown lists while preserving inline markup and paragraph boundaries;
- focused Core, CLI, renderer, and packaged MSBuild regressions protect the corrected behavior.

Architectural themes:

- contained corrections preserve the existing public aggregation and rendering contracts;
- generated documentation remains durable across `Clean`, while derived incremental state follows the normal MSBuild lifecycle;
- generated Markdown structure remains deterministic across repeated renders.

## Future work

Future capabilities should be represented by GitHub issues and milestones before they are added here. This keeps the roadmap tied to accepted scope rather than speculative feature lists.
