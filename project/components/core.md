# Core Component

`Xml2Doc.Core` owns XML documentation parsing, deterministic model construction, reference/inheritance resolution, diagnostics, Markdown rendering, aggregation, and the shared execution pipeline.

It targets `netstandard2.0`, `net8.0`, and `net9.0`.

## Model loading

The primary model type is `Xml2Doc.Core.Models.Xml2Doc`.

- `Xml2Doc.Load(path)` loads one primary compiler XML file.
- `Xml2Doc.LoadAggregate(paths)` loads multiple primary XML files as one deterministic model.
- `model.LoadReferences(paths)` adds reference-only XML used for inheritance/reference resolution without creating primary output pages.

Aggregate primary inputs are converted to full paths, de-duplicated with the platform path comparer, sorted deterministically, and then merged. Duplicate documentation-member ownership across primary inputs fails with `XML2DOC006`.

## Rendering

`MarkdownRenderer` renders the initialized model to either per-type files or one single file. `RendererOptions` controls output shape and built-in services.

Current replaceable rendering services include:

- `IAnchorGenerator`
- `IAliasProvider`
- `ITemplateRenderer`
- `IAutoLinker`
- `IExternalSymbolResolver`
- `ISignatureRenderer`
- front-matter callbacks and signature style options.

Built-in behavior and consumer-provided implementations run through the same rendering path.

`RendererOptions.Metadata` accepts generic scalar/list caller metadata. Core snapshots values at
renderer construction, exposes the immutable merged collection through
`TemplateRenderContext.Metadata`, and emits ordinally ordered YAML front matter. Programmatic
per-document values override generic caller keys; authoritative `documentId`, `documentKind`,
`namespace`, `symbol`, and `outputPath` values override both.

## Runner pipeline

`RendererRunner` coordinates output planning and execution around a renderer. It reports planned/written/skipped/pruned files and timing data, supports dry-run behavior, and is used by host integrations as the common execution boundary.

Per-type rendering supports bounded parallel execution while preserving deterministic file contents and ordering. Unchanged files are skipped rather than rewritten.

## Diagnostics

Core diagnostics use stable IDs (`XML2DOC001` through `XML2DOC006` in Core-owned behavior). Hosts preserve diagnostic meaning while mapping severity/message/location into their native output surfaces.

`XML2DOC007` is an MSBuild aggregation-ownership diagnostic rather than a Core parser/render diagnostic.

## Output lifecycle

Per-type stale pruning requires a stable manifest identity. Ownership manifests authorize deletion only for files previously recorded for that exact invocation identity. The lifecycle model is designed to remain safe across clean checkouts and moved repositories.

Generated Markdown uses LF by default for cross-platform byte determinism.
