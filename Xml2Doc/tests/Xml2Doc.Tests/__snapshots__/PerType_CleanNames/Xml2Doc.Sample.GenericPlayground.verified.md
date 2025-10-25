# GenericPlayground

Generic playground for exercising nested generic parameter lists.

## Method: Flatten(IEnumerable<IEnumerable<XItem>>)
Flattens a nested sequence.

## Method: Index(Dictionary<string, List<XItem>>)
Builds an index over a nested structure.

**Parameters**
- `map` — Nested map to index.

**Returns**

Total number of leaf items.

## Method: Transform<T1,T2>(List<Dictionary<T1, List<T2>>>)
Tests generic method arity formatting and nested generic parameters.

