# Component View

## Core model layer

`Xml2Doc.Core.Models.Xml2Doc` is the primary documentation model.

- `Load` creates a model from one primary XML file.
- `LoadAggregate` creates one model from several primary XML files after canonical path normalization.
- `LoadReferences` adds reference-only members for inheritance/reference resolution.

The model keeps primary output ownership separate from reference-only symbols.

## Rendering layer

`MarkdownRenderer` converts the initialized model into per-type or single-file Markdown. `RendererOptions` configures built-in behavior and accepts replaceable services for anchors, aliases, templates, auto-linking, external symbols, signatures, and front matter.

Output planning and rendering share the same filename/link/anchor rules so planned files correspond to actual writes.

## Pipeline layer

`RendererRunner` coordinates planning and execution and returns structured run results. The runner is used for deterministic normal generation and dry-run-oriented workflows; hosts layer their own reporting/diff/status behavior around the shared result model.

Per-type execution may be parallelized within the configured bound while preserving deterministic output.

## Diagnostics layer

Core diagnostics carry stable IDs, severity, message, and optional context. CLI and MSBuild map these into console/MSBuild-native diagnostics.

## CLI layer

The CLI parses arguments/JSON, resolves primary input precedence, loads either one model or an aggregate model, configures renderer services, and translates results into reports and process exit codes.

## MSBuild layer

Normal MSBuild generation invokes `GenerateMarkdownFromXmlDoc`. Repository aggregation invokes `GenerateMarkdownFromXmlDocs` once with all primary inputs.

MSBuild targets own fingerprint/stamp/ledger orchestration, project-reference XML discovery, output-directory ownership validation, and package task assembly selection.
