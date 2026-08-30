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

- [FileNameMode](Xml2Doc.Core.RendererOptions.md#xml2doc.core.rendereroptions.filenamemode) normalization → root namespace trimming → basename stripping.
- Slug generation uses [AnchorAlgorithm](Xml2Doc.Core.RendererOptions.md#xml2doc.core.rendereroptions.anchoralgorithm) and does not depend on file naming.
- Changing [AnchorAlgorithm](Xml2Doc.Core.RendererOptions.md#xml2doc.core.rendereroptions.anchoralgorithm) after publishing may invalidate inbound links.

<a id="xml2doc.core.rendereroptions.#ctor(xml2doc.core.filenamemode,string,string,bool,xml2doc.core.anchoralgorithm,string,string,bool,string,string,bool,bool,bool,system.nullable[int],bool,bool,string,xml2doc.core.lineendingstyle,system.action[string])"></a>

## Method: #ctor(FileNameMode, string, string, bool, AnchorAlgorithm, string, string, bool, string, string, bool, bool, bool, Nullable<int>, bool, bool, string, LineEndingStyle, Action<string>)

Preserves the constructor signature published before alias-provider injection was added.

<a id="xml2doc.core.rendereroptions.#ctor(xml2doc.core.filenamemode,string,string,bool,xml2doc.core.anchoralgorithm,string,string,bool,string,string,bool,bool,bool,system.nullable[int],bool,bool,string,xml2doc.core.lineendingstyle,system.action[string],xml2doc.core.aliasing.ialiasprovider)"></a>

## Method: #ctor(FileNameMode, string, string, bool, AnchorAlgorithm, string, string, bool, string, string, bool, bool, bool, Nullable<int>, bool, bool, string, LineEndingStyle, Action<string>, IAliasProvider)

Preserves the constructor signature published with alias-provider injection.

<a id="xml2doc.core.rendereroptions.#ctor(xml2doc.core.filenamemode,string,string,bool,xml2doc.core.anchoralgorithm,string,string,bool,string,string,bool,bool,bool,system.nullable[int],bool,bool,string,xml2doc.core.lineendingstyle,system.action[string],xml2doc.core.aliasing.ialiasprovider,xml2doc.core.anchoring.ianchorgenerator)"></a>

## Method: #ctor(FileNameMode, string, string, bool, AnchorAlgorithm, string, string, bool, string, string, bool, bool, bool, Nullable<int>, bool, bool, string, LineEndingStyle, Action<string>, IAliasProvider, IAnchorGenerator)

Preserves the constructor signature published with anchor-generator injection.

<a id="xml2doc.core.rendereroptions.#ctor(xml2doc.core.filenamemode,string,string,bool,xml2doc.core.anchoralgorithm,string,string,bool,string,string,bool,bool,bool,system.nullable[int],bool,bool,string,xml2doc.core.lineendingstyle,system.action[string],xml2doc.core.aliasing.ialiasprovider,xml2doc.core.anchoring.ianchorgenerator,xml2doc.core.templates.itemplaterenderer)"></a>

## Method: #ctor(FileNameMode, string, string, bool, AnchorAlgorithm, string, string, bool, string, string, bool, bool, bool, Nullable<int>, bool, bool, string, LineEndingStyle, Action<string>, IAliasProvider, IAnchorGenerator, ITemplateRenderer)

Preserves the constructor signature published with template-renderer injection.

<a id="xml2doc.core.rendereroptions.#ctor(xml2doc.core.filenamemode,string,string,bool,xml2doc.core.anchoralgorithm,string,string,bool,string,string,bool,bool,bool,system.nullable[int],bool,bool,string,xml2doc.core.lineendingstyle,system.action[string],xml2doc.core.aliasing.ialiasprovider,xml2doc.core.anchoring.ianchorgenerator,xml2doc.core.templates.itemplaterenderer,system.func[xml2doc.core.templates.templaterendercontext,system.collections.generic.ireadonlydictionary[string,object]])"></a>

## Method: #ctor(FileNameMode, string, string, bool, AnchorAlgorithm, string, string, bool, string, string, bool, bool, bool, Nullable<int>, bool, bool, string, LineEndingStyle, Action<string>, IAliasProvider, IAnchorGenerator, ITemplateRenderer, Func<TemplateRenderContext, IReadOnlyDictionary<string, object>>)

Preserves the constructor signature published with front-matter injection.

<a id="xml2doc.core.rendereroptions.#ctor(xml2doc.core.filenamemode,string,string,bool,xml2doc.core.anchoralgorithm,string,string,bool,string,string,bool,bool,bool,system.nullable[int],bool,bool,string,xml2doc.core.lineendingstyle,system.action[string],xml2doc.core.aliasing.ialiasprovider,xml2doc.core.anchoring.ianchorgenerator,xml2doc.core.templates.itemplaterenderer,system.func[xml2doc.core.templates.templaterendercontext,system.collections.generic.ireadonlydictionary[string,object]],xml2doc.core.autolinking.iautolinker)"></a>

## Method: #ctor(FileNameMode, string, string, bool, AnchorAlgorithm, string, string, bool, string, string, bool, bool, bool, Nullable<int>, bool, bool, string, LineEndingStyle, Action<string>, IAliasProvider, IAnchorGenerator, ITemplateRenderer, Func<TemplateRenderContext, IReadOnlyDictionary<string, object>>, IAutoLinker)

Preserves the constructor signature published with auto-linker injection.

<a id="xml2doc.core.rendereroptions.#ctor(xml2doc.core.filenamemode,string,string,bool,xml2doc.core.anchoralgorithm,string,string,bool,string,string,bool,bool,bool,system.nullable[int],bool,bool,string,xml2doc.core.lineendingstyle,system.action[string],xml2doc.core.aliasing.ialiasprovider,xml2doc.core.anchoring.ianchorgenerator,xml2doc.core.templates.itemplaterenderer,system.func[xml2doc.core.templates.templaterendercontext,system.collections.generic.ireadonlydictionary[string,object]],xml2doc.core.autolinking.iautolinker,xml2doc.core.linking.linkpolicy,xml2doc.core.linking.iexternalsymbolresolver)"></a>

## Method: #ctor(FileNameMode, string, string, bool, AnchorAlgorithm, string, string, bool, string, string, bool, bool, bool, Nullable<int>, bool, bool, string, LineEndingStyle, Action<string>, IAliasProvider, IAnchorGenerator, ITemplateRenderer, Func<TemplateRenderContext, IReadOnlyDictionary<string, object>>, IAutoLinker, LinkPolicy, IExternalSymbolResolver)

Preserves the constructor signature published with external cref resolution.

<a id="xml2doc.core.rendereroptions.#ctor(xml2doc.core.filenamemode,string,string,bool,xml2doc.core.anchoralgorithm,string,string,bool,string,string,bool,bool,bool,system.nullable[int],bool,bool,string,xml2doc.core.lineendingstyle,system.action[string],xml2doc.core.aliasing.ialiasprovider,xml2doc.core.anchoring.ianchorgenerator,xml2doc.core.templates.itemplaterenderer,system.func[xml2doc.core.templates.templaterendercontext,system.collections.generic.ireadonlydictionary[string,object]],xml2doc.core.autolinking.iautolinker,xml2doc.core.linking.linkpolicy,xml2doc.core.linking.iexternalsymbolresolver,xml2doc.core.signatures.signaturestyle,xml2doc.core.signatures.isignaturerenderer)"></a>

## Method: #ctor(FileNameMode, string, string, bool, AnchorAlgorithm, string, string, bool, string, string, bool, bool, bool, Nullable<int>, bool, bool, string, LineEndingStyle, Action<string>, IAliasProvider, IAnchorGenerator, ITemplateRenderer, Func<TemplateRenderContext, IReadOnlyDictionary<string, object>>, IAutoLinker, LinkPolicy, IExternalSymbolResolver, SignatureStyle, ISignatureRenderer)

Preserves the constructor signature published with signature rendering.

<a id="xml2doc.core.rendereroptions.#ctor(xml2doc.core.filenamemode,string,string,bool,xml2doc.core.anchoralgorithm,string,string,bool,string,string,bool,bool,bool,system.nullable[int],bool,bool,string,xml2doc.core.lineendingstyle,system.action[string],xml2doc.core.aliasing.ialiasprovider,xml2doc.core.anchoring.ianchorgenerator,xml2doc.core.templates.itemplaterenderer,system.func[xml2doc.core.templates.templaterendercontext,system.collections.generic.ireadonlydictionary[string,object]],xml2doc.core.autolinking.iautolinker,xml2doc.core.linking.linkpolicy,xml2doc.core.linking.iexternalsymbolresolver,xml2doc.core.signatures.signaturestyle,xml2doc.core.signatures.isignaturerenderer,xml2doc.core.diagnostics.idiagnosticsink)"></a>

## Method: #ctor(FileNameMode, string, string, bool, AnchorAlgorithm, string, string, bool, string, string, bool, bool, bool, Nullable<int>, bool, bool, string, LineEndingStyle, Action<string>, IAliasProvider, IAnchorGenerator, ITemplateRenderer, Func<TemplateRenderContext, IReadOnlyDictionary<string, object>>, IAutoLinker, LinkPolicy, IExternalSymbolResolver, SignatureStyle, ISignatureRenderer, IDiagnosticSink)

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

- [FileNameMode](Xml2Doc.Core.RendererOptions.md#xml2doc.core.rendereroptions.filenamemode) normalization → root namespace trimming → basename stripping.
- Slug generation uses [AnchorAlgorithm](Xml2Doc.Core.RendererOptions.md#xml2doc.core.rendereroptions.anchoralgorithm) and does not depend on file naming.
- Changing [AnchorAlgorithm](Xml2Doc.Core.RendererOptions.md#xml2doc.core.rendereroptions.anchoralgorithm) after publishing may invalidate inbound links.

**Parameters**

- `FileNameMode` — File naming strategy (see [FileNameMode](Xml2Doc.Core.FileNameMode.md)). Applied before namespace trimming and basename stripping.
- `RootNamespaceToTrim` — Optional namespace prefix removed from visible type headings and link labels (e.g. trimming `MyCompany.MyProduct` from `MyCompany.MyProduct.Feature.Widget` yields `Feature.Widget`). Does not alter underlying IDs.
- `CodeBlockLanguage` — Default fenced code block language (e.g. `csharp`, `xml`) used when no language is specified in source XML.
- `TrimRootNamespaceInFileNames` — When true, also trims `RootNamespaceToTrim` from generated file names after `FileNameMode` normalization. Ignored if `RootNamespaceToTrim` is `null` / empty.
- `AnchorAlgorithm` — Slug algorithm for headings (see [AnchorAlgorithm](Xml2Doc.Core.AnchorAlgorithm.md)). Changing this after publication alters fragment IDs.
- `TemplatePath` — Optional path to a wrapping template (e.g. Razor / token) applied around rendered body content; null = built‑in minimal layout.
- `FrontMatterPath` — Optional path to front‑matter (YAML / TOML / JSON) prepended verbatim to each output file (for SSG integration).
- `AutoLink` — When true, heuristically links unadorned type/member mentions in prose. Off by default to reduce false positives.
- `AliasMapPath` — Path to a JSON/text alias map adding custom type/namespace replacements beyond built‑in C# keyword aliases.
- `ExternalDocs` — Base URL for external documentation used for unresolved cref targets (e.g. framework APIs). Requires [PreferExternalForUnknown](Xml2Doc.Core.Linking.LinkPolicy.md#xml2doc.core.linking.linkpolicy.preferexternalforunknown).
- `EmitToc` — When true, emits a member table of contents per type in multi‑file mode (suppressed in single‑file mode).
- `EmitNamespaceIndex` — When true, generates a `namespaces.md` overview plus one page per namespace (multi‑file mode only).
- `BasenameOnly` — When true, file names drop namespace segments (after trimming if enabled), keeping only the final identifier.
- `ParallelDegree` — Maximum parallelism for per-type rendering. Values greater than one enable bounded parallel execution; `null` or values less than or equal to one preserve serial rendering. Custom rendering extensions must support concurrent calls when parallel rendering is enabled.
- `GenerateIndex` — When true, per-type output includes `index.md`. Disable this when multiple independent invocations intentionally share one output directory and index ownership is handled separately.
- `PruneStaleFiles` — When true, per-type rendering removes only stale files recorded by the same invocation manifest. Disabled by default.
- `ManifestIdentity` — Explicit stable invocation identity required when `PruneStaleFiles` is true.
- `LineEndings` — Line-ending policy for all rendered Markdown. Defaults to deterministic LF on every host.
- `WarningSink` — Optional callback invoked for non-fatal rendering warnings, including unresolved `<inheritdoc />` members.
- `AliasProvider` — Optional alias provider. When omitted, [DefaultAliasProvider](Xml2Doc.Core.Aliasing.DefaultAliasProvider.md) preserves the built-in C# keyword mappings.
- `AnchorGenerator` — Optional anchor generator. When omitted, the selected `AnchorAlgorithm` uses Xml2Doc's built-in implementation.
- `TemplateRenderer` — Optional programmatic template renderer. Cannot be combined with `TemplatePath` or `FrontMatterPath`.
- `FrontMatter` — Optional per-document metadata provider. Returned scalar values are serialized as deterministic YAML front matter. Cannot be combined with `FrontMatterPath`.
- `AutoLinker` — Optional free-text linker used when `AutoLink` is true.
- `LinkPolicy` — Controls whether unresolved cref targets retain the existing internal-link behavior or are offered to an external resolver.
- `ExternalSymbolResolver` — Optional provider for unresolved cref targets. When omitted and `ExternalDocs` is set, a [BaseUrlExternalSymbolResolver](Xml2Doc.Core.Linking.BaseUrlExternalSymbolResolver.md) is used.
- `SignatureStyle` — Optional controls for parameter names, generic constraints, and default values. Constraint output also uses documented generic parameter names in the signature. The default preserves existing signature output.
- `SignatureRenderer` — Optional signature and label renderer. When omitted, Xml2Doc uses [DefaultSignatureRenderer](Xml2Doc.Core.Signatures.DefaultSignatureRenderer.md).
- `DiagnosticSink` — Optional receiver for structured loading and rendering diagnostics.

<a id="xml2doc.core.rendereroptions.aliasmappath"></a>

## Property: AliasMapPath

Path to a JSON/text alias map adding custom type/namespace replacements beyond built‑in C# keyword aliases.

<a id="xml2doc.core.rendereroptions.aliasprovider"></a>

## Property: AliasProvider

Optional alias provider. When omitted, [DefaultAliasProvider](Xml2Doc.Core.Aliasing.DefaultAliasProvider.md) preserves the built-in C# keyword mappings.

<a id="xml2doc.core.rendereroptions.anchoralgorithm"></a>

## Property: AnchorAlgorithm

Slug algorithm for headings (see [AnchorAlgorithm](Xml2Doc.Core.AnchorAlgorithm.md)). Changing this after publication alters fragment IDs.

<a id="xml2doc.core.rendereroptions.anchorgenerator"></a>

## Property: AnchorGenerator

Optional anchor generator. When omitted, the selected `AnchorAlgorithm` uses Xml2Doc's built-in implementation.

<a id="xml2doc.core.rendereroptions.autolink"></a>

## Property: AutoLink

When true, heuristically links unadorned type/member mentions in prose. Off by default to reduce false positives.

<a id="xml2doc.core.rendereroptions.autolinker"></a>

## Property: AutoLinker

Optional free-text linker used when `AutoLink` is true.

<a id="xml2doc.core.rendereroptions.basenameonly"></a>

## Property: BasenameOnly

When true, file names drop namespace segments (after trimming if enabled), keeping only the final identifier.

<a id="xml2doc.core.rendereroptions.codeblocklanguage"></a>

## Property: CodeBlockLanguage

Default fenced code block language (e.g. `csharp`, `xml`) used when no language is specified in source XML.

<a id="xml2doc.core.rendereroptions.diagnosticsink"></a>

## Property: DiagnosticSink

Optional receiver for structured loading and rendering diagnostics.

<a id="xml2doc.core.rendereroptions.emitnamespaceindex"></a>

## Property: EmitNamespaceIndex

When true, generates a `namespaces.md` overview plus one page per namespace (multi‑file mode only).

<a id="xml2doc.core.rendereroptions.emittoc"></a>

## Property: EmitToc

When true, emits a member table of contents per type in multi‑file mode (suppressed in single‑file mode).

<a id="xml2doc.core.rendereroptions.externaldocs"></a>

## Property: ExternalDocs

Base URL for external documentation used for unresolved cref targets (e.g. framework APIs). Requires [PreferExternalForUnknown](Xml2Doc.Core.Linking.LinkPolicy.md#xml2doc.core.linking.linkpolicy.preferexternalforunknown).

<a id="xml2doc.core.rendereroptions.externalsymbolresolver"></a>

## Property: ExternalSymbolResolver

Optional provider for unresolved cref targets. When omitted and `ExternalDocs` is set, a [BaseUrlExternalSymbolResolver](Xml2Doc.Core.Linking.BaseUrlExternalSymbolResolver.md) is used.

<a id="xml2doc.core.rendereroptions.filenamemode"></a>

## Property: FileNameMode

File naming strategy (see [FileNameMode](Xml2Doc.Core.FileNameMode.md)). Applied before namespace trimming and basename stripping.

<a id="xml2doc.core.rendereroptions.frontmatter"></a>

## Property: FrontMatter

Optional per-document metadata provider. Returned scalar values are serialized as deterministic YAML front matter. Cannot be combined with `FrontMatterPath`.

<a id="xml2doc.core.rendereroptions.frontmatterpath"></a>

## Property: FrontMatterPath

Optional path to front‑matter (YAML / TOML / JSON) prepended verbatim to each output file (for SSG integration).

<a id="xml2doc.core.rendereroptions.generateindex"></a>

## Property: GenerateIndex

When true, per-type output includes `index.md`. Disable this when multiple independent invocations intentionally share one output directory and index ownership is handled separately.

<a id="xml2doc.core.rendereroptions.lineendings"></a>

## Property: LineEndings

Line-ending policy for all rendered Markdown. Defaults to deterministic LF on every host.

<a id="xml2doc.core.rendereroptions.linkpolicy"></a>

## Property: LinkPolicy

Controls whether unresolved cref targets retain the existing internal-link behavior or are offered to an external resolver.

<a id="xml2doc.core.rendereroptions.manifestidentity"></a>

## Property: ManifestIdentity

Explicit stable invocation identity required when `PruneStaleFiles` is true.

<a id="xml2doc.core.rendereroptions.paralleldegree"></a>

## Property: ParallelDegree

Maximum parallelism for per-type rendering. Values greater than one enable bounded parallel execution; `null` or values less than or equal to one preserve serial rendering. Custom rendering extensions must support concurrent calls when parallel rendering is enabled.

<a id="xml2doc.core.rendereroptions.prunestalefiles"></a>

## Property: PruneStaleFiles

When true, per-type rendering removes only stale files recorded by the same invocation manifest. Disabled by default.

<a id="xml2doc.core.rendereroptions.rootnamespacetotrim"></a>

## Property: RootNamespaceToTrim

Optional namespace prefix removed from visible type headings and link labels (e.g. trimming `MyCompany.MyProduct` from `MyCompany.MyProduct.Feature.Widget` yields `Feature.Widget`). Does not alter underlying IDs.

<a id="xml2doc.core.rendereroptions.signaturerenderer"></a>

## Property: SignatureRenderer

Optional signature and label renderer. When omitted, Xml2Doc uses [DefaultSignatureRenderer](Xml2Doc.Core.Signatures.DefaultSignatureRenderer.md).

<a id="xml2doc.core.rendereroptions.signaturestyle"></a>

## Property: SignatureStyle

Optional controls for parameter names, generic constraints, and default values. Constraint output also uses documented generic parameter names in the signature. The default preserves existing signature output.

<a id="xml2doc.core.rendereroptions.templatepath"></a>

## Property: TemplatePath

Optional path to a wrapping template (e.g. Razor / token) applied around rendered body content; null = built‑in minimal layout.

<a id="xml2doc.core.rendereroptions.templaterenderer"></a>

## Property: TemplateRenderer

Optional programmatic template renderer. Cannot be combined with `TemplatePath` or `FrontMatterPath`.

<a id="xml2doc.core.rendereroptions.trimrootnamespaceinfilenames"></a>

## Property: TrimRootNamespaceInFileNames

When true, also trims `RootNamespaceToTrim` from generated file names after `FileNameMode` normalization. Ignored if `RootNamespaceToTrim` is `null` / empty.

<a id="xml2doc.core.rendereroptions.warningsink"></a>

## Property: WarningSink

Optional callback invoked for non-fatal rendering warnings, including unresolved `<inheritdoc />` members.
