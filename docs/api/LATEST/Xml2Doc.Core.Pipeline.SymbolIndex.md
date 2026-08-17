# SymbolIndex

Provides an immutable, deterministically ordered snapshot of the symbols used during a rendering invocation.

<a id="xml2doc.core.pipeline.symbolindex.build(xml2doc.core.models.xml2doc)"></a>

## Method: Build(Xml2Doc)

Builds an immutable snapshot from a parsed XML documentation model.

<a id="xml2doc.core.pipeline.symbolindex.containsmember(string)"></a>

## Method: ContainsMember(string)

Determines whether a renderable symbol exists in the snapshot.

<a id="xml2doc.core.pipeline.symbolindex.members"></a>

## Property: Members

Gets renderable symbols keyed by XML documentation ID.

<a id="xml2doc.core.pipeline.symbolindex.referencemembers"></a>

## Property: ReferenceMembers

Gets reference-only symbols used for inheritance lookup.

<a id="xml2doc.core.pipeline.symbolindex.types"></a>

## Property: Types

Gets renderable type symbols in ordinal documentation-ID order.
