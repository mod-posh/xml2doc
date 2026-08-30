# MSBuild Component

`Xml2Doc.MSBuild` integrates Core generation into project builds. It targets `net472` for Visual Studio/full-framework MSBuild and `net8.0` for `dotnet` SDK MSBuild.

## Normal per-project generation

For a project with `GenerateDocumentationFile=true`, package-owned targets load the compiler XML through `GenerateMarkdownFromXmlDoc`, compute incremental state, render Markdown, and record generated outputs.

The task delegates parsing and rendering semantics to Core. MSBuild owns host concerns such as evaluated properties, project-reference discovery, incremental target inputs/outputs, warnings/errors, and package task-host selection.

## Repository aggregation

Version `2.3.1` includes `GenerateMarkdownFromXmlDocs`, an opt-in aggregation target, and configuration-scoped cleanup of Xml2Doc incremental state. Aggregation is activated with:

```xml
<Xml2Doc_AggregateEnabled>true</Xml2Doc_AggregateEnabled>
```

One owner project collects primary XML from resolved `ProjectReference` outputs plus explicit `Xml2Doc_AggregateXml` items, then calls Core aggregation once. `Xml2Doc_ReferenceXml` remains reference-only.

The aggregation owner has separate stamp, fingerprint, ledger, and report state from normal per-project generation.

## Ownership validation

A repository aggregate should have one `index.md` owner. With project-reference participants, `Xml2Doc_AggregateValidateIndexOwnership` checks for referenced projects that are still enabled, use per-type output, generate an index, and target the same normalized output directory.

Conflicts fail with `XML2DOC007` before normal referenced-project builds proceed.

## Incremental behavior

Normal generation fingerprints compiler XML, reference XML contents, and significant renderer options.

Aggregate generation tracks primary/reference XML inputs, aggregate participation identities, significant renderer options, and the host newline token when `Xml2Doc_LineEndings=native`. Missing files in an output ledger invalidate the corresponding stamp so the next build recreates them.

## Package layout

The NuGet package includes task assemblies and runtime dependencies under `lib/<tfm>` and imports package-owned `.props`/`.targets` assets. Aggregation targets are packaged as an implementation asset and resolve the task assembly correctly from that nested package location.

See [`../../docs/msbuild-repository-aggregation.md`](../../docs/msbuild-repository-aggregation.md) for the supported owner/participant pattern.
