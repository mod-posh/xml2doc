# BuiltInDocumentPathResolver

Provides Xml2Doc's supported built-in document layouts.

<a id="xml2doc.core.paths.builtindocumentpathresolver.#ctor(xml2doc.core.paths.documentlayout,string)"></a>

## Method: #ctor(DocumentLayout, string)

Creates a resolver for a built-in layout.

**Parameters**

- `layout` — Layout selected by the caller.
- `rootNamespaceToTrim` — Optional namespace prefix removed from namespace directories.

<a id="xml2doc.core.paths.builtindocumentpathresolver.getpath(xml2doc.core.paths.documentpathcontext)"></a>

## Method: GetPath(DocumentPathContext)

Returns the canonical logical path for `context`.
