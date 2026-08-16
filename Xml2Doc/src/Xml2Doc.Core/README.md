# Xml2Doc.Core

Core library for the Xml2Doc toolset (part of **mod-posh**).
Now multi-targeted and verified for consistent output across modern .NET TFMs.

## Overview

`Xml2Doc.Core` is the engine behind the Xml2Doc CLI and MSBuild task. It parses XML doc comments, resolves references, and renders clean, linkable Markdown—either one file per type or a single combined document.

## Features

- Parses common XML doc elements: `<summary>`, `<remarks>`, `<example>`, `<seealso>`, `<exception>`, `<inheritdoc/>`.
- Converts `<see>`, `<paramref>` into inline Markdown links and code spans.
- Cleans namespaces and shortens generics (`List<T>` vs `System.Collections.Generic.List<T>`).
- Built-in type aliasing (`System.String` → `string`, etc.) without breaking identifiers (`StringComparer` remains intact).
- Optional `IAliasProvider` injection for consumer-defined signature, label, and anchor aliases.
- Overload grouping for cleaner member sections.
- Two output modes:
  - **Per-type** (`RenderToDirectory`) → `TypeName.md` with in-file member anchors.
  - **Single-file** (`RenderToSingleFile` / `RenderToString`) → one Markdown with headings + explicit anchors.
- Stable, explicit anchors for members; GitHub-style heading slugs for types (single-file).
- Depth-aware generic formatting (correctly renders nested generics).
- Paragraph- and code-block–preserving normalization.
- Configuration via `RendererOptions`:
  - Filename mode: `Verbatim` or `CleanGenerics`
  - `RootNamespaceToTrim` (display-only trimming)
  - Code block language (default `csharp`)
  - Per-type `index.md` generation (`GenerateIndex`, default `true`)
  - Markdown line endings (`Lf` default, `CrLf`, or `Native`)
  - Output mode (single vs. multi-file)

## Supported Target Frameworks

Shipped TFMs:

- `netstandard2.0` — broad library reach
- `net8.0`
- `net9.0`

**Guarantee:** Default Markdown output is deterministic and equivalent across these TFMs and hosts,
including LF line endings. We test cross-TFM output directly without relying on post-render newline
normalization.

### Language-version notes

- `netstandard2.0`: Source uses C# 10 syntax (file-scoped namespaces/global usings) but avoids runtime-only APIs not present in NS2.0. Where needed, we use compatibility shims and alternate overloads (e.g., prefer `string.Split(char, StringSplitOptions)` and avoid `Index`/`Range` in hot paths).
- `net8.0` / `net9.0`: SDK defaults.

## Behavior Guarantees Across TFMs

- **Anchors/slugs:** Identical across TFMs and modes.
- **Linking:** Member `cref` targets resolve to owning type pages (per-type) or in-document anchors (single-file).
- **Formatting:** Built-in aliases, generic labels, paragraph and code-block handling match exactly.

## Compatibility (netstandard2.0)

To keep `netstandard2.0` first-class:

- Removed reliance on APIs absent in NS2.0 (`AsSpan`, `Range`/`Index`, certain `Split` overloads).
- Added targeted nullability guards where analyzers flagged potential issues (no behavior changes).
- Kept rendering logic identical to newer TFMs.

## Anchors & Link Behavior

- **Per-type (`RenderToDirectory`)**
  - Types render to `*.md` files (filename strategy controlled by `FileNameMode`).
  - Members link to **anchors inside the type file** (e.g., `Type.md#member-anchor`).

- **Single-file (`RenderToSingleFile` / `RenderToString`)**
  - Types get **heading-based slugs** (GitHub-style).
  - Members get **explicit anchors** in the combined document.

## Example

```csharp
using Xml2Doc.Core;

var model = XmlDocModel.Load("MyLibrary.xml");

var options = new RendererOptions(
    FileNameMode: FileNameMode.CleanGenerics,
    RootNamespaceToTrim: "MyCompany.MyProduct",
    CodeBlockLanguage: "csharp"
);

var renderer = new MarkdownRenderer(model, options);

// Per-type output
renderer.RenderToDirectory("./docs");

// Single-file output
renderer.RenderToSingleFile("./docs/api.md");
````

Custom aliasing is opt-in and uses the same provider for visible signatures, link labels, and member
anchors so link targets remain aligned:

```csharp
using Xml2Doc.Core.Aliasing;

var options = new RendererOptions(
    AliasProvider: new MyAliasProvider());
```

Implement `IAliasProvider.ApplyAliases` with token-aware replacements. Omitting the provider uses
`DefaultAliasProvider.Instance` and preserves the existing C# keyword mappings.

Anchor generation is also replaceable while the built-in `AnchorAlgorithm` values remain the
default:

```csharp
using Xml2Doc.Core.Anchoring;

var options = new RendererOptions(
    AnchorGenerator: new MyAnchorGenerator());
```

Implement both `GenerateHeadingAnchor` and `GenerateMemberAnchor`. Xml2Doc uses the selected
provider for emitted anchors and internal link targets, keeping them aligned in per-type and
single-file output.

Templates and front matter are optional. A file template must contain `{{content}}`; it may also
use `{{title}}` and `{{kind}}`. Front matter is prepended verbatim, so include delimiters such as
`---` in the front-matter file when targeting YAML-aware documentation systems:

```csharp
var options = new RendererOptions(
    TemplatePath: "templates/page.md",
    FrontMatterPath: "templates/front-matter.yml");
```

For programmatic layouts, implement `ITemplateRenderer.Render` and pass it through
`TemplateRenderer`. Programmatic renderers cannot be combined with the file-based options.
Omitting all template options preserves the built-in Markdown byte-for-byte.

## Tests & Snapshots

- Snapshot tests assert stable Markdown for representative inputs.
- Cross-TFM test renders via CLI for `net8.0` and `net9.0` and compares outputs (with EOL normalization).
- A Windows-only, opt-in check ensures the MSBuild task graph stays healthy (especially P2P TFM mapping).

## Related Work / Issues

- **#33** — Multi-framework support: Core (`netstandard2.0;net8.0;net9.0`), CLI (`net8.0;net9.0`), MSBuild task (`net472;net8.0`).
- **#46** — `netstandard2.0` compatibility: removed `Index`/`Range` usages, updated `Split` usage, and added nullability guards.

## Maintenance & Support

- Tracks current .NET (e.g., `net8.0`, `net9.0`) while keeping `netstandard2.0` for broad compatibility.
- If you notice any cross-TFM drift in output, please open an issue with a minimal XML sample plus expected vs. actual Markdown.
