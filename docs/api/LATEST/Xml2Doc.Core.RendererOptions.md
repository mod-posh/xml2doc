# RendererOptions

Rendering options applied when converting XML documentation to Markdown.

**Remarks**

Example:

```csharp
            var opts = new RendererOptions(
                FileNameMode: FileNameMode.CleanGenerics,
                RootNamespaceToTrim: "MyCompany.MyProduct",
                CodeBlockLanguage: "csharp",
                TrimRootNamespaceInFileNames: true,
                AnchorAlgorithm: AnchorAlgorithm.Github,
                TemplatePath: "templates/type.md.tpl",
                FrontMatterPath: "templates/frontmatter.yml",
                AutoLink: true,
                AliasMapPath: "config/aliases.json",
                ExternalDocs: "https://learn.microsoft.com/dotnet/api/",
                EmitToc: true,
                EmitNamespaceIndex: true,
                BasenameOnly: false,
                ParallelDegree: Environment.ProcessorCount
            );
            
```

Ordering:

normalization → root namespace trimming → basename stripping. Slug generation uses and does not depend on file naming. Changing after publishing may invalidate inbound links.

<a id="xml2doc.core.rendereroptions.#ctor(xml2doc.core.filenamemode,string,string,bool,xml2doc.core.anchoralgorithm,string,string,bool,string,string,bool,bool,bool,system.nullable[int],bool,bool,string,xml2doc.core.lineendingstyle)"></a>

## Method: #ctor(FileNameMode, string, string, bool, AnchorAlgorithm, string, string, bool, string, string, bool, bool, bool, Nullable<int>, bool, bool, string, LineEndingStyle)

Rendering options applied when converting XML documentation to Markdown.

**Parameters**

- `FileNameMode` — File naming strategy (see [FileNameMode](Xml2Doc.Core.FileNameMode.md)). Applied before namespace trimming and basename stripping.
- `RootNamespaceToTrim` — Optional namespace prefix removed from visible type headings and link labels (e.g. trimming `MyCompany.MyProduct` from `MyCompany.MyProduct.Feature.Widget` yields `Feature.Widget`). Does not alter underlying IDs.
- `CodeBlockLanguage` — Default fenced code block language (e.g. `csharp`, `xml`) used when no language is specified in source XML.
- `TrimRootNamespaceInFileNames` — When true, also trims `RootNamespaceToTrim` from generated file names after `FileNameMode` normalization. Ignored if `RootNamespaceToTrim` is / empty.
- `AnchorAlgorithm` — Slug algorithm for headings (see [AnchorAlgorithm](Xml2Doc.Core.AnchorAlgorithm.md)). Changing this after publication alters fragment IDs.
- `TemplatePath` — Optional path to a wrapping template (e.g. Razor / token) applied around rendered body content; null = built‑in minimal layout.
- `FrontMatterPath` — Optional path to front‑matter (YAML / TOML / JSON) prepended verbatim to each output file (for SSG integration).
- `AutoLink` — When true, heuristically links unadorned type/member mentions in prose. Off by default to reduce false positives.
- `AliasMapPath` — Path to a JSON/text alias map adding custom type/namespace replacements beyond built‑in C# keyword aliases.
- `ExternalDocs` — Base URL (or map) for external documentation used for unresolved cref targets (e.g. framework APIs).
- `EmitToc` — When true, emits a member table of contents per type in multi‑file mode (suppressed in single‑file mode).
- `EmitNamespaceIndex` — When true, generates a `namespaces.md` overview plus one page per namespace (multi‑file mode only).
- `BasenameOnly` — When true, file names drop namespace segments (after trimming if enabled), keeping only the final identifier.
- `ParallelDegree` — Max parallelism for rendering; or <= 0 selects a heuristic (typically `Environment.ProcessorCount`).
- `GenerateIndex` — When true, per-type output includes `index.md`. Disable this when multiple independent invocations intentionally share one output directory and index ownership is handled separately.
- `PruneStaleFiles` — When true, per-type rendering removes only stale files recorded by the same invocation manifest. Disabled by default.
- `ManifestIdentity` — Explicit stable invocation identity required when `PruneStaleFiles` is true.
- `LineEndings` — Line-ending policy for all rendered Markdown. Defaults to deterministic LF on every host.

<a id="xml2doc.core.rendereroptions.aliasmappath"></a>

## Property: AliasMapPath

Path to a JSON/text alias map adding custom type/namespace replacements beyond built‑in C# keyword aliases.

<a id="xml2doc.core.rendereroptions.anchoralgorithm"></a>

## Property: AnchorAlgorithm

Slug algorithm for headings (see [AnchorAlgorithm](Xml2Doc.Core.AnchorAlgorithm.md)). Changing this after publication alters fragment IDs.

<a id="xml2doc.core.rendereroptions.autolink"></a>

## Property: AutoLink

When true, heuristically links unadorned type/member mentions in prose. Off by default to reduce false positives.

<a id="xml2doc.core.rendereroptions.basenameonly"></a>

## Property: BasenameOnly

When true, file names drop namespace segments (after trimming if enabled), keeping only the final identifier.

<a id="xml2doc.core.rendereroptions.codeblocklanguage"></a>

## Property: CodeBlockLanguage

Default fenced code block language (e.g. `csharp`, `xml`) used when no language is specified in source XML.

<a id="xml2doc.core.rendereroptions.emitnamespaceindex"></a>

## Property: EmitNamespaceIndex

When true, generates a `namespaces.md` overview plus one page per namespace (multi‑file mode only).

<a id="xml2doc.core.rendereroptions.emittoc"></a>

## Property: EmitToc

When true, emits a member table of contents per type in multi‑file mode (suppressed in single‑file mode).

<a id="xml2doc.core.rendereroptions.externaldocs"></a>

## Property: ExternalDocs

Base URL (or map) for external documentation used for unresolved cref targets (e.g. framework APIs).

<a id="xml2doc.core.rendereroptions.filenamemode"></a>

## Property: FileNameMode

File naming strategy (see [FileNameMode](Xml2Doc.Core.FileNameMode.md)). Applied before namespace trimming and basename stripping.

<a id="xml2doc.core.rendereroptions.frontmatterpath"></a>

## Property: FrontMatterPath

Optional path to front‑matter (YAML / TOML / JSON) prepended verbatim to each output file (for SSG integration).

<a id="xml2doc.core.rendereroptions.generateindex"></a>

## Property: GenerateIndex

When true, per-type output includes `index.md`. Disable this when multiple independent invocations intentionally share one output directory and index ownership is handled separately.

<a id="xml2doc.core.rendereroptions.lineendings"></a>

## Property: LineEndings

Line-ending policy for all rendered Markdown. Defaults to deterministic LF on every host.

<a id="xml2doc.core.rendereroptions.manifestidentity"></a>

## Property: ManifestIdentity

Explicit stable invocation identity required when `PruneStaleFiles` is true.

<a id="xml2doc.core.rendereroptions.paralleldegree"></a>

## Property: ParallelDegree

Max parallelism for rendering; or <= 0 selects a heuristic (typically `Environment.ProcessorCount`).

<a id="xml2doc.core.rendereroptions.prunestalefiles"></a>

## Property: PruneStaleFiles

When true, per-type rendering removes only stale files recorded by the same invocation manifest. Disabled by default.

<a id="xml2doc.core.rendereroptions.rootnamespacetotrim"></a>

## Property: RootNamespaceToTrim

Optional namespace prefix removed from visible type headings and link labels (e.g. trimming `MyCompany.MyProduct` from `MyCompany.MyProduct.Feature.Widget` yields `Feature.Widget`). Does not alter underlying IDs.

<a id="xml2doc.core.rendereroptions.templatepath"></a>

## Property: TemplatePath

Optional path to a wrapping template (e.g. Razor / token) applied around rendered body content; null = built‑in minimal layout.

<a id="xml2doc.core.rendereroptions.trimrootnamespaceinfilenames"></a>

## Property: TrimRootNamespaceInFileNames

When true, also trims `RootNamespaceToTrim` from generated file names after `FileNameMode` normalization. Ignored if `RootNamespaceToTrim` is / empty.
