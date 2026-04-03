[LLAMARC42-METADATA]
Type: Component

Concepts: [
  "Xml2Doc.Core",
  "MarkdownRenderer",
  "RendererOptions",
  "ILinkResolver",
  "XMember",
  "rendering engine",
  "anchor algorithm"
]

Scope: Component

Confidence: Observed

Source: [
  "code",
  "docs"
]
[/LLAMARC42-METADATA]

# Component: Xml2Doc.Core

## Identity

| Property | Value |
|----------|-------|
| Assembly | `Xml2Doc.Core` |
| Type | NuGet library |
| Frameworks | `netstandard2.0`, `net8.0`, `net9.0` |
| NuGet package | `Xml2Doc.Core` |
| Version | 1.4.0 (current) |

## Role

Core is the sole rendering engine. It owns XML loading, Markdown generation, link resolution, anchor computation, signature formatting, and all rendering options. CLI and MSBuild are hosts that only translate their native inputs into `RendererOptions` and invoke Core.

## Public API Surface

### Types

#### `Xml2Doc` (class, sealed)

Entry point for loading a documentation model.

```csharp
public sealed class Xml2Doc
{
    public Dictionary<string, XMember> Members { get; }
    public static Xml2Doc Load(string xmlPath)
}
```

- `Load` reads the compiler-generated `.xml` file and returns a model with all documented members indexed by their doc ID (`"T:Ns.Type"`, `"M:Ns.Type.Method(...)"`).

#### `XMember` (record, sealed)

Represents one documented member.

```csharp
public sealed record XMember(string Name, XElement Element)
{
    public string Kind { get; }  // "T", "M", "P", "F", "E", "N"
    public string Id { get; }    // identifier after the colon
}
```

#### `MarkdownRenderer` (class, sealed)

The rendering engine.

```csharp
public sealed class MarkdownRenderer
{
    public MarkdownRenderer(Models.Xml2Doc model, RendererOptions? options = null)
    public void RenderToDirectory(string outDir)
    public void RenderToSingleFile(string outFile)
    public string RenderToString()
    public List<string> PlanOutputs(string outDir = "", string? singleFilePath = null)
}
```

#### `RendererOptions` (record, sealed)

All rendering configuration, immutable by design.

```csharp
public sealed record RendererOptions(
    FileNameMode FileNameMode = FileNameMode.Verbatim,
    string? RootNamespaceToTrim = null,
    string CodeBlockLanguage = "csharp",
    bool TrimRootNamespaceInFileNames = false,
    AnchorAlgorithm AnchorAlgorithm = AnchorAlgorithm.Default,
    string? TemplatePath = null,       // declared; not yet implemented
    string? FrontMatterPath = null,    // declared; not yet implemented
    bool AutoLink = false,             // declared; not yet implemented
    string? AliasMapPath = null,       // declared; not yet implemented
    string? ExternalDocs = null,       // declared; not yet implemented
    bool EmitToc = false,
    bool EmitNamespaceIndex = false,
    bool BasenameOnly = false,
    int? ParallelDegree = null
)
```

Options marked "declared; not yet implemented" are accepted as future work. They do not affect rendering behavior in the current version.

#### `FileNameMode` (enum)

```csharp
public enum FileNameMode
{
    Verbatim,      // preserve doc ID as-is (e.g., MyLib.Widget`1.md)
    CleanGenerics  // remove generic arity (e.g., MyLib.Widget.md)
}
```

#### `AnchorAlgorithm` (enum)

```csharp
public enum AnchorAlgorithm
{
    Default  = 0,  // lowercase, whitespace→dash, strip non [a-z0-9-]
    Github   = 1,  // Unicode normalize, remove diacritics, lowercase
    Kramdown = 2,  // like GitHub but preserves underscores
    Gfm      = 3   // alias of GitHub
}
```

## Internal Components (Not Public API)

| Type | Role |
|------|------|
| `ILinkResolver` | Interface for cref → Markdown link resolution |
| `DefaultLinkResolver` | Concrete resolver; uses four delegates from `MarkdownRenderer` |
| `LinkContext` | Ambient context: current type, single-file flag, base path |
| `MarkdownLink` | Result: href + label |
| `InheritDocResolver` | Heuristic `<inheritdoc>` resolution |
| `NetStandard20Compat` | Polyfills for NS2.0 |

`ILinkResolver` is internal and is confirmed **stable** in its current form. It is not part of the public API and is not designed for external extensibility at this time.

## Rendering Behaviors

### Overload Grouping

Methods with the same name are grouped under one heading. Individual overloads are listed as bullets beneath the heading with their full signatures.

### XML Tag Handling

| XML Tag | Markdown Output |
|---------|----------------|
| `<summary>` | Body text |
| `<remarks>` | Italic note block |
| `<param>` | Parameter table row |
| `<returns>` | Returns section |
| `<exception>` | Exception table row |
| `<see cref="...">` | Resolved Markdown link |
| `<seealso cref="...">` | See Also section link |
| `<example>` | Example section |
| `<code>` | Fenced code block with `CodeBlockLanguage` |
| `<list>` | Markdown list (bullet or numbered) |
| `<inheritdoc>` | Resolved and merged from parent member |

### Normalization

- Blank lines between paragraphs are preserved
- Intra-line whitespace is collapsed in prose
- Stray spaces before punctuation are removed
- Inline `<c>` is rendered as backtick code
- Fenced code blocks preserve verbatim content

### Anchor Computation

Two anchor functions:
- `IdToAnchor(string id)` — for member anchors: token-aware aliasing, `{}` → `[]`, lowercase
- `HeadingSlug(string text)` — for heading anchors: applies selected `AnchorAlgorithm`

## Dependencies

| Package | Version | Scope |
|---------|---------|-------|
| `System.Text.Json` | 8.0.5 | All TFMs |
| `System.Text.Encodings.Web` | 8.0.0 | All TFMs |
| `System.Buffers` | 4.5.1 | NS2.0 only |
| `System.Memory` | 4.5.5 | NS2.0 only |
| `System.Numerics.Vectors` | 4.5.0 | NS2.0 only |
| `System.Runtime.CompilerServices.Unsafe` | 6.0.0 | NS2.0 only |

> **Cross-reference:** [architecture/component-view.md](../architecture/component-view.md) · [components/cli.md](cli.md) · [components/msbuild.md](msbuild.md)
