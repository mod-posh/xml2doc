# HTTP_Parser_v2

Demonstration types whose names exercise different slug/anchor algorithms (Default, Github/Gfm, Kramdown).

**Remarks**

Each class name intentionally includes characters or patterns that various slug generators treat differently:

Mixed case + underscores + digits (). Nested type (introduces a '+' in doc IDs) with generic arity and double underscores (). Sequences of underscores in a single identifier (). Diacritics / accented characters (). Plain ASCII baseline ().

Use these to visually compare generated anchors under different `AnchorAlgorithm` values.

<a id="xml2doc.sample.http_parser_v2.go"></a>
## Method: Go
Trivial member; included so the type has at least one documented method anchor.

