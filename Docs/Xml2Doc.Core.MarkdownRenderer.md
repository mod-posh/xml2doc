# Xml2Doc.Core.MarkdownRenderer

Renders an [Xml2Doc.Core.Models.Xml2Doc](Xml2Doc.Core.Models.Xml2Doc.md) model into Markdown files.

## F: _model

The loaded XML documentation model to render.

## M: Xml2Doc)

Initializes a new instance of the [Xml2Doc.Core.MarkdownRenderer](Xml2Doc.Core.MarkdownRenderer.md) class.

**Parameters**

- `model` — The XML documentation model to render.

## M: String)

Computes a display name for a documentation ID.

**Parameters**

- `id` — The documentation ID.

**Returns**

The display text. Currently returns the ID as-is.

## M: String)

Generates a file name for a type ID suitable for use on disk.

**Parameters**

- `typeId` — The type documentation ID (without the T: prefix).

**Returns**

The markdown file name. Generic angle brackets are replaced with square brackets.

## M: String)

Converts a documentation ID to a Markdown anchor.

**Parameters**

- `id` — The documentation ID.

**Returns**

A lowercase anchor string.

## M: XMember)

Builds a concise header for a member, e.g., M: Method(…).

**Parameters**

- `m` — The member to summarize.

**Returns**

A short header containing the kind prefix and simplified signature.

## M: XElement)

Converts XML documentation nodes to Markdown text.

**Parameters**

- `element` — The XML element to normalize (e.g., summary, returns, or param).

**Returns**

The normalized Markdown text, or an empty string if `element` is.

## M: String)

Renders all types from the model into Markdown files within the specified output directory.

**Parameters**

- `outDir` — The output directory. It is created if it does not exist.

## M: XMember)

Renders a single type and its members to a Markdown document.

**Parameters**

- `type` — The type member (T: entry) to render.

**Returns**

The Markdown content for the specified type.
