[LLAMARC42-METADATA]
Type: Glossary

Concepts: [
  "XML documentation",
  "Markdown",
  "cref",
  "anchor",
  "RendererOptions",
  "per-type mode",
  "single-file mode",
  "fingerprint",
  "XMember",
  "doc ID",
  "ILinkResolver",
  "AnchorAlgorithm",
  "FileNameMode"
]

Scope: System

Confidence: Observed

Source: [
  "code",
  "docs"
]
[/LLAMARC42-METADATA]

# Glossary

This glossary defines canonical terms used throughout the xml2doc project documentation. Terms are listed alphabetically.

---

## AnchorAlgorithm

An enum in `Xml2Doc.Core` that controls how heading text is converted to URL fragment slugs. Options:

| Value | Behavior |
|-------|----------|
| `Default` | Lowercase; whitespace → dash; strip non-`[a-z0-9-]`; collapse multi-dash runs |
| `Github` (= `Gfm`) | Unicode normalize → remove diacritics → lowercase → whitespace to dash → strip non-`[a-z0-9-]` |
| `Kramdown` | Like GitHub but retains underscores |

See also: [Anchor](#anchor), [HeadingSlug](#headingslug)

---

## Anchor

A URL fragment identifier (e.g., `#method-render`) embedded in a Markdown file. xml2doc emits two kinds:

- **Member anchors:** Computed by `IdToAnchor` from the member's doc ID. Stable and explicit.
- **Heading anchors:** Computed by `HeadingSlug` from the heading text. Algorithm-selectable via `AnchorAlgorithm`.

Anchors are part of the public contract (ADR-006). Changing them is a breaking change.

---

## cref

A documentation attribute (`cref="T:Ns.Type"` or `cref="M:Ns.Type.Method(...)"`) used in XML doc tags (`<see>`, `<seealso>`, `<inheritdoc>`) to reference other types or members. The format is `Kind:FullyQualifiedId`.

See also: [Doc ID](#doc-id), [ILinkResolver](#ilinksolver)

---

## DefaultLinkResolver

The internal concrete implementation of `ILinkResolver`. Constructed by `MarkdownRenderer` using four delegates: `labelFromCref`, `idToAnchor`, `typeFileName`, `headingSlug`.

---

## DevelopmentDependency

A NuGet package property (`<DevelopmentDependency>true</DevelopmentDependency>`) that marks a package as a build tool only. It does not appear in the transitive dependency graph of consuming projects. `Xml2Doc.MSBuild` uses this property.

---

## Doc ID

The fully qualified string used by the C# compiler to identify a documented member in the XML output. Format: `Kind:FullyQualifiedMemberName`. Examples:
- `T:MyLib.Widget` — a type
- `M:MyLib.Widget.Render(System.String)` — a method
- `P:MyLib.Widget.Name` — a property
- `F:MyLib.Widget.DefaultValue` — a field
- `E:MyLib.Widget.Changed` — an event

See also: [cref](#cref), [XMember](#xmember)

---

## Fingerprint

A hash value stored in a file between builds by the MSBuild task. Computed from the SHA-256 of the XML documentation file plus a serialization of all effective `RendererOptions`. Used to skip regeneration when inputs have not changed.

See also: [Incremental Build](#incremental-build)

---

## FileNameMode

An enum in `Xml2Doc.Core` that controls how output `.md` filenames are derived from doc IDs.

| Value | Behavior | Example |
|-------|----------|---------|
| `Verbatim` | Preserve doc ID as filename (including generic arity) | `MyLib.Widget\`1.md` |
| `CleanGenerics` | Remove generic arity from filename | `MyLib.Widget.md` |

---

## GenerateMarkdownFromXmlDoc

The MSBuild `Task` class defined in `Xml2Doc.MSBuild`. It is invoked by the `Xml2Doc_Generate` target. It receives MSBuild properties, constructs `RendererOptions`, and calls Core.

---

## HeadingSlug

An internal method in `MarkdownRenderer` that converts heading text to a URL-safe fragment using the selected `AnchorAlgorithm`. Used for type heading anchors in single-file mode.

---

## IdToAnchor

An internal method in `MarkdownRenderer` that converts a member doc ID (without the `X:` prefix) to a stable anchor string. Applies token-aware aliasing, `{}` → `[]` normalization, and lowercasing. Used for explicit member anchors.

---

## Incremental Build

The behavior of the MSBuild task that skips documentation regeneration when the fingerprint of the XML file and options is unchanged from the previous build. Implemented via `Xml2Doc_ComputeFingerprint` target and stamp file.

---

## InheritDocResolver

An internal static class in `Xml2Doc.Core` that resolves `<inheritdoc>` tags. Uses a heuristic to find matching members in the same XML model (not across assemblies). Merges inherited content into the current member's `XElement`.

---

## ILinkResolver

An internal interface in `Xml2Doc.Core.Linking` that defines a single method: `Resolve(string cref, LinkContext context) → MarkdownLink`. Decouples link strategy from the renderer. Implemented by `DefaultLinkResolver`. Confirmed stable.

---

## LinkContext

An internal `sealed record` that carries ambient context for link resolution: `CurrentTypeId`, `SingleFile` (bool), and `BasePath` (optional string for multi-level directory structures).

---

## MarkdownLink

An internal `sealed record` that represents the result of link resolution: `Href` (the URL fragment or file path) and `Label` (the display text).

---

## MarkdownRenderer

The public class in `Xml2Doc.Core` that orchestrates Markdown generation. Takes a `Xml2Doc` model and `RendererOptions`. Exposes `RenderToDirectory`, `RenderToSingleFile`, `RenderToString`, and `PlanOutputs`.

---

## Member

A documented .NET entity represented by an entry in the XML documentation file. Members have a Kind (`T`, `M`, `P`, `F`, `E`, `N`) and a doc ID. Represented in the model as `XMember`.

---

## Overload Grouping

The behavior of `MarkdownRenderer` where multiple method overloads with the same name are rendered under a single heading, with individual overload signatures listed as bullets.

---

## Per-Type Mode

Output mode where `MarkdownRenderer` writes one `.md` file per documented type plus an `index.md`. In this mode, `<see cref>` links resolve to `TypeFile.md#anchor`.

---

## RendererOptions

A `sealed record` in `Xml2Doc.Core` that encapsulates all rendering configuration. Passed from hosts (CLI, MSBuild) to `MarkdownRenderer`. Immutable by design. Some fields are declared but not yet implemented (future work).

---

## Single-File Mode

Output mode where `MarkdownRenderer` writes all documented types into one consolidated `.md` file. In this mode, `<see cref>` links resolve to in-document anchors (`#slug`).

---

## Stamp File

A file written by the MSBuild task after successful generation. Used by MSBuild's own incremental tracking mechanism to avoid re-running targets when outputs are up to date.

---

## Token-Aware Aliasing

A technique in `IdToAnchor` and `ShortLabelFromCref` that substitutes framework type names with C# keyword aliases (e.g., `System.String` → `string`) while preserving identifiers that contain those strings as substrings (e.g., `StringComparer` is not altered).

---

## XMember

A `sealed record` in `Xml2Doc.Core.Models` that represents one documented member from the XML file. Fields: `Name` (full doc ID string), `Element` (raw `XElement`), `Kind` (single character), `Id` (identifier portion after the colon).

---

## Xml2Doc (model class)

The top-level model class in `Xml2Doc.Core`. Loaded via `Xml2Doc.Load(string xmlPath)`. Contains a `Dictionary<string, XMember>` of all members indexed by their doc ID.

---

## xml2doc (tool)

The command-line tool produced by `Xml2Doc.Cli`. Invoked as `xml2doc --xml <path> --out <dir>` (or similar). Targets `net8.0` and `net9.0`.

---

## Xml2Doc_ComputeFingerprint

An MSBuild target (AfterTargets: `CoreCompile`) that computes the fingerprint and decides whether to skip generation.

---

## Xml2Doc_Generate

An MSBuild target (DependsOnTargets: `Xml2Doc_ComputeFingerprint`) that invokes `GenerateMarkdownFromXmlDoc` and writes output files.

> **Cross-reference:** All other documents in `/project/`
