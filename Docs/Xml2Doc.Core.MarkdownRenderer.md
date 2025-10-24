# MarkdownRenderer

Renders a parsed XML documentation model to Markdown files.

**Remarks**

- Use [string)](Xml2Doc.md#xml2doc.core.markdownrenderer.rendertodirectory(system.string)) to emit one file per type plus an index. - Use [string)](Xml2Doc.md#xml2doc.core.markdownrenderer.rendertosinglefile(system.string)) to generate a single consolidated Markdown file. Rendering is influenced by [RendererOptions](Xml2Doc.Core.RendererOptions.md) (filename style, code block language, and display trimming).

**See also**

- [RendererOptions](Xml2Doc.Core.RendererOptions.md)
- [FileNameMode](Xml2Doc.Core.FileNameMode.md)

## Field: Aliases

Built-in mappings for fully-qualified BCL types and their C# aliases.

## Method: Boolean)

Normalizes XML documentation nodes to Markdown.

**Parameters**

- `element` — The XML element to normalize (e.g., `summary`, `remarks`, `returns`, `param`, `example`).
- `preferCodeBlocks` — If , prefers fenced code blocks for code samples (e.g., within `example` or `code` elements).

**Returns**

The normalized Markdown text, or an empty string if `element` is.

## Method: FileNameMode)

Generates a Markdown file name for a type ID based on the chosen [FileNameMode](Xml2Doc.Core.FileNameMode.md).

**Parameters**

- `typeId` — The type ID (portion after the kind prefix).
- `mode` — The file name generation mode.

**Returns**

A file-system-friendly name ending with `.md`.

## Method: GetTypes

Gets all documented types (`T:` members) from the model.

## Method: RendererOptions)

Initializes a new instance of [MarkdownRenderer](Xml2Doc.Core.MarkdownRenderer.md).

**Parameters**

- `model` — The XML documentation model to render.
- `options` — Optional rendering options. If , defaults are used (e.g., [Verbatim](Xml2Doc.md#xml2doc.core.filenamemode.verbatim), language `csharp`).

## Method: String)

- `Method: String)`
Replaces fully-qualified type names and common framework type names with their C# aliases.

**Parameters**

- `s` — The input type string.

**Returns**

The aliased form (e.g., `System.String` becomes `string`).

- `Method: String)`
Converts a `cref` to a Markdown link, resolving types and members to local files/anchors.

**Parameters**

- `cref` — The cref value (e.g., `T:Namespace.Type`, `M:Namespace.Type.Method`).
- `displayFallback` — Optional display text if the cref cannot be resolved.

**Returns**

A Markdown link, or the fallback/display text if unavailable.

- `Method: String)`
Converts a documentation ID into a Markdown anchor.

**Parameters**

- `id` — The documentation ID (portion after the kind prefix).

- `Method: String)`
Converts a documentation kind letter to a readable word.

**Parameters**

- `kind` — The kind prefix (e.g., `M`, `P`, `F`, `E`, `T`).

- `Method: String)`
Renders all types to individual Markdown files in the specified directory and writes an `index.md`.

**Parameters**

- `outDir` — The output directory. It is created if it does not exist.

- `Method: String)`
Renders all types to a single Markdown file that includes an index followed by each type section.

**Parameters**

- `outPath` — The output file path. The containing directory is created if necessary.

- `Method: String)`
Shortens a fully-qualified type used in a signature to a compact display form.

**Parameters**

- `full` — The full type representation, e.g., `System.Collections.Generic.List{System.String}`.

**Returns**

A simplified representation, e.g., `List<string>`.

- `Method: String)`
Produces a short label from a `cref` for display purposes (e.g., replaces arity and aliases BCL types).

**Parameters**

- `cref` — The cref value, e.g., `T:Namespace.Type`2` or `M:Namespace.Type.Method(System.String)`.

**Returns**

A simplified display name.

- `Method: String)`
Produces a short display name for a type ID, optionally trimming a root namespace and formatting generic arity as `<T1,T2>`.

**Parameters**

- `typeId` — The type documentation ID (portion after the `T:` prefix).

## Method: XElement)

Converts a `<seealso>` element into Markdown.

**Parameters**

- `sa` — The `seealso` element.

**Returns**

A Markdown link or normalized text.

## Method: XMember)

- `Method: XMember)`
Builds a concise header for a member (e.g., `Method: Foo(int, string)`), simplifying type names and generics.

**Parameters**

- `m` — The member to summarize.

**Returns**

A short header containing the member kind and simplified signature.

- `Method: XMember)`
Renders a single type section including summary, remarks, examples, see-also, and its members.

**Parameters**

- `type` — The type (`T:` entry) to render.

**Returns**

Markdown content for the specified type.

## Method: XMember})

Builds the table of contents for the provided types.

**Parameters**

- `types` — The set of types to include in the index.

**Returns**

Markdown content for the index page.
