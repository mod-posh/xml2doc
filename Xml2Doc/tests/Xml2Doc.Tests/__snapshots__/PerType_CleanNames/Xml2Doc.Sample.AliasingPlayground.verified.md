# AliasingPlayground

Types used to validate token-aware aliasing.

<a id="xml2doc.sample.aliasingplayground.mix(string,int,uint)"></a>

## Method: Mix(string, int, uint)

Mixes aliasable tokens with non-aliasable substrings so the renderer aliases true tokens while leaving larger identifiers intact.

**Parameters**

- `s` — Example string.
- `x` — An example Int32 value.
- `y` — An example UInt32 value.

<a id="xml2doc.sample.aliasingplayground.usecomparer(system.stringcomparer)"></a>

## Method: UseComparer(StringComparer)

Accepts a non-aliased BCL type that contains "String" as a substring. Used to ensure token-aware aliasing does not transform "StringComparer".

**Parameters**

- `comparer` — Comparer instance.
