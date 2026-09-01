# DocumentDescriptor

Identifies one logical Markdown document before template or front-matter application.

**Remarks**

The descriptor contains only identity supported by Xml2Doc's authoritative inputs. It does not infer whether a documented type is a class, interface, record, struct, or enum.

<a id="xml2doc.core.documentdescriptor.#ctor(xml2doc.core.templates.templatedocumentkind,string,string,string)"></a>

## Method: #ctor(TemplateDocumentKind, string, string, string)

Creates an immutable logical document descriptor.

**Parameters**

- `kind` — The kind of generated Markdown document.
- `documentId` — Stable logical identity for the generated document.
- `namespace` — Applicable documented namespace, or `null`.
- `symbol` — Applicable unqualified documented symbol, or `null`.

<a id="xml2doc.core.documentdescriptor.documentid"></a>

## Property: DocumentId

Gets the stable logical identity for the generated document.

<a id="xml2doc.core.documentdescriptor.kind"></a>

## Property: Kind

Gets the kind of generated Markdown document.

<a id="xml2doc.core.documentdescriptor.namespace"></a>

## Property: Namespace

Gets the applicable documented namespace.

<a id="xml2doc.core.documentdescriptor.symbol"></a>

## Property: Symbol

Gets the applicable unqualified documented symbol.
