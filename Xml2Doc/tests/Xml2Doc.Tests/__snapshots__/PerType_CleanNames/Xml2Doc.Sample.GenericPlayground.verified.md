# GenericPlayground

Generic playground for exercising nested generic parameter lists.

<a id="xml2doc.sample.genericplayground.flatten(system.collections.generic.ienumerable[system.collections.generic.ienumerable[xml2doc.sample.xitem]])"></a>
## Method: Flatten(IEnumerable<IEnumerable<XItem>>)
Flattens a nested sequence.

**Remarks**

This used to surface a stray brace in signatures like `IEnumerable{IEnumerable{XItem}}`. The renderer must output `IEnumerable<IEnumerable<XItem>>` (no extra `}`). See also [Flatten(System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<Xml2Doc.Sample.XItem>>)](Xml2Doc.Sample.GenericPlayground.md#xml2doc.sample.genericplayground.flatten(system.collections.generic.ienumerable[system.collections.generic.ienumerable[xml2doc.sample.xitem]])).

<a id="xml2doc.sample.genericplayground.index(system.collections.generic.dictionary[string,system.collections.generic.list[xml2doc.sample.xitem]])"></a>
## Method: Index(Dictionary<string, List<XItem>>)
Builds an index over a nested structure.

**Remarks**

Signature includes `Dictionary{string, List{XItem}}` which must render as `Dictionary<string, List<XItem>>` (no stray braces).

**Parameters**
- `map` — Nested map to index.

**Returns**

Total number of leaf items.

<a id="xml2doc.sample.genericplayground.transform``2(system.collections.generic.list[system.collections.generic.dictionary[``0,system.collections.generic.list[``1]]])"></a>
## Method: Transform<T1,T2>(List<Dictionary<T1, List<T2>>>)
Tests generic method arity formatting and nested generic parameters.

