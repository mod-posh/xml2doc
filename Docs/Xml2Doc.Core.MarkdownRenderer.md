# MarkdownRenderer

Renders a parsed XML documentation model to Markdown files.

**Remarks**

- Use [RenderToDirectory(string)](Xml2Doc.md#xml2doc.core.markdownrenderer.rendertodirectory(string)) to emit one file per type plus an index. - Use [RenderToSingleFile(string)](Xml2Doc.md#xml2doc.core.markdownrenderer.rendertofloatfile(string)) to generate a single consolidated Markdown file. - Overloaded methods are grouped under a single header with each overload listed as a bullet. - `<inheritdoc>` is resolved and merged via [InheritDocResolver](Xml2Doc.Core.InheritDocResolver.md) before rendering. Rendering is influenced by [RendererOptions](Xml2Doc.Core.RendererOptions.md) (filename style, code block language, and optional root namespace trimming).

**See also**

- [RendererOptions](Xml2Doc.Core.RendererOptions.md)
- [FileNameMode](Xml2Doc.Core.FileNameMode.md)
- [InheritDocResolver](Xml2Doc.Core.InheritDocResolver.md)

## Method: #ctor(Xml2Doc, RendererOptions)

Initializes a new instance of [MarkdownRenderer](Xml2Doc.Core.MarkdownRenderer.md).

**Parameters**

- `model` — The XML documentation model to render.
- `options` — Optional rendering options. If , defaults are used (e.g., [Verbatim](Xml2Doc.md#xml2doc.core.filenamemode.verbatim), language `csharp`).

## Field: Aliases

Built-in mappings for fully-qualified BCL types and their C# aliases.

## Method: ApplyAliases(string)

Replaces fully-qualified type names and common framework type names with their C# aliases.

**Parameters**

- `s` — The input type string.

**Returns**

The aliased form (e.g., `System.String` becomes `string`).

## Method: CrefToMarkdown(string, string)

Converts a `cref` to a Markdown link, resolving types and members to local files/anchors.

**Parameters**

- `cref` — The cref value (e.g., `T:Namespace.Type`, `M:Namespace.Type.Method`).
- `displayFallback` — Optional display text if the cref cannot be resolved.

**Returns**

A Markdown link, or the fallback/display text if unavailable.

## Method: FileNameFor(string, FileNameMode)

Generates a Markdown file name for a type ID based on the chosen [FileNameMode](Xml2Doc.Core.FileNameMode.md).

**Parameters**

- `typeId` — The type ID (portion after the kind prefix).
- `mode` — The file name generation mode.

**Returns**

A file-system-friendly name ending with `.md`.

## Method: GetTypes

Gets all documented types (`T:` members) from the model.

**Returns**

An enumeration of members whose kind is `"T"` (types).

## Method: HeadingSlug(string)

Builds a slug from a heading text compatible with common Markdown engines (e.g., GitHub). Lowercases, trims, replaces spaces with dashes, and removes non [a-z0-9-].

## Method: IdToAnchor(string)

Converts a documentation ID into a Markdown anchor.

**Parameters**

- `id` — The documentation ID (portion after the kind prefix).

**Returns**

Lowercase anchor text that can be referenced in links.

## Method: KindToWord(string)

Converts a documentation kind letter to a readable word.

**Parameters**

- `kind` — The kind prefix (e.g., `M`, `P`, `F`, `E`, `T`).

## Method: MemberHeader(XMember)

Builds a concise header for a member (e.g., `Method: Foo(int, string)`), simplifying type names and generics.

**Parameters**

- `m` — The member to summarize.

**Returns**

A short header containing the member kind and simplified signature.

## Method: NormalizeXmlToMarkdown(XElement, bool)

Normalizes XML documentation nodes to Markdown.

**Parameters**

- `element` — The XML element to normalize (e.g., `summary`, `remarks`, `returns`, `param`, `example`).
- `preferCodeBlocks` — If , prefers fenced code blocks for code samples (e.g., within `example` or `code` elements).

**Returns**

The normalized Markdown text, or an empty string if `element` is.

## Method: RenderIndex(IEnumerable<XMember>, bool)

Builds the table of contents for the provided types.

**Parameters**

- `types` — The set of types to include in the index.
- `useAnchors` — When true, emits in-document anchor links (for single-file mode). When false (default), links to per-type files.

**Returns**

Markdown content for the index page.

## Method: RenderMember(XMember, stringBuilder, bool)

Renders a single member section or overload list item, including summary, parameters, returns, exceptions, examples, and see-also.

**Parameters**

- `m` — The member to render.
- `sb` — The output builder to append Markdown to.
- `asOverload` — If , renders as a bullet item under an overload group; otherwise renders as a full section with a heading.

## Method: RenderToDirectory(string)

Renders all types to individual Markdown files in the specified directory and writes an `index.md`.

**Parameters**

- `outDir` — The output directory. It is created if it does not exist.

**Exceptions**

- [IOException](System.IO.IOException.md) — An I/O error occurs while writing files.
- [UnauthorizedAccessException](System.UnauthorizedAccessException.md) — Caller does not have the required permission.

## Method: RenderToSingleFile(string)

Renders all types to a single Markdown file that includes an index followed by each type section.

**Parameters**

- `outPath` — The output file path. The containing directory is created if necessary.

**Exceptions**

- [IOException](System.IO.IOException.md) — An I/O error occurs while writing the file.
- [UnauthorizedAccessException](System.UnauthorizedAccessException.md) — Caller does not have the required permission.

## Method: RenderToString

Returns the single-file markdown content as a string (same as RenderToSingleFile but without writing to disk).

## Method: RenderType(XMember, bool)

Renders a single type section including summary, remarks, examples, see-also, and its members.

**Parameters**

- `type` — The type (`T:` entry) to render.
- `includeHeader` — When true, includes the type heading; otherwise only renders the body.

**Returns**

Markdown content for the specified type.

## Method: SeeAlsoToMarkdown(XElement)

Converts a `<seealso>` element into Markdown.

**Parameters**

- `sa` — The `seealso` element.

**Returns**

A Markdown link or normalized text.

## Method: ShortenSignatureType(string)

Shortens a fully-qualified type used in a signature to a compact display form, preserving the outer generic type name and formatting generic arguments recursively. Handles XML-doc generics (`{}`) → (`<>`), BCL aliases, and generic placeholders (```0`/``0` → `T1`).

## Method: ShortenTypeName(string)

Produces a short label from a `cref` for display purposes (e.g., replaces arity and aliases BCL types).

**Parameters**

- `cref` — The cref value, e.g., `T:Namespace.Type`2` or `M:Namespace.Type.Method(System.String)`.

**Returns**

A simplified display name.

## Method: ShortLabelFromCref(string)

Creates a short, human-friendly label from a cref string.

**Parameters**

- `cref` — A cref such as `T:Namespace.Type` or `M:Namespace.Type.Method(Type,Type)`.

**Returns**

For types, the short type display (with generic arity formatted). For methods, the method name with simplified parameter types. For other kinds, the simple member identifier.

## Method: ShortTypeDisplay(string)

Produces a short display name for a type ID, optionally trimming a root namespace and formatting generic arity as `<T1,T2>`.

**Parameters**

- `typeId` — The type documentation ID (portion after the `T:` prefix).

**Returns**

The simple type name with generic arity displayed, and the root namespace removed if configured.
