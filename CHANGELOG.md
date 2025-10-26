# Changelog

All changes to this project should be reflected in this document.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.3.1](https://github.com/mod-posh/Xml2Doc/releases/tag/v1.3.1) - 2025-10-26

Small bugfix release focused on fixing broken internal links and tightening anchor/label consistency. No new features.

### Fixed

* Per‑type member links now point to the correct type page + member anchor.
  * Previously, links could truncate to the first namespace segment (e.g., `Xml2Doc.md#...`). We now derive the containing type via the last dot before the parameter list, producing correct targets like `Xml2Doc.Core.MarkdownRenderer.md#method-rendertodirectorystring`.
* Anchor/href matching for nested generics
  * Link fragments for complex signatures now exactly match emitted anchors, including the closing `)`, and with `{}` normalized to `[]` and C# aliases applied.
* Method generic arity in labels
  * `ShortLabelFromCref` now renders method generic arity tokens (e.g., ````1`` → `<T1>`) so labels like `Transform<T1>(...)` align with expectations and tests.

### Internal

* Refined `CrefToMarkdown` for member link target calculation (use last dot before `(`).
* Kept anchors stable via `IdToAnchor` (token‑aware aliasing, `{}` → `[]`, lowercased).
* Added and hardened tests for internal linking across per‑type/single‑file modes and nested generics.
* Removed unused usings and performed minor code hygiene across Core/CLI/MSBuild.

---

## [1.3.0](https://github.com/mod-posh/Xml2Doc/releases/tag/v1.3.0) - 2025-10-26

This release focuses on correctness and predictability: stable links/anchors across modes, depth‑aware generic formatting, and paragraph‑preserving normalization. It also expands tests and documentation accordingly.

### Added

* Token‑aware aliasing for framework types so identifiers like `StringComparer` remain intact while true tokens (e.g., `System.String`) are aliased to C# keywords.
* Depth‑aware generic argument splitting in labels and headers. Nested generics like `Dictionary<string, List<Dictionary<string, int>>>` now render correctly in member headers and `<see/>` labels.
* Explicit member anchors are emitted consistently; in single‑file mode, types also have heading‑based anchors for reliable in‑document navigation.
* New sample type `Xml2Doc.Sample.AliasingPlayground` to validate token‑aware aliasing and signature rendering.
* New tests:
  * `AliasingTests` to ensure identifiers containing aliasable substrings are not corrupted.
  * `NormalizationTests` to verify paragraph preservation, intra‑line trimming, and code fence protection.
  * `NestedGenericsTests` to validate depth‑aware formatting in headers and labels.

### Changed

* XML → Markdown normalization:
  * Preserves paragraph breaks (blank lines) and code fences verbatim.
  * Collapses soft line wraps within a paragraph to a single space.
  * Trims excess spaces/tabs within lines and removes stray spaces before punctuation in prose.
* `ShortTypeDisplay` now recognizes constructed generic types and delegates to a depth‑aware formatter, applying aliases and trimming namespaces for compact display.
* Clarified link behavior:
  * Per‑type output links to files produced by `FileNameMode` and anchors within them.
  * Single‑file output links to heading slugs for types and explicit anchors for members.

### Fixed

* Nested generic `<see/>` labels no longer degrade into malformed text (e.g., eliminated artifacts like `Int32}}}`); labels now correctly apply aliases and trim namespaces.
* Prevented accidental alias replacement inside larger identifiers (e.g., `StringComparer` no longer becomes `stringComparer`).
* Stabilized Markdown output by trimming leading indentation in prose and composing paragraphs predictably, reducing snapshot churn.

### Internal

* `ApplyAliases` refactored to token‑aware regexes for both fully‑qualified (`System.String`) and short names (`String`).
* Consolidated generic formatting via `ShortenSignatureType` and used in more display paths.
* Snapshot seeds updated to include the new `AliasingPlayground` page and refreshed index; tests aligned with normalization behavior.

---

## [1.2.1](https://github.com/mod-posh/Xml2Doc/releases/tag/v1.2.1) - 2025-10-24

This release is a focused bugfix to clean up how nested generic types and parentheses render in Markdown, along with some related test and snapshot fixes. No new features — just making the existing behavior finally *correct*.

## Fixed

* **Trailing parentheses and braces**

  * Eliminated artifacts like `Int32)` and `XMember})` that appeared in method headers.
  * Signatures such as `Flatten(IEnumerable<IEnumerable<XItem>>)` now render with balanced angle brackets and no stray symbols.
* **Alias link formatting**

  * Fixed malformed reference links in alias lines — e.g.
    `Alias that calls [Add(int, int)](...)` now renders cleanly without extra parentheses.
* **Section parsing during tests**

  * Updated `RenderSnapshots` section extraction logic to properly capture multi-level headings in single-file outputs.
  * Prevents cases where `# Mathx` or similar headings couldn’t be found or truncated early.
* **Cross-platform snapshot consistency**

  * Line endings and spacing normalized across all verified markdowns.
  * Reduces false diffs when running tests on Linux or Windows.
* **Snapshot seed refresh**

  * `snapshot_seed.ps1` updated to regenerate reference docs with corrected renderer output.

## Internal

* Refined regex checks for generic parameter lists in `RenderSnapshots`.
* Cleaned up `MarkdownRenderer` short-name logic to remove lingering parentheses on primitive types.
* All snapshot tests (`Mathx`, `GenericPlayground`, `XItem`, and `index`) now pass consistently.

---

## [1.2.0](https://github.com/mod-posh/Xml2Doc/releases/tag/v1.2.0) - 2025-10-24

### Added

* **Grouped Members Rendering** — Members are now grouped by type (Methods, Properties, Events, etc.) for cleaner, more readable Markdown output.
* **Expanded Test Coverage** — Introduced a dedicated test project validating Core, CLI, and MSBuild output end-to-end.
* **CLI Experience Enhancements** — Added improved help text, descriptive flag output, and clearer validation for incorrect parameters.
* **Configuration Validation** — The CLI and MSBuild integrations now validate config files and gracefully handle missing or invalid values.
* **Markdown Rendering Improvements** — Better handling of `<typeparam>`, `<returns>`, and `<value>` elements, and more consistent formatting for parameter and return sections.
* **Improved Project Metadata** — Added uniform tags, repository URLs, and license metadata across all `.csproj` files.
* **Per-Type vs Single-File Output Consistency** — Both modes now share identical structure and styling rules for predictable output.
* **Enhanced Logging for MSBuild** — Output now includes clear file paths and generation summaries.

### Changed

* Refactored renderer to separate type-level and member-level rendering.
* Standardized internal naming conventions across Core, CLI, and MSBuild projects.
* Updated CLI argument parsing and error handling for better consistency.
* Cleaned up namespace trimming and display logic for more readable type names.
* Snapshot tests updated to reflect grouped output and new formatting rules.

### Fixed

* Fixed incorrect handling of certain XML tags (`<value>`, `<typeparam>`, `<returns>`).
* Resolved path issues when working with nested namespaces and relative output paths.
* Corrected CLI config parsing where missing keys would previously throw errors.
* Fixed inconsistent Markdown spacing between type and member documentation blocks.

---

## [1.1.0](https://github.com/mod-posh/Xml2Doc/releases/tag/v1.1.0) - 2025-10-24

### Added

* Support for `<remarks>`, `<example>`, `<seealso>`, `<exception>`, and `<inheritdoc/>` tags.
* Method overload grouping for cleaner, consolidated output.
* New CLI option `--config` to load JSON configuration files.
* New CLI options `--file-names` and `--single` for flexible output modes.
* Added `RendererOptions` class with more granular configuration (filename mode, namespace trimming, language, etc.).
* Added full **snapshot test suite** validating per-type and single-file output.
* Added new **MSBuild properties**:
  * `Xml2Doc_SingleFile`
  * `Xml2Doc_OutputFile`
  * `Xml2Doc_OutputDir`
  * `Xml2Doc_FileNameMode`
  * `Xml2Doc_RootNamespaceToTrim`
  * `Xml2Doc_CodeBlockLanguage`
* Added NuGet package metadata including license, icon, project URL, and README integration.

### Changed

* Updated all projects to target **.NET 9.0**.
* Improved display names for generic types and shortened namespace output.
* Standardized built-in type aliasing (`System.String` → `string`, etc.).
* Reorganized project structure for consistency across Core, CLI, and MSBuild.
* Cleaned up `.csproj` files and centralized shared properties into `Directory.Build.props`.

### Fixed

* Addressed `PackageTags` element issues in older project files.
* Resolved type display inconsistencies for nested generics and collections.
* Fixed missing README visibility in NuGet packages.

---

## [1.0.0](https://github.com/mod-posh/Xml2Doc/releases/tag/v1.0.0) - 2025-10-22

### Added

* Core library to load and render XML documentation to Markdown.
* CLI tool (`Xml2Doc.Cli`) with `--xml` and `--out` parameters.
* MSBuild integration task to auto-generate Markdown after build.
* Default per-type Markdown output with generated `index.md`.
* Initial .NET 8.0 target and project scaffolding.
