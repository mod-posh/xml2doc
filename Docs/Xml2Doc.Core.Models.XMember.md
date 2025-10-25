# XMember

Represents a single `<member>` element from an XML documentation file.

## Method: #ctor(string, XElement)

Represents a single `<member>` element from an XML documentation file.

**Parameters**

- `Name` — The full documentation ID (e.g., `M:Namespace.Type.Method(System.String)`).
- `Element` — The underlying XML element for this member.

## Property: Element

The underlying XML element for this member.

## Property: Id

Gets the identifier portion of the documentation ID after the colon.

**Example**

```csharp
M:MyNamespace.MyType.MyMethod(System.String)
```

## Property: Kind

Gets the kind prefix of the documentation ID before the colon.

## Property: Name

The full documentation ID (e.g., `M:Namespace.Type.Method(System.String)`).
