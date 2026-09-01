# TemplateRenderContext

Describes a rendered Markdown document before template application.

<a id="xml2doc.core.templates.templaterendercontext.#ctor(string,string,xml2doc.core.templates.templatedocumentkind)"></a>

## Method: #ctor(string, string, TemplateDocumentKind)

Describes a rendered Markdown document before template application.

**Parameters**

- `Content` — The complete built-in Markdown content.
- `Title` — The document title, when available.
- `Kind` — The kind of generated document.

<a id="xml2doc.core.templates.templaterendercontext.content"></a>

## Property: Content

The complete built-in Markdown content.

<a id="xml2doc.core.templates.templaterendercontext.document"></a>

## Property: Document

Gets the logical identity metadata supplied by an Xml2Doc rendering operation.

**Remarks**

This remains `null` when a context is constructed directly through the backward-compatible three-argument constructor.

<a id="xml2doc.core.templates.templaterendercontext.kind"></a>

## Property: Kind

The kind of generated document.

<a id="xml2doc.core.templates.templaterendercontext.metadata"></a>

## Property: Metadata

Gets the immutable document-derived and caller-supplied metadata for this render.

**Remarks**

Document-derived keys are authoritative when they collide with caller-supplied values. Directly constructed contexts expose an empty collection.

<a id="xml2doc.core.templates.templaterendercontext.outputpath"></a>

## Property: OutputPath

Gets the resolved output-root-relative logical path using forward slashes.

**Remarks**

In-memory rendering that has no resolved output location exposes `null`.

<a id="xml2doc.core.templates.templaterendercontext.title"></a>

## Property: Title

The document title, when available.
