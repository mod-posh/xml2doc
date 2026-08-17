# Xml2Doc.Core

Core library for Xml2Doc. It parses C# compiler XML documentation, resolves references and inheritance, builds deterministic documentation models, and renders Markdown.

Version `2.3.0` adds deterministic multi-input aggregation while preserving the existing single-input API.

## Supported frameworks

- `netstandard2.0`
- `net8.0`
- `net9.0`

## Single-input example

```csharp
using Xml2Doc.Core;
using Xml2Doc.Core.Models;

var model = Xml2Doc.Core.Models.Xml2Doc.Load("MyLibrary.xml");
var renderer = new MarkdownRenderer(
    model,
    new RendererOptions
    {
        FileNameMode = FileNameMode.CleanGenerics,
        RootNamespaceToTrim = "MyCompany.MyProduct",
        TrimRootNamespaceInFileNames = true,
        LineEndings = LineEndingStyle.Lf
    });

renderer.RenderToDirectory("docs");
```

## Multi-input aggregation

```csharp
using Xml2Doc.Core;
using Xml2Doc.Core.Models;

var model = Xml2Doc.Core.Models.Xml2Doc.LoadAggregate(new[]
{
    "ProjectA.xml",
    "ProjectB.xml"
});

var renderer = new MarkdownRenderer(
    model,
    new RendererOptions
    {
        FileNameMode = FileNameMode.CleanGenerics,
        GenerateIndex = true,
        LineEndings = LineEndingStyle.Lf
    });

renderer.RenderToDirectory("docs");
```

`LoadAggregate` canonicalizes, de-duplicates, and ordinally sorts primary input paths before loading. If two primary XML files define the same documentation member ID, loading fails deterministically with `XML2DOC006` rather than selecting a winner based on caller order.

## Reference XML and inheritance

Reference-only XML can be loaded after the primary model:

```csharp
model.LoadReferences(new[]
{
    "Framework.Contracts.xml"
});
```

Reference members are available to `<inheritdoc />` and reference resolution but do not become primary generated pages.

## Renderer options

`RendererOptions` is a strongly typed record. Its public options are:

- `FileNameMode`
- `RootNamespaceToTrim`
- `CodeBlockLanguage`
- `TrimRootNamespaceInFileNames`
- `AnchorAlgorithm`
- `TemplatePath`
- `FrontMatterPath`
- `AutoLink`
- `AliasMapPath`
- `ExternalDocs`
- `EmitToc`
- `EmitNamespaceIndex`
- `BasenameOnly`
- `ParallelDegree`
- `GenerateIndex`
- `PruneStaleFiles`
- `ManifestIdentity`
- `LineEndings`
- `WarningSink`
- `AliasProvider`
- `AnchorGenerator`
- `TemplateRenderer`
- `FrontMatter`
- `AutoLinker`
- `LinkPolicy`
- `ExternalSymbolResolver`
- `SignatureStyle`
- `SignatureRenderer`
- `DiagnosticSink`

`FileNameMode`, `AnchorAlgorithm`, and `LineEndings` use the `FileNameMode`, `AnchorAlgorithm`, and `LineEndingStyle` enums respectively. Single-file output is selected through `MarkdownRenderer.RenderToSingleFile(...)` or `RendererRunMode.SingleFile`; it is not a `RendererOptions` property.

Built-in and consumer-provided rendering services use the same renderer pipeline.

## Runner pipeline

`RendererRunner` coordinates output planning and execution around an initialized `MarkdownRenderer`.

```csharp
using Xml2Doc.Core.Pipeline;

var runner = new RendererRunner(renderer);
var request = new RendererRunRequest(
    "docs",
    RendererRunMode.PerType);

var result = runner.Run(request);
```

The runner is the shared execution boundary used by host integrations for planning, deterministic rendering, incremental write results, pruning, and dry-run behavior.

## Determinism and output ownership

- Generated Markdown uses LF by default on every platform.
- Per-type rendering can run in parallel while preserving deterministic output.
- Unchanged files are skipped instead of rewritten.
- Stale pruning is per-type only and requires a stable `ManifestIdentity`.
- Ownership manifests are stored under `<output-root>/.xml2doc/manifests` and authorize deletion only for the matching invocation identity.

## Diagnostics

Core exposes stable diagnostics consumed by the CLI and MSBuild hosts. Aggregation-specific diagnostics are:

- `XML2DOC006` — duplicate documentation-member ownership across primary XML inputs.
- `XML2DOC007` is MSBuild-specific and is emitted by repository aggregation ownership validation.

See the repository-level [`Xml2Doc.md`](../../../Xml2Doc.md) for the complete diagnostic table and host usage examples.
