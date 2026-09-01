# MarkdownRenderer

Renders a parsed XML documentation model to Markdown (multi‑file or single‑file).

**Remarks**

Core capabilities:

- Multi‑file output via [RenderToDirectory(string)](Xml2Doc.Core.MarkdownRenderer.md#xml2doc.core.markdownrenderer.rendertodirectory(string)) (one file per type + `index.md`).
- Single consolidated file via [RenderToSingleFile(string)](Xml2Doc.Core.MarkdownRenderer.md#xml2doc.core.markdownrenderer.rendertosinglefile(string)) (index followed by all types).
- Overload grouping (method overloads share one heading, individual signatures listed as bullets).
- `<inheritdoc>` resolution / merge through [InheritDocResolver](Xml2Doc.Core.InheritDocResolver.md).
- Stable anchors for member sections ([IdToAnchor(string)](Xml2Doc.Core.MarkdownRenderer.md#xml2doc.core.markdownrenderer.idtoanchor(string))) and heading slugs ([HeadingSlug(string)](Xml2Doc.Core.MarkdownRenderer.md#xml2doc.core.markdownrenderer.headingslug(string))).
- Depth‑aware generic signature formatting with alias substitution (framework types → C# keywords).
- Paragraph‑preserving XML → Markdown normalization (code blocks kept verbatim; soft wraps collapsed).
- Optional root namespace trimming and filename transformations ([RendererOptions](Xml2Doc.Core.RendererOptions.md)).
- Optional per‑type member TOC ([EmitToc](Xml2Doc.Core.RendererOptions.md#xml2doc.core.rendereroptions.emittoc)).
- Optional namespace index pages ([EmitNamespaceIndex](Xml2Doc.Core.RendererOptions.md#xml2doc.core.rendereroptions.emitnamespaceindex)).
- Deterministic planning of outputs without writing via [PlanOutputs(string, string)](Xml2Doc.Core.MarkdownRenderer.md#xml2doc.core.markdownrenderer.planoutputs(string,string)) (used for dry‑run / reporting).
- Selectable slug algorithm ([AnchorAlgorithm](Xml2Doc.Core.RendererOptions.md#xml2doc.core.rendereroptions.anchoralgorithm)): Default / GitHub / Kramdown / Gfm.

Anchor algorithm summary:

- Default: lowercase, whitespace → dash, strip non `[a-z0-9-]`, collapse multi‑dash runs.
- GitHub/Gfm: Unicode normalization + diacritic removal; drop punctuation; whitespace → dash; trim dashes.
- Kramdown: Similar to GitHub but retains underscores; punctuation removed; whitespace → dash.

Public rendering methods allow I/O exceptions to surface (no catch/ swallow beyond outer `Main` typical usage).

<a id="xml2doc.core.markdownrenderer.#ctor(xml2doc.core.models.xml2doc,xml2doc.core.rendereroptions)"></a>

## Method: #ctor(Xml2Doc, RendererOptions)

Creates a renderer for a parsed XML documentation model.

**Parameters**

- `model` — Parsed XML documentation model (never null).
- `options` — Optional rendering options; defaults applied when null.

<a id="xml2doc.core.markdownrenderer.buildmembertoc(system.collections.generic.ienumerable[xml2doc.core.models.xmember])"></a>

## Method: BuildMemberToc(IEnumerable<XMember>)

Builds a member table of contents (overload groups collapsed to first anchor).

<a id="xml2doc.core.markdownrenderer.buildsinglefilecontent(string)"></a>

## Method: BuildSingleFileContent(string)

Builds single‑file content, temporarily switching link mode to in‑document anchors.

**Returns**

Markdown string containing index + all types.

<a id="xml2doc.core.markdownrenderer.creftomarkdown(system.text.stringbuilder,string,string)"></a>

## Method: CrefToMarkdown(StringBuilder, string, string)

Appends a Markdown link for a cref to a [StringBuilder](System.Text.StringBuilder.md) using the configured resolver.

<a id="xml2doc.core.markdownrenderer.creftomarkdown(string,string)"></a>

## Method: CrefToMarkdown(string, string)

Returns a Markdown link for a cref value (type or member).

<a id="xml2doc.core.markdownrenderer.documentplan"></a>

## Property: DocumentPlan

Gets the immutable authoritative multi-document path plan.

<a id="xml2doc.core.markdownrenderer.filenamefor(string,xml2doc.core.filenamemode)"></a>

## Method: FileNameFor(string, FileNameMode)

Basic filename builder (mode only; no root namespace trimming).

<a id="xml2doc.core.markdownrenderer.filenameforpertype(string)"></a>

## Method: FileNameForPerType(string)

Per‑type filename generator applying mode + optional root namespace trimming + optional basename stripping.

**Remarks**

Basename stripping applied only when [BasenameOnly](Xml2Doc.Core.RendererOptions.md#xml2doc.core.rendereroptions.basenameonly) is true.

<a id="xml2doc.core.markdownrenderer.gettypes"></a>

## Method: GetTypes

Enumerates all documented types (`T:` members only).

<a id="xml2doc.core.markdownrenderer.headingslug(string)"></a>

## Method: HeadingSlug(string)

Resolves a heading slug using the configured [AnchorAlgorithm](Xml2Doc.Core.RendererOptions.md#xml2doc.core.rendereroptions.anchoralgorithm).

**Parameters**

- `heading` — Raw heading text.

**Returns**

Algorithm-specific slug string.

<a id="xml2doc.core.markdownrenderer.idtoanchor(string)"></a>

## Method: IdToAnchor(string)

Converts a documentation ID to a stable anchor (lowercase; generic braces → square brackets; aliases applied).

<a id="xml2doc.core.markdownrenderer.normalizexmltomarkdown(system.xml.linq.xelement,bool,bool,string)"></a>

## Method: NormalizeXmlToMarkdown(XElement, bool, bool, string)

Normalizes an XML documentation element (summary, remarks, example, param, see, code) to Markdown with paragraph preservation.

**Parameters**

- `element` — XML element or null.
- `preferCodeBlocks` — True to prefer fenced blocks for multi‑line code/examples.
- `preserveListItemMarkers` — Preserves internal bullet-list continuation markers.
- `currentDocumentId` — Current planned document identity for relative links.

**Returns**

Markdown string (empty if element is null).

<a id="xml2doc.core.markdownrenderer.planoutputs(string,string)"></a>

## Method: PlanOutputs(string, string)

Computes the exact list of files this renderer would write for the current options (no disk I/O).

**Remarks**

Multi‑file mode includes `index.md` when [GenerateIndex](Xml2Doc.Core.RendererOptions.md#xml2doc.core.rendereroptions.generateindex) is true. Namespace index emission adds `namespaces.md` and one page per namespace.

**Parameters**

- `outDir` — Destination directory (may not exist).
- `singleFilePath` — If non-null, plans single-file output; otherwise multi‑file.

**Returns**

Absolute paths of files that would be produced.

<a id="xml2doc.core.markdownrenderer.renderindex(system.collections.generic.ienumerable[xml2doc.core.models.xmember],bool,string)"></a>

## Method: RenderIndex(IEnumerable<XMember>, bool, string)

Builds a type index linking either to per‑type files or heading anchors (single‑file mode).

**Parameters**

- `types` — Sequence of type members.
- `useAnchors` — True to link to in‑document anchors; false for per‑type files.
- `currentDocumentId` — Current planned document identity for relative links.

<a id="xml2doc.core.markdownrenderer.rendermember(xml2doc.core.models.xmember,system.text.stringbuilder,string,bool)"></a>

## Method: RenderMember(XMember, StringBuilder, string, bool)

Renders a member (or overload bullet) including summary, parameters, returns, exceptions, examples, see‑also links, and a stable anchor.

**Parameters**

- `m` — Member to render.
- `sb` — Destination builder.
- `currentDocumentId` — Current planned document identity for relative links.
- `asOverload` — True to render as a bullet under an overload group; false for a full section.

<a id="xml2doc.core.markdownrenderer.rendertodirectory(string)"></a>

## Method: RenderToDirectory(string)

Emits one Markdown file per documented type and, by default, an `index.md`. Optionally emits namespace index pages.

**Remarks**

Overwrites existing files. Per‑type links point to sibling files; member links point to in‑file anchors. Respects [FileNameMode](Xml2Doc.Core.RendererOptions.md#xml2doc.core.rendereroptions.filenamemode) and [TrimRootNamespaceInFileNames](Xml2Doc.Core.RendererOptions.md#xml2doc.core.rendereroptions.trimrootnamespaceinfilenames). Namespace index emission ([EmitNamespaceIndex](Xml2Doc.Core.RendererOptions.md#xml2doc.core.rendereroptions.emitnamespaceindex)) adds:

- `namespaces.md` — overview of all namespaces.
- `namespaces/<namespace>.md` — per‑namespace type listing.

**Parameters**

- `outDir` — Destination directory (created if absent).

**Exceptions**

- [IOException](System.IO.IOException.md) — Error writing one or more output files.
- [UnauthorizedAccessException](System.UnauthorizedAccessException.md) — Insufficient permissions for the target directory.

<a id="xml2doc.core.markdownrenderer.rendertosinglefile(string)"></a>

## Method: RenderToSingleFile(string)

Emits a single Markdown file (index + all types + their members).

**Remarks**

Type links become heading slugs; member links use explicit anchors from [IdToAnchor(string)](Xml2Doc.Core.MarkdownRenderer.md#xml2doc.core.markdownrenderer.idtoanchor(string)).

**Parameters**

- `outPath` — Output file path (parent directory created if needed).

**Exceptions**

- [IOException](System.IO.IOException.md) — Error writing the output file.
- [UnauthorizedAccessException](System.UnauthorizedAccessException.md) — Insufficient permissions for the output path.

<a id="xml2doc.core.markdownrenderer.rendertostring"></a>

## Method: RenderToString

Returns the consolidated single‑file content (index + all types) without writing.

<a id="xml2doc.core.markdownrenderer.rendertype(xml2doc.core.models.xmember,string,bool)"></a>

## Method: RenderType(XMember, string, bool)

Renders a single type (summary, remarks, examples, see‑also, optional member TOC, members grouped by overload).

**Parameters**

- `type` — Type (`T:`) member.
- `currentDocumentId` — Current planned document identity for relative links.
- `includeHeader` — Emit a top-level heading when true.

<a id="xml2doc.core.markdownrenderer.resolvetypehref(string,string)"></a>

## Method: ResolveTypeHref(string, string)

Produces the per‑type output filename for a cref (normalizes nested type separators then applies renderer rules).

<a id="xml2doc.core.markdownrenderer.safenamespacefilename(string)"></a>

## Method: SafeNamespaceFileName(string)

Creates a stable file safe namespace page filename (replaces separators and generic brackets).

<a id="xml2doc.core.markdownrenderer.seealsotomarkdown(system.xml.linq.xelement,string)"></a>

## Method: SeeAlsoToMarkdown(XElement, string)

Converts a `<seealso>` element to Markdown (cref, href, or inner text).
