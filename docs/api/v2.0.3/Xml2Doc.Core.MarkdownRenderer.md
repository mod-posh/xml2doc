# MarkdownRenderer

Renders a parsed XML documentation model to Markdown (multi‑file or single‑file).

**Remarks**

Core capabilities:

Multi‑file output via (one file per type + index.md). Single consolidated file via (index followed by all types). Overload grouping (method overloads share one heading, individual signatures listed as bullets). <inheritdoc> resolution / merge through. Stable anchors for member sections () and heading slugs (). Depth‑aware generic signature formatting with alias substitution (framework types → C# keywords). Paragraph‑preserving XML → Markdown normalization (code blocks kept verbatim; soft wraps collapsed). Optional root namespace trimming and filename transformations (). Optional per‑type member TOC (). Optional namespace index pages (). Deterministic planning of outputs without writing via (used for dry‑run / reporting). Selectable slug algorithm (): Default / GitHub / Kramdown / Gfm.

Anchor algorithm summary:

Default: lowercase, whitespace → dash, strip non [a-z0-9-], collapse multi‑dash runs. GitHub/Gfm: Unicode normalization + diacritic removal; drop punctuation; whitespace → dash; trim dashes. Kramdown: Similar to GitHub but retains underscores; punctuation removed; whitespace → dash.

Public rendering methods allow I/O exceptions to surface (no catch/ swallow beyond outer `Main` typical usage).

<a id="xml2doc.core.markdownrenderer.#ctor(xml2doc.core.models.xml2doc,xml2doc.core.rendereroptions)"></a>

## Method: #ctor(Xml2Doc, RendererOptions)

Creates a renderer for a parsed XML documentation model.

**Parameters**

- `model` — Parsed XML documentation model (never null).
- `options` — Optional rendering options; defaults applied when null.

<a id="xml2doc.core.markdownrenderer.aliases"></a>

## Field: Aliases

Built‑in mappings from fully‑qualified BCL types to C# aliases.

<a id="xml2doc.core.markdownrenderer.applyaliases(string)"></a>

## Method: ApplyAliases(string)

Applies alias substitutions to framework type tokens without touching longer identifiers.

<a id="xml2doc.core.markdownrenderer.buildmembertoc(system.collections.generic.ienumerable[xml2doc.core.models.xmember])"></a>

## Method: BuildMemberToc(IEnumerable<XMember>)

Builds a member table of contents (overload groups collapsed to first anchor).

<a id="xml2doc.core.markdownrenderer.buildsinglefilecontent"></a>

## Method: BuildSingleFileContent

Builds single‑file content, temporarily switching link mode to in‑document anchors.

**Returns**

Markdown string containing index + all types.

<a id="xml2doc.core.markdownrenderer.collapsedash"></a>

## Field: CollapseDash

Collapses consecutive dashes to a single dash (unused placeholder for potential manual slug pipelines).

<a id="xml2doc.core.markdownrenderer.creftomarkdown(string)"></a>

## Method: CrefToMarkdown(string)

Returns a Markdown link for a cref value (type or member).

<a id="xml2doc.core.markdownrenderer.creftomarkdown(system.text.stringbuilder,string)"></a>

## Method: CrefToMarkdown(StringBuilder, string)

Appends a Markdown link for a cref to a [StringBuilder](System.Text.StringBuilder.md) using the configured resolver.

<a id="xml2doc.core.markdownrenderer.defaultslug(string)"></a>

## Method: DefaultSlug(string)

Default slug (lowercase, whitespace → single dash, strip non `[a-z0-9-]`, collapse multi‑dash runs, trim dashes).

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

<a id="xml2doc.core.markdownrenderer.gfmdrop"></a>

## Field: GfmDrop

Precompiled pattern for GFM slug punctuation removal (unused placeholder).

<a id="xml2doc.core.markdownrenderer.gfmslug(string)"></a>

## Method: GfmSlug(string)

GFM slug variant: lowercase, retain underscore and dot, remove other punctuation, whitespace becomes dash, collapse dashes, trim.

<a id="xml2doc.core.markdownrenderer.githubdrop"></a>

## Field: GitHubDrop

Precompiled pattern for GitHub slug punctuation removal (currently unused; kept for potential micro‑optimization).

<a id="xml2doc.core.markdownrenderer.githubslug(string)"></a>

## Method: GithubSlug(string)

GitHub-style slug: Unicode normalize + diacritic removal, lowercase, drop punctuation, collapse spaces to dashes, trim.

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

<a id="xml2doc.core.markdownrenderer.kindtoword(string)"></a>

## Method: KindToWord(string)

Maps XML documentation kind letter to a readable word.

<a id="xml2doc.core.markdownrenderer.kramdowndrop"></a>

## Field: KramdownDrop

Precompiled pattern for Kramdown slug punctuation removal (unused placeholder).

<a id="xml2doc.core.markdownrenderer.kramdownslug(string)"></a>

## Method: KramdownSlug(string)

Kramdown/Jekyll slug: diacritics removed, lowercase, punctuation stripped (except underscore), whitespace → dash, trim.

<a id="xml2doc.core.markdownrenderer.memberheader(xml2doc.core.models.xmember)"></a>

## Method: MemberHeader(XMember)

Builds a concise member header (Kind + simplified signature) for headings and overload bullets.

<a id="xml2doc.core.markdownrenderer.normalizexmltomarkdown(system.xml.linq.xelement,bool)"></a>

## Method: NormalizeXmlToMarkdown(XElement, bool)

Normalizes an XML documentation element (summary, remarks, example, param, see, code) to Markdown with paragraph preservation.

**Parameters**

- `element` — XML element or null.
- `preferCodeBlocks` — True to prefer fenced blocks for multi‑line code/examples.

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

<a id="xml2doc.core.markdownrenderer.renderindex(system.collections.generic.ienumerable[xml2doc.core.models.xmember],bool)"></a>

## Method: RenderIndex(IEnumerable<XMember>, bool)

Builds a type index linking either to per‑type files or heading anchors (single‑file mode).

**Parameters**

- `types` — Sequence of type members.
- `useAnchors` — True to link to in‑document anchors; false for per‑type files.

<a id="xml2doc.core.markdownrenderer.rendermember(xml2doc.core.models.xmember,system.text.stringbuilder,bool)"></a>

## Method: RenderMember(XMember, StringBuilder, bool)

Renders a member (or overload bullet) including summary, parameters, returns, exceptions, examples, see‑also links, and a stable anchor.

**Parameters**

- `m` — Member to render.
- `sb` — Destination builder.
- `asOverload` — True to render as a bullet under an overload group; false for a full section.

<a id="xml2doc.core.markdownrenderer.rendertodirectory(string)"></a>

## Method: RenderToDirectory(string)

Emits one Markdown file per documented type and, by default, an `index.md`. Optionally emits namespace index pages.

**Remarks**

Overwrites existing files. Per‑type links point to sibling files; member links point to in‑file anchors. Respects [FileNameMode](Xml2Doc.Core.RendererOptions.md#xml2doc.core.rendereroptions.filenamemode) and [TrimRootNamespaceInFileNames](Xml2Doc.Core.RendererOptions.md#xml2doc.core.rendereroptions.trimrootnamespaceinfilenames). Namespace index emission ([EmitNamespaceIndex](Xml2Doc.Core.RendererOptions.md#xml2doc.core.rendereroptions.emitnamespaceindex)) adds:

namespaces.md — overview of all namespaces. namespaces/<namespace>.md — per‑namespace type listing.

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

<a id="xml2doc.core.markdownrenderer.rendertype(xml2doc.core.models.xmember,bool)"></a>

## Method: RenderType(XMember, bool)

Renders a single type (summary, remarks, examples, see‑also, optional member TOC, members grouped by overload).

**Parameters**

- `type` — Type (`T:`) member.
- `includeHeader` — Emit a top-level heading when true.

<a id="xml2doc.core.markdownrenderer.safenamespacefilename(string)"></a>

## Method: SafeNamespaceFileName(string)

Creates a stable file safe namespace page filename (replaces separators and generic brackets).

<a id="xml2doc.core.markdownrenderer.seealsotomarkdown(system.xml.linq.xelement)"></a>

## Method: SeeAlsoToMarkdown(XElement)

Converts a `<seealso>` element to Markdown (cref, href, or inner text).

<a id="xml2doc.core.markdownrenderer.shortensignaturetype(string)"></a>

## Method: ShortenSignatureType(string)

Shortens a fully‑qualified type for signature display (aliases + recursive generic argument formatting).

<a id="xml2doc.core.markdownrenderer.shortentypename(string)"></a>

## Method: ShortenTypeName(string)

Shortens a type cref for display (arity → placeholders, braces normalized, aliases applied).

<a id="xml2doc.core.markdownrenderer.shortlabelfromcref(string)"></a>

## Method: ShortLabelFromCref(string)

Generates a short label from a cref (type name or method name + simplified parameter list).

<a id="xml2doc.core.markdownrenderer.shorttypedisplay(string)"></a>

## Method: ShortTypeDisplay(string)

Produces a short display name for a type ID (generic arity → <T…>, optional root namespace trimming).

<a id="xml2doc.core.markdownrenderer.spaces"></a>

## Field: Spaces

Precompiled whitespace matching regex (reserved for future slug optimizations).

<a id="xml2doc.core.markdownrenderer.typefilenameforresolver(string)"></a>

## Method: TypeFileNameForResolver(string)

Produces the per‑type output filename for a cref (normalizes nested type separators then applies renderer rules).
