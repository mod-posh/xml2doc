# HTTP_Parser_v2

Demonstration types whose names exercise different slug/anchor algorithms (Default, Github/Gfm, Kramdown).

**Remarks**

Each class name intentionally includes characters or patterns that various slug generators treat differently:

- Mixed case + underscores + digits ([HTTP_Parser_v2](Xml2Doc.Sample.HTTP_Parser_v2.md)).
- Nested type (introduces a '+' in doc IDs) with generic arity and double underscores ([Inner_Type__Beta2<T1>](Xml2Doc.Sample.Outer.Inner_Type__Beta2__1.md)).
- Sequences of underscores in a single identifier ([Name__With__Many___Underscores](Xml2Doc.Sample.Name__With__Many___Underscores.md)).
- Diacritics / accented characters ([RésuméParser](Xml2Doc.Sample.RésuméParser.md)).
- Plain ASCII baseline ([SimpleType](Xml2Doc.Sample.SimpleType.md)).

Use these to visually compare generated anchors under different `AnchorAlgorithm` values.

<a id="xml2doc.sample.http_parser_v2.go"></a>
## Method: Go
Trivial member; included so the type has at least one documented method anchor.
