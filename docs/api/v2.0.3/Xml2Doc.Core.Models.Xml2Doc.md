# Xml2Doc

Represents an in-memory model of a.NET XML documentation file.

<a id="xml2doc.core.models.xml2doc.load(string)"></a>

## Method: Load(string)

Loads an XML documentation file and builds the [Xml2Doc](Xml2Doc.Core.Models.Xml2Doc.md) model.

**Remarks**

Exceptions thrown are those of [Load(string, LoadOptions)](System.Xml.Linq.XDocument.md#system.xml.linq.xdocument.load(string,system.xml.linq.loadoptions)) and file I/O operations (e.g., file not found, access denied, malformed XML).

**Parameters**

- `xmlPath` — The path to the XML documentation file.

**Returns**

An [Xml2Doc](Xml2Doc.Core.Models.Xml2Doc.md) instance containing parsed members.

<a id="xml2doc.core.models.xml2doc.loadreferences(system.collections.generic.ienumerable[string])"></a>

## Method: LoadReferences(IEnumerable<string>)

Loads additional XML documentation for inheritance lookup without adding referenced types to the set of rendered output pages.

**Parameters**

- `xmlPaths` — Reference XML documentation paths.

<a id="xml2doc.core.models.xml2doc.members"></a>

## Property: Members

Gets the collection of documented members keyed by their XML documentation `name` attribute.

**Remarks**

Keys are case-sensitive and compared using [Ordinal](System.StringComparer.md#system.stringcomparer.ordinal). Examples: `T:MyNamespace.MyType`, `M:MyNamespace.MyType.MyMethod(System.String)`.

<a id="xml2doc.core.models.xml2doc.referencemembers"></a>

## Property: ReferenceMembers

Gets documented members loaded from reference XML files. These members are available for inheritance lookup but are not rendered as output pages.
