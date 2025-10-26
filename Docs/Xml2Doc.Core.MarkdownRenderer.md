# MarkdownRenderer

Renders a parsed XML documentation model to Markdown files.

**Remarks**

- Use [RenderToDirectory(string)](Xml2Doc.md#xml2doc.core.markdownrenderer.rendertodirectory(string)) to emit one file per type plus an index. - Use [RenderToSingleFile(string)](Xml2Doc.md#xml2doc.core.markdownrenderer.rendertosinglefile(string)) to generate a single consolidated Markdown file. Rendering is influenced by [RendererOptions](Xml2Doc.Core.RendererOptions.md) (filename style, code block language, and display trimming).

**See also**

- [RendererOptions](Xml2Doc.Core.RendererOptions.md)
- [FileNameMode](Xml2Doc.Core.FileNameMode.md)

<a id="xml2doc.core.markdownrenderer.#ctor(xml2doc.core.models.xml2doc,xml2doc.core.rendereroptions)"></a>

## Method: #ctor(Xml2Doc, RendererOptions)

Initializes a new instance of [MarkdownRenderer](Xml2Doc.Core.MarkdownRenderer.md).

**Parameters**

- `model` — The XML documentation model to render.
- `options` — Optional rendering options. If, defaults are used (e.g., [Verbatim](Xml2Doc.md#xml2doc.core.filenamemode.verbatim), language `csharp`).

<a id="xml2doc.core.markdownrenderer.aliases"></a>

## Field: Aliases

Built-in mappings for fully-qualified BCL types and their C# aliases.

<a id="xml2doc.core.markdownrenderer.applyaliases(string)"></a>

## Method: ApplyAliases(string)

Replaces fully-qualified type names and common framework type names with their C# aliases.

**Parameters**

- `s` — The input type string.

**Returns**

The aliased form (e.g., `System.String` becomes `string`).

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

<a id="xml2doc.core.markdownrenderer.idtoanchor(string)"></a>

## Method: IdToAnchor(string)

Converts a documentation ID into a Markdown anchor.

**Parameters**

- `id` — The documentation ID (portion after the kind prefix).

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

<a id="xml2doc.core.markdownrenderer.renderindex(system.collections.generic.ienumerable[xml2doc.core.models.xmember])"></a>

## Method: RenderIndex(IEnumerable<XMember>)

Builds the table of contents for the provided types.

**Parameters**

- `types` — The set of types to include in the index.

**Returns**

Markdown content for the index page.

<a id="xml2doc.core.markdownrenderer.rendertodirectory(string)"></a>

## Method: RenderToDirectory(string)

Renders all types to individual Markdown files in the specified directory and writes an `index.md`.

**Parameters**

- `outDir` — The output directory. It is created if it does not exist.

<a id="xml2doc.core.markdownrenderer.rendertosinglefile(string)"></a>

## Method: RenderToSingleFile(string)

Renders all types to a single Markdown file that includes an index followed by each type section.

**Parameters**

- `outPath` — The output file path. The containing directory is created if necessary.

<a id="xml2doc.core.markdownrenderer.rendertype(xml2doc.core.models.xmember)"></a>

## Method: RenderType(XMember)

Renders a single type section including summary, remarks, examples, see-also, and its members.

**Parameters**

- `type` — The type (`T:` entry) to render.

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

Shortens a fully-qualified type used in a signature to a compact display form.

**Parameters**

- `full` — The full type representation, e.g., `System.Collections.Generic.List{System.String}`.

**Returns**

A simplified representation, e.g., `List<string>`.

<a id="xml2doc.core.markdownrenderer.shortentypename(string)"></a>

## Method: ShortenTypeName(string)

Produces a short label from a `cref` for display purposes (e.g., replaces arity and aliases BCL types).

**Parameters**

- `cref` — The cref value, e.g., `T:Namespace.Type`2` or `M:Namespace.Type.Method(System.String)`.

**Returns**

A simplified display name.

<a id="xml2doc.core.markdownrenderer.shorttypedisplay(string)"></a>

## Method: ShortTypeDisplay(string)

Produces a short display name for a type ID, optionally trimming a root namespace and formatting generic arity as `<T1,T2>`.

**Parameters**

- `typeId` — The type documentation ID (portion after the `T:` prefix).
