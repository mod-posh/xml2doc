# IExternalSymbolResolver

Resolves XML documentation identifiers to external documentation URLs.

<a id="xml2doc.core.linking.iexternalsymbolresolver.tryresolve(string,string@)"></a>

## Method: TryResolve(string, string@)

Attempts to resolve an XML documentation identifier such as `T:System.String` to an absolute or site-relative URL.

**Parameters**

- `cref` — The complete XML documentation identifier.
- `href` — The resolved URL when successful.

**Returns**

`true` when a non-empty URL was resolved.
