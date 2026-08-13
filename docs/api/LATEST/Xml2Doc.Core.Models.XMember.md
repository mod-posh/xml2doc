# XMember

Represents a single `<member>` element from an XML documentation file.

<a id="xml2doc.core.models.xmember.#ctor(string,system.xml.linq.xelement)"></a>

## Method: #ctor(string, XElement)

Represents a single `<member>` element from an XML documentation file.

**Parameters**

- `Name` — The full documentation ID (e.g., `M:Namespace.Type.Method(System.String)`).
- `Element` — The underlying XML element for this member.

<a id="xml2doc.core.models.xmember.element"></a>

## Property: Element

The underlying XML element for this member.

<a id="xml2doc.core.models.xmember.id"></a>

## Property: Id

Gets the identifier portion of the documentation ID after the colon.

**Example**

```csharp
M:MyNamespace.MyType.MyMethod(System.String)
```

<a id="xml2doc.core.models.xmember.kind"></a>

## Property: Kind

Gets the kind prefix of the documentation ID before the colon.

<a id="xml2doc.core.models.xmember.name"></a>

## Property: Name

The full documentation ID (e.g., `M:Namespace.Type.Method(System.String)`).
