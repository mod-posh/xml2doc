# BaseUrlExternalSymbolResolver

Resolves symbols beneath a common documentation URL by appending the escaped identifier without its XML documentation kind prefix.

<a id="xml2doc.core.linking.baseurlexternalsymbolresolver.#ctor(string)"></a>

## Method: #ctor(string)

Creates a resolver for the supplied documentation base URL.

**Parameters**

- `baseUrl` — The non-empty URL prefix used for resolved symbols.

<a id="xml2doc.core.linking.baseurlexternalsymbolresolver.tryresolve(string,string@)"></a>

## Method: TryResolve(string, string@)

Attempts to resolve an XML documentation identifier such as `T:System.String` to an absolute or site-relative URL.

**Parameters**

- `cref` — The complete XML documentation identifier.
- `href` — The resolved URL when successful.

**Returns**

`true` when a non-empty URL was resolved.
