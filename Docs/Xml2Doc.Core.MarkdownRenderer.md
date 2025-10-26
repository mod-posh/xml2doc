# MarkdownRenderer

Renders a parsed XML documentation model to Markdown files.

**Remarks**

- Use [RenderToDirectory(string)](Xml2Doc.Core.MarkdownRenderer.md#xml2doc.core.markdownrenderer.rendertodirectory(string)) to emit one file per type plus an index. - Use [RenderToSingleFile(string)](Xml2Doc.Core.MarkdownRenderer.md#xml2doc.core.markdownrenderer.rendertosinglefile(string)) to generate a single consolidated Markdown file. - Overloaded methods are grouped under a single header with each overload listed as a bullet. - `<inheritdoc>` is resolved and merged via [InheritDocResolver](Xml2Doc.Core.InheritDocResolver.md) before rendering. - Each member section emits a stable HTML anchor (via [IdToAnchor(string)](Xml2Doc.Core.MarkdownRenderer.md#xml2doc.core.markdownrenderer.idtoanchor(string))) so cref links resolve reliably. - In single-file output, each type section also emits an anchor derived from the visible heading text (via [HeadingSlug(string)](Xml2Doc.Core.MarkdownRenderer.md#xml2doc.core.markdownrenderer.headingslug(string))). - Token-aware aliasing prevents accidental replacements inside longer identifiers (e.g., keeps `StringComparer` intact). - Depth-aware generic formatting: nested generics (e.g., `Dictionary<string, List<int>>`) are preserved and displayed compactly. - Paragraph-preserving normalization: preserves paragraph breaks and fenced code blocks, collapses soft line wraps, and trims stray spaces before punctuation.

Rendering is influenced by [RendererOptions](Xml2Doc.Core.RendererOptions.md) (filename style, code block language, and optional root namespace trimming).

**See also**

- [RendererOptions](Xml2Doc.Core.RendererOptions.md)
- [FileNameMode](Xml2Doc.Core.FileNameMode.md)
- [InheritDocResolver](Xml2Doc.Core.InheritDocResolver.md)

<a id="xml2doc.core.markdownrenderer.#ctor(xml2doc.core.models.xml2doc,xml2doc.core.rendereroptions)"></a>

## Method: #ctor(Xml2Doc, RendererOptions)

Initializes a new instance of [MarkdownRenderer](Xml2Doc.Core.MarkdownRenderer.md).

**Parameters**

- `model` — The XML documentation model to render.
- `options` — Optional rendering options. If, defaults are used (e.g., [Verbatim](Xml2Doc.Core.FileNameMode.md#xml2doc.core.filenamemode.verbatim), language `csharp`).

<a id="xml2doc.core.markdownrenderer.aliases"></a>

## Field: Aliases

Built-in mappings for fully-qualified BCL types and their C# aliases.

<a id="xml2doc.core.markdownrenderer.applyaliases(string)"></a>

## Method: ApplyAliases(string)

Replaces fully-qualified type names and common framework type names with their C# aliases, using token-aware regex so we don't corrupt longer identifiers (e.g., `StringComparer`).

<a id="xml2doc.core.markdownrenderer.creftomarkdown(string,string)"></a>

## Method: CrefToMarkdown(string, string)

Converts a `cref` to a Markdown link, resolving types and members to local files/anchors.

**Parameters**

- `cref` — The cref value (e.g., `T:Namespace.Type`, `M:Namespace.Type.Method`).
- `displayFallback` — Optional display text if the cref cannot be resolved.

**Returns**

A Markdown link, or the fallback/display text if unavailable.

<a id="xml2doc.core.markdownrenderer.filenamefor(string,xml2doc.core.filenamemode)"></a>

## Method: FileNameFor(string, FileNameMode)

Generates a Markdown file name for a type ID based on the chosen [FileNameMode](Xml2Doc.Core.FileNameMode.md).

**Parameters**

- `typeId` — The type ID (portion after the kind prefix).
- `mode` — The file name generation mode.

**Returns**

A file-system-friendly name ending with `.md`.

<a id="xml2doc.core.markdownrenderer.gettypes"></a>

## Method: GetTypes

Gets all documented types (`T:` members) from the model.

**Returns**

An enumeration of members whose kind is `"T"` (types).

<a id="xml2doc.core.markdownrenderer.headingslug(string)"></a>

## Method: HeadingSlug(string)

Builds a slug from a heading text compatible with common Markdown engines (e.g., GitHub). Lowercases, trims, replaces spaces with dashes, and removes non [a-z0-9-].

<a id="xml2doc.core.markdownrenderer.idtoanchor(string)"></a>

## Method: IdToAnchor(string)

Converts a documentation ID into a Markdown anchor.

**Parameters**

- `id` — The documentation ID (portion after the kind prefix).

**Returns**

Anchor text (lowercased) that can be referenced in links.

<a id="xml2doc.core.markdownrenderer.kindtoword(string)"></a>

## Method: KindToWord(string)

Converts a documentation kind letter to a readable word.

**Parameters**

- `kind` — The kind prefix (e.g., `M`, `P`, `F`, `E`, `T`).

<a id="xml2doc.core.markdownrenderer.memberheader(xml2doc.core.models.xmember)"></a>

## Method: MemberHeader(XMember)

Builds a concise header for a member (e.g., `Method: Foo(int, string)`), simplifying type names and generics.

**Parameters**

- `m` — The member to summarize.

**Returns**

A short header containing the member kind and simplified signature.

<a id="xml2doc.core.markdownrenderer.normalizexmltomarkdown(system.xml.linq.xelement,bool)"></a>

## Method: NormalizeXmlToMarkdown(XElement, bool)

Normalizes XML documentation nodes to Markdown.

**Parameters**

- `element` — The XML element to normalize (e.g., `summary`, `remarks`, `returns`, `param`, `example`).
- `preferCodeBlocks` — If, prefers fenced code blocks for code samples (e.g., within `example` or `code` elements).

**Returns**

The normalized Markdown text, or an empty string if `element` is.

<a id="xml2doc.core.markdownrenderer.renderindex(system.collections.generic.ienumerable[xml2doc.core.models.xmember],bool)"></a>

## Method: RenderIndex(IEnumerable<XMember>, bool)

Builds the table of contents for the provided types.

**Parameters**

- `types` — The set of types to include in the index.
- `useAnchors` — When, emits in-document anchor links (for single-file mode). When, links to per-type files.

**Returns**

Markdown content for the index page.

<a id="xml2doc.core.markdownrenderer.rendermember(xml2doc.core.models.xmember,system.text.stringbuilder,bool)"></a>

## Method: RenderMember(XMember, StringBuilder, bool)

Renders a single member section or overload list item, including summary, parameters, returns, exceptions, examples, and see-also.

**Parameters**

- `m` — The member to render.
- `sb` — The output builder to append Markdown to.
- `asOverload` — If, renders as a bullet item under an overload group; otherwise renders as a full section with a heading.

<a id="xml2doc.core.markdownrenderer.rendertodirectory(string)"></a>

## Method: RenderToDirectory(string)

Renders all types to individual Markdown files in the specified directory and writes an `index.md`.

**Parameters**

- `outDir` — The output directory. It is created if it does not exist.

**Exceptions**

- [IOException](System.IO.IOException.md) — An I/O error occurs while writing files.
- [UnauthorizedAccessException](System.UnauthorizedAccessException.md) — Caller does not have the required permission.

<a id="xml2doc.core.markdownrenderer.rendertosinglefile(string)"></a>

## Method: RenderToSingleFile(string)

Renders all types to a single Markdown file that includes an index followed by each type section.

**Parameters**

- `outPath` — The output file path. The containing directory is created if necessary.

**Exceptions**

- [IOException](System.IO.IOException.md) — An I/O error occurs while writing the file.
- [UnauthorizedAccessException](System.UnauthorizedAccessException.md) — Caller does not have the required permission.

<a id="xml2doc.core.markdownrenderer.rendertostring"></a>

## Method: RenderToString

Returns the single-file markdown content as a string (same as [RenderToSingleFile(string)](Xml2Doc.Core.MarkdownRenderer.md#xml2doc.core.markdownrenderer.rendertosinglefile(string)) but without writing to disk).

<a id="xml2doc.core.markdownrenderer.rendertype(xml2doc.core.models.xmember,bool)"></a>

## Method: RenderType(XMember, bool)

Renders a single type section including summary, remarks, examples, see-also, and its members.

**Parameters**

- `type` — The type (`T:` entry) to render.
- `includeHeader` — When, includes the type heading; otherwise only renders the body.

**Returns**

Markdown content for the specified type.

<a id="xml2doc.core.markdownrenderer.seealsotomarkdown(system.xml.linq.xelement)"></a>

## Method: SeeAlsoToMarkdown(XElement)

Converts a `<seealso>` element into Markdown.

**Parameters**

- `sa` — The `seealso` element.

**Returns**

A Markdown link or normalized text.

<a id="xml2doc.core.markdownrenderer.shortensignaturetype(string)"></a>

## Method: ShortenSignatureType(string)

Shortens a fully-qualified type used in a signature to a compact display form, preserving the outer generic type name and formatting generic arguments recursively. Handles XML-doc generics (`{}`) → (`<>`), BCL aliases, and generic placeholders (```0`/``0` → `T1`).

<a id="xml2doc.core.markdownrenderer.shortentypename(string)"></a>

## Method: ShortenTypeName(string)

Produces a short label from a `cref` for display purposes (e.g., replaces arity and aliases BCL types).

**Parameters**

- `cref` — The cref value, e.g., `T:Namespace.Type`2` or `M:Namespace.Type.Method(System.String)`.

**Returns**

A simplified display name.

<a id="xml2doc.core.markdownrenderer.shortlabelfromcref(string)"></a>

## Method: ShortLabelFromCref(string)

Creates a short, human-friendly label from a cref string.

**Parameters**

- `cref` — A cref such as `T:Namespace.Type` or `M:Namespace.Type.Method(Type,Type)`.

**Returns**

For types, the short type display (with generic arity formatted). For methods, the method name with simplified parameter types. For other kinds, the simple member identifier.

<a id="xml2doc.core.markdownrenderer.shorttypedisplay(string)"></a>

## Method: ShortTypeDisplay(string)

Produces a short display name for a type ID, optionally trimming a root namespace and formatting generic arity as `<T1,T2>`. Handles constructed generics (XML-doc `{}`) by delegating to [ShortenSignatureType(string)](Xml2Doc.Core.MarkdownRenderer.md#xml2doc.core.markdownrenderer.shortensignaturetype(string)) for depth-aware formatting.
