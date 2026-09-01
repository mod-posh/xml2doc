# MetadataCollection

Provides an immutable, ordinally keyed snapshot of deterministic metadata values.

**Remarks**

Values may be `null`, strings, booleans, numeric values, dates, enums, or recursively nested lists of those scalar values. Object-valued metadata is not supported.

<a id="xml2doc.core.metadatacollection.#ctor(system.collections.generic.ienumerable[system.collections.generic.keyvaluepair[string,object]])"></a>

## Method: #ctor(IEnumerable<KeyValuePair<string, object>>)

Creates an immutable snapshot of caller-supplied metadata.

**Parameters**

- `values` — Metadata values keyed using ordinal semantics.

<a id="xml2doc.core.metadatacollection.containskey(string)"></a>

## Method: ContainsKey(string)

<a id="xml2doc.core.metadatacollection.count"></a>

## Property: Count

<a id="xml2doc.core.metadatacollection.empty"></a>

## Property: Empty

Gets an empty metadata collection.

<a id="xml2doc.core.metadatacollection.equals(xml2doc.core.metadatacollection)"></a>

## Method: Equals(MetadataCollection)

<a id="xml2doc.core.metadatacollection.equals(object)"></a>

## Method: Equals(object)

<a id="xml2doc.core.metadatacollection.getenumerator"></a>

## Method: GetEnumerator

<a id="xml2doc.core.metadatacollection.gethashcode"></a>

## Method: GetHashCode

<a id="xml2doc.core.metadatacollection.item(string)"></a>

## Property: Item(string)

<a id="xml2doc.core.metadatacollection.keys"></a>

## Property: Keys

<a id="xml2doc.core.metadatacollection.parsejson(string)"></a>

## Method: ParseJson(string)

Parses a JSON object into an immutable metadata collection.

**Parameters**

- `json` — JSON object containing scalar or list metadata values.

**Returns**

The parsed immutable metadata collection.

<a id="xml2doc.core.metadatacollection.system#collections#ienumerable#getenumerator"></a>

## Method: System#Collections#IEnumerable#GetEnumerator

<a id="xml2doc.core.metadatacollection.trygetvalue(string,object@)"></a>

## Method: TryGetValue(string, object@)

<a id="xml2doc.core.metadatacollection.values"></a>

## Property: Values
