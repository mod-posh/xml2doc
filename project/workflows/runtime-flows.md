[LLAMARC42-METADATA]
Type: Workflow

Concepts: [
  "runtime flow",
  "XML loading",
  "Markdown rendering",
  "link resolution",
  "fingerprinting",
  "incremental build"
]

Scope: System

Confidence: Observed

Source: [
  "code",
  "docs"
]
[/LLAMARC42-METADATA]

# Runtime Flows

## Overview

This document describes the end-to-end execution flows for each entry point: CLI and MSBuild. Both flows share the Core rendering pipeline but differ in how inputs are collected and how outputs are managed.

---

## Flow 1: CLI Invocation

```mermaid
sequenceDiagram
    participant User
    participant CLI as Xml2Doc.Cli
    participant Core as Xml2Doc.Core
    participant FS as File System

    User->>CLI: xml2doc --xml api.xml --out docs/

    CLI->>CLI: Parse CLI arguments
    CLI->>FS: Load JSON config (if --config provided)
    CLI->>CLI: Merge args + config → RendererOptions

    CLI->>Core: Xml2Doc.Load("api.xml")
    Core->>FS: Read XML file
    Core-->>CLI: Xml2Doc model (Dictionary<string, XMember>)

    CLI->>Core: new MarkdownRenderer(model, options)
    Core-->>CLI: renderer instance

    alt --dry-run
        CLI->>Core: PlanOutputs(outDir)
        Core-->>CLI: List<string> planned files
        CLI->>User: Print planned files, exit 0
    else per-type mode
        CLI->>Core: RenderToDirectory(outDir)
        Core->>FS: Write TypeName.md per type
        Core->>FS: Write index.md
    else single-file mode
        CLI->>Core: RenderToSingleFile(outFile)
        Core->>FS: Write combined.md
    end

    opt --report provided
        CLI->>FS: Write xml2doc-report.json
    end

    CLI->>User: Exit code 0
```

### Key Data Transformations

| Stage | Input | Output |
|-------|-------|--------|
| XML loading | `.xml` file (compiler output) | `Dictionary<string, XMember>` |
| Options resolution | CLI args + optional JSON | `RendererOptions` (immutable record) |
| Rendering | Model + options | `.md` files |
| cref resolution | `cref` string + `LinkContext` | `MarkdownLink` (href + label) |
| Anchor computation | Member ID string | Anchor fragment string |

---

## Flow 2: MSBuild Integration

```mermaid
sequenceDiagram
    participant MSBuild
    participant Target as Xml2Doc_ComputeFingerprint
    participant Task as GenerateMarkdownFromXmlDoc
    participant Core as Xml2Doc.Core
    participant FS as File System

    MSBuild->>MSBuild: CoreCompile completes
    MSBuild->>Target: Xml2Doc_ComputeFingerprint

    Target->>FS: Read XML file → SHA-256 hash
    Target->>Target: Combine hash + serialized options → fingerprint
    Target->>FS: Compare with stored .fingerprint file

    alt Fingerprint unchanged
        Target-->>MSBuild: Skip generation
    else Fingerprint changed
        MSBuild->>Task: Xml2Doc_Generate → GenerateMarkdownFromXmlDoc

        Task->>Core: Xml2Doc.Load(XmlPath)
        Core->>FS: Read XML file
        Core-->>Task: Xml2Doc model

        Task->>Core: new MarkdownRenderer(model, options)
        Task->>Core: RenderToDirectory or RenderToSingleFile
        Core->>FS: Write .md files

        opt ReportPath set
            Task->>FS: Write xml2doc-report.json
        end

        Task->>FS: Update .fingerprint file
        Task->>FS: Write .stamp file
        Task-->>MSBuild: GeneratedFiles[] output items
    end
```

---

## Core Rendering Pipeline (Shared)

Both CLI and MSBuild flows invoke the same Core rendering pipeline once `MarkdownRenderer` is constructed.

```mermaid
flowchart TD
    A[MarkdownRenderer constructed] --> B[Determine output mode]
    B -->|Per-type| C[Group members by type]
    B -->|Single-file| D[Flatten all members]

    C --> E[For each type: RenderType]
    D --> E

    E --> F[Resolve inheritdoc]
    F --> G[Render XML tags to Markdown]
    G --> H[Normalize whitespace and paragraphs]
    H --> I[Resolve crefs to Markdown links]
    I --> J[Compute member anchors]
    J --> K[Group overloads]
    K --> L[Write or append output]
```

### cref Resolution Detail

When the renderer encounters a `<see cref="...">` or `<seealso cref="...">` tag:

1. `DefaultLinkResolver.Resolve(cref, context)` is called
2. The kind character (`T`, `M`, `P`, etc.) determines link strategy
3. In per-type mode:
   - Type (`T:`) → `TypeFile.md`
   - Member (`M:`/`P:`/etc.) → `TypeFile.md#member-anchor`
4. In single-file mode:
   - Type → `#heading-slug`
   - Member → `#member-anchor`
5. Label is derived from the cref with alias substitution and namespace trimming applied

### Anchor Computation Detail

Two anchor functions are used:

| Function | Input | Output | Used For |
|----------|-------|--------|----------|
| `IdToAnchor` | Member doc ID (no `X:` prefix) | `method-membername-param1-param2` | Explicit member anchors |
| `HeadingSlug` | Heading text string | Slug per `AnchorAlgorithm` setting | Type heading anchors (single-file) |

`IdToAnchor` applies:
- Token-aware alias substitution (`System.String` → `string`, but `StringComparer` unchanged)
- `{}` → `[]` (generic brace normalization)
- Lowercase

---

## InheritDoc Resolution

Before rendering any member, `InheritDocResolver` is consulted if `<inheritdoc>` is present:

1. If `cref` attribute is set on `<inheritdoc>`, the model is looked up directly by that key
2. Otherwise, the resolver trims type ID segments heuristically to find matching members in parent types within the same XML model
3. Resolved content is merged into the member's `XElement` (existing author content is never overwritten)

**Limitation:** Cross-assembly inheritance is not resolved. Only members present in the same XML file are candidates.

> **Cross-reference:** [workflows/key-scenarios.md](key-scenarios.md) · [components/core.md](../components/core.md)
