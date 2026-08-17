# Runtime Flows

## CLI single-input flow

```text
arguments / JSON config
        │
        ▼
CLI validation + option mapping
        │
        ▼
Xml2Doc.Load(primary.xml)
        │
        ├── LoadReferences(reference XML)
        ▼
MarkdownRenderer(RendererOptions)
        │
        ▼
RendererRunner / report pipeline
        │
        ▼
plan → render/compare → lifecycle → report
```

The runner centralizes planning and execution. `--dry-run` plans without writing; `--diff` compares without mutation; normal generation writes only changed outputs and performs lifecycle actions allowed by ownership state.

## CLI multi-input flow

```text
repeated --xml / JSON XmlInputs
        │
        ▼
canonicalize + de-duplicate primary paths
        │
        ▼
Xml2Doc.LoadAggregate(...)
        │
        ├── duplicate primary member → XML2DOC006
        ├── LoadReferences(reference XML)
        ▼
one aggregate MarkdownRenderer
        │
        ▼
one deterministic output set + index
```

Caller input order does not determine aggregate type/index ordering.

## Normal MSBuild flow

```text
CoreCompile
   │
   ├── compiler XML
   ├── automatic/explicit reference XML
   ▼
compute fingerprint
   │
   ├── validate recorded outputs
   ▼
GenerateMarkdownFromXmlDoc
   │
   ▼
Core model + renderer/runner
   │
   ▼
Markdown + report + stamp + output ledger
```

A changed compiler/reference input or significant option updates incremental state. A missing generated output recorded in the ledger invalidates the stamp and is recreated.

## MSBuild repository aggregation flow

```text
owner project
   │
   ├── validate referenced index ownership
   │      └── conflict → XML2DOC007
   │
   ├── ResolveReferences
   │      └── project-reference compiler XML
   │
   ├── explicit Xml2Doc_AggregateXml
   ├── optional Xml2Doc_ReferenceXml
   ▼
prepare aggregate fingerprint/lifecycle state
   │
   ▼
GenerateMarkdownFromXmlDocs
   │
   ▼
Xml2Doc.LoadAggregate(primary XML)
   │
   ▼
one deterministic aggregate output set
```

The owner uses separate `xml2doc.aggregate.*` incremental files from ordinary project generation.

## `<inheritdoc />` resolution

Primary and reference models participate in inheritance lookup. Project-reference XML is discovered automatically by MSBuild normal generation, and explicit reference XML can be supplied by either host. Referenced symbols support inheritance resolution without being promoted to primary generated pages.
