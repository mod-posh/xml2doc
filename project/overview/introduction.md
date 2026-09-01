# Introduction

Xml2Doc is a deterministic C# XML-documentation to Markdown generator with three public integration surfaces: Core, CLI, and MSBuild.

The repository and release target are `2.4.0`, focused on deterministic document metadata and output-layout extensibility.

## What Xml2Doc does

- Parses C# compiler XML documentation.
- Resolves XML documentation constructs including references and `<inheritdoc />`.
- Renders per-type or single-file Markdown.
- Provides stable anchors, links, signatures, aliases, templates, front matter, auto-linking, and external-documentation fallback.
- Emits structured diagnostics with stable `XML2DOC###` identifiers.
- Supports planning, dry run, diff, reports, incremental writes, deterministic parallel rendering, and invocation-scoped stale-output ownership.
- Aggregates multiple primary XML inputs deterministically through Core, repeated CLI `--xml`, or an MSBuild repository aggregation owner.
- Exposes immutable per-document identity and deterministic caller metadata to templates and front matter.
- Uses one authoritative plan for multi-document paths, links, reports, manifests, pruning, and writes.

## Public components

| Component | Current role |
| --- | --- |
| `Xml2Doc.Core` | Parsing, model construction, aggregation, inheritance/reference resolution, rendering, diagnostics, and runner pipeline. |
| `Xml2Doc.Cli` | Command-line and JSON configuration host over Core. |
| `Xml2Doc.MSBuild` | Project-build integration for normal single-project generation and opt-in repository aggregation. |

## Current release theme

`2.4.0` adds immutable document descriptors, deterministic caller metadata, and pluggable output paths while preserving flat paths and existing rendering behavior as compatible defaults.

See [`../../Xml2Doc.md`](../../Xml2Doc.md) for user documentation and [`../../docs/msbuild-repository-aggregation.md`](../../docs/msbuild-repository-aggregation.md) for the supported MSBuild owner pattern.
