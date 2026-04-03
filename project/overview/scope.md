[LLAMARC42-METADATA]
Type: Overview

Concepts: [
  "scope",
  "non-goals",
  "XML documentation",
  "Markdown output",
  "static site generator"
]

Scope: System

Confidence: Observed

Source: [
  "docs",
  "code"
]
[/LLAMARC42-METADATA]

# Scope

## In Scope

The following capabilities are within the scope of xml2doc.

### Transformation

- Parsing `.xml` files produced by the C# compiler (`<GenerateDocumentationFile>true`)
- Rendering XML documentation members (`T:`, `M:`, `P:`, `F:`, `E:`, `N:` kinds) to Markdown
- Handling XML doc tags: `<summary>`, `<remarks>`, `<param>`, `<returns>`, `<exception>`, `<see>`, `<seealso>`, `<example>`, `<code>`, `<list>`, `<inheritdoc>`

### Output Modes

- **Per-type mode:** one `.md` file per documented type, plus `index.md`
- **Single-file mode:** all types combined into one `.md` file

### Rendering Options (Observed as Implemented)

The following `RendererOptions` fields are confirmed as implemented based on code inspection:

| Option | Behavior |
|--------|----------|
| `FileNameMode` | `Verbatim` or `CleanGenerics` (removes generic arity from filenames) |
| `RootNamespaceToTrim` | Strips namespace prefix from display labels |
| `TrimRootNamespaceInFileNames` | Also strips prefix from output filenames |
| `CodeBlockLanguage` | Language tag applied to fenced code blocks |
| `AnchorAlgorithm` | Slug algorithm: Default, GitHub/GFM, Kramdown |
| `EmitToc` | Per-type member table of contents |
| `EmitNamespaceIndex` | Namespace index pages |
| `BasenameOnly` | Drop namespace segments from file names |
| `ParallelDegree` | Max parallelism for rendering |

### Rendering Options (Declared, Not Yet Implemented)

The following options are declared in `RendererOptions` but are accepted as **future work**. They are not currently implemented in the renderer:

| Option | Declared Default | Notes |
|--------|-----------------|-------|
| `TemplatePath` | `null` | Wrapping template file |
| `FrontMatterPath` | `null` | Prepended YAML/TOML/JSON front matter |
| `AutoLink` | `false` | Heuristic auto-linking in prose. No implementation exists. |
| `AliasMapPath` | `null` | Custom type aliases from file |
| `ExternalDocs` | `null` | External documentation base URL |

> **Developer confirmation:** These options are declared as future work. They are accepted as part of the declared API surface but do not affect rendering behavior in the current version.

### Link Resolution

- Resolving `cref` attributes in XML to Markdown links
- Per-type links: type → `.md` file; member → `file.md#anchor`
- Single-file links: type → `#heading-slug`; member → `#explicit-anchor`
- `<inheritdoc>` resolution (heuristic, not reflection-based)

### Build Integration

- MSBuild incremental execution via fingerprinting
- JSON report output (`xml2doc-report.json`)
- Dry-run mode (plan outputs without writing)

### Diagnostics

- Diagnostic codes are intended to surface warnings and errors as CLI stderr output and MSBuild task output. This is a declared goal (ADR-009) currently in progress.

---

## Out of Scope

These capabilities are explicitly excluded per the project Constitution and ADR-001:

| Capability | Reason |
|-----------|--------|
| Static site generation | Not a documentation platform |
| Documentation hosting | Not a hosting service |
| Prose editing | Not an authoring tool |
| Reflection-based type resolution | Only compiler-emitted XML is consumed |
| Full `<inheritdoc>` type system traversal | Heuristic resolution only; no binary reflection |
| Auto-linking in prose | Declared but not implemented; no current code |
| SymbolIndex / two-phase pipeline | Does not exist; may be future architecture |

> **Cross-reference:** [introduction.md](introduction.md) · [risks/risks-and-technical-debt.md](../risks/risks-and-technical-debt.md)
