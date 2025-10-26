# Xml2Doc

Represents an in-memory model of a.NET XML documentation file.

<a id="xml2doc.core.models.xml2doc.load(string)"></a>

## Method: Load(string)

Loads an XML documentation file and builds the [Xml2Doc](Xml2Doc.Core.Models.Xml2Doc.md) model.

**Parameters**

- `xmlPath` — The path to the XML documentation file.

**Returns**

An [Xml2Doc](Xml2Doc.Core.Models.Xml2Doc.md) instance containing parsed members.

<a id="xml2doc.core.models.xml2doc.members"></a>

## Property: Members

Gets the collection of documented members keyed by their XML documentation `name` attribute.
