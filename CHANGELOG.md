# Changelog

All changes to this project should be reflected in this document.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [2.3.0](https://github.com/mod-posh/Xml2Doc/releases/tag/2.3.0) - 2026-08-17

### Added

- Deterministic Core aggregation for multiple primary XML documentation inputs.
- Repeated CLI `--xml` arguments and JSON `XmlInputs` for multi-project aggregation.
- Canonical aggregate `xmlInputs` in CLI and MSBuild reports.
- Opt-in MSBuild repository aggregation ownership through `Xml2Doc_AggregateEnabled=true`.
- Automatic aggregate XML discovery from resolved project references and explicit
  `Xml2Doc_AggregateXml` primary inputs.
- `XML2DOC006` for duplicate documentation-member ownership across primary aggregate inputs.
- `XML2DOC007` for conflicting MSBuild ownership of the aggregate `index.md`.
- Separate aggregate stamp, fingerprint, output ledger, and report lifecycle state.

### Changed

- Repository-level documentation can now be rendered once from all participating compiler XML
  files instead of merging independently generated indexes.
- Aggregate input ordering is canonical and ordinal, so caller/project scheduling order does not
  control generated type or index ordering.
- Aggregate incremental tracking includes primary/reference XML participation, significant render
  options, and the host newline token when `Xml2Doc_LineEndings=native`.
- `Xml2Doc_GenerateIndex=false` remains the supported compatibility mitigation for independent
  projects sharing an output directory without repository aggregation.

### Tests

- Added Core aggregation ordering, input-order independence, and duplicate-member tests.
- Added CLI aggregation, JSON precedence, report, and deterministic ordering integration coverage.
- Added Windows/Linux MSBuild aggregation coverage comparing parallel builds with `/m:1`
  byte-for-byte.
- Expanded package integration to build a repository aggregation owner from the packed
  `Xml2Doc.MSBuild` package.

---

## [2.2.0](https://github.com/mod-posh/Xml2Doc/releases/tag/2.2.0) - 2026-08-17

### Added

- Stable structured diagnostics across Core, CLI, and MSBuild, including severity, diagnostic IDs,
  source/member context, and host-specific warning/error mapping.
- Runner-backed planning, rendering, dry-run, diff, reporting, and output-lifecycle coordination.
- Bounded parallel per-type rendering with deterministic output ordering and bytes.
- Incremental writes that skip unchanged generated files.
- CLI support for templates, front matter, auto-linking, alias maps, external documentation,
  anchor algorithms, namespace indexes, TOCs, reports, parallelism, lifecycle controls, dry run,
  and diff behavior.
- Stable CLI diff exit code `3` when generated output differs from the current files.

### Changed

- CLI and MSBuild diagnostics use the same Core diagnostic contract.
- Reports distinguish planned, written, skipped, pruned, and non-mutating comparison results.
- Dry run, diff, pruning, reporting, and normal generation share one runner execution model.
- Invalid CLI/configuration values fail with actionable validation messages instead of being
  accepted silently.

### Tests

- Added serial/parallel parity coverage and unchanged-file write avoidance tests.
- Added diagnostic mapping and validation coverage across Core, CLI, and MSBuild.
- Added CLI flag/config precedence and smoke coverage for the expanded option surface.

---

## [2.1.0](https://github.com/mod-posh/Xml2Doc/releases/tag/2.1.0) - 2026-08-16

### Added

- Pluggable rendering services for Core consumers:
  - `IAnchorGenerator` for custom heading and member anchors.
  - `IAliasProvider` for configurable type-name aliases.
  - `ITemplateRenderer` and a per-document front-matter callback.
  - `IAutoLinker` for safe free-text symbol linking.
  - `IExternalSymbolResolver` and `LinkPolicy` for unresolved cref targets.
  - `ISignatureRenderer` and `SignatureStyle` for parameter names, generic constraints,
    default values, and indexer formatting.
- Built-in file templates and deterministic YAML front matter.
- Optional auto-linking that protects fenced code, inline code, existing links, and partial
  identifiers.
- External documentation fallback using a configurable base URL.

### Changed

- Rendering now routes anchors, aliases, links, templates, and signatures through the same
  replaceable services used by the built-in implementations.
- Default rendering remains compatible with the `2.0.x` baseline unless a new option or custom
  service is explicitly selected.

### Tests

- Added a per-type/single-file link-routing matrix for every built-in anchor algorithm.
- Added parity coverage for emitted anchors and generated link targets.
- Added integration coverage for every custom rendering service and auto-link safety boundaries.

---

## [2.0.3](https://github.com/mod-posh/Xml2Doc/releases/tag/2.0.3) - 2026-08-15

### Added

- Cross-project XML documentation discovery for `<inheritdoc />`, including automatic project
  reference XML loading and explicit `Xml2Doc_ReferenceXml` items.
- Portable output-ownership manifests that remain valid when a repository is moved to a different
  checkout path.
- Output ledgers that allow incremental MSBuild execution to detect and regenerate missing
  Markdown files.

### Fixed

- `<see langword="..."/>` now renders its language keyword correctly while preserving attribute
  precedence for existing `<see cref="..."/>` behavior.
- `<inheritdoc />` now resolves inherited documentation for unique members, overloads, interfaces,
  and referenced projects without mutating the source model.
- Missing generated Markdown invalidates the MSBuild output stamp and is recreated on the next
  build.
- Ownership manifests migrate safe `2.0.x` entries without authorizing traversal or deletion
  outside the current output root.

### Changed

- Manifest files are deterministic and portable; transaction directories remain local staging
  state and are removed after successful cleanup.
- Unresolved inheritance targets and missing explicitly configured reference XML files are reported
  as non-fatal MSBuild warnings.

---

## [2.0.2](https://github.com/mod-posh/Xml2Doc/releases/tag/2.0.2) - 2026-08-14

### Fixed

- Corrected the `Xml2Doc.MSBuild` NuGet layout so `Xml2Doc.Core.dll` and other required runtime
  dependencies are packaged beside the `net472` and `net8.0` task assemblies.
- Fixed clean-consumer builds that previously failed with `FileNotFoundException` when MSBuild
  loaded `Xml2Doc.Core`.
- Fixed package integration commands so they execute from the generated consumer project directory.
- Added the cross-platform `ZipFile` assembly loading required by package inspection.

### Tests

- Added package-layout assertions for both task target frameworks.
- Added a clean .NET 9 consumer integration test that restores only the local
  `Xml2Doc.MSBuild` package and verifies Markdown generation.
- Suppressed the expected NuGet dependency-group warning after validating the self-contained task
  package layout.

---

## [2.0.1](https://github.com/mod-posh/Xml2Doc/releases/tag/2.0.1) - 2026-08-14

### Changed

- Restricted NuGet publishing to milestone releases and tightened release-workflow permissions.
- Updated package versions to `2.0.1`.

### Known issue

- The published `Xml2Doc.MSBuild` package did not include `Xml2Doc.Core.dll` beside its task
  assemblies. Clean consumers could restore successfully but fail during `dotnet build`.
  This packaging defect was corrected in `2.0.2`.

---

## [2.0.0](https://github.com/mod-posh/Xml2Doc/releases/tag/2.0.0) - 2026-08-13

### Added

- Explicit `lf`, `crlf`, and `native` line-ending policies across Core, CLI, and MSBuild.
- `--line-endings` for the CLI and `Xml2Doc_LineEndings` for MSBuild consumers.
- Cross-platform tests covering line endings and UTF-8 output without a byte-order mark.

### Changed

- Generated Markdown now uses LF on every platform by default for deterministic output.
- Final output normalization is applied consistently across per-type and single-file modes.

### Breaking

- The default changed from platform-native line endings to LF. Windows consumers that require the
  previous byte-level behavior must select `crlf` or `native` explicitly.

---

## [1.4.0](https://github.com/mod-posh/Xml2Doc/releases/tag/1.4.0) - 2026-08-13

### Added

- **Multi-targeting across the solution**
  - **Core**: `netstandard2.0; net8.0; net9.0`
  - **CLI**: `net8.0; net9.0`
  - **MSBuild task**: `net472; net8.0` (host-selects automatically)
- **Visual Studio 2022 support** via `Xml2Doc.MSBuild (net472)` so the task runs inside VS/MSBuild.exe.
- **Cross-TFM consistency test**: builds CLI for `net8.0` and `net9.0`, renders both, and asserts identical Markdown (with EOL normalization).
- **Optional Windows-only build check** for the MSBuild task on `net472` to guard the P2P TFM mapping.
- **Snapshot seed script** now runs the **built CLI artifact** (chooses a produced TFM), avoiding `dotnet run` quirks.

### Changed

- **`Xml2Doc.Core` netstandard2.0 compatibility** (issues **#33**, **#46**):
  - Avoid APIs not present on NS2.0 (`AsSpan`, `Index`/`Range`, certain `Split` overloads).
  - Added targeted nullable guards where analyzers flagged potential dereferences (no behavior change).
  - Conditioned language version so NS2.0 builds can compile source using C# 10 syntax.
- **Build stability**:
  - Ensured scalar `OutputPath`/`IntermediateOutputPath` to prevent MSBuild seeing item lists (fixes `HasTrailingSlash` errors on multi-TFM).
  - Solution/test flow builds once, then tests run artifacts to reduce file-handle contention and intermittent copy retries.
- **Docs**:
  - Updated Core/CLI/MSBuild READMEs to document multi-TFM support, correct `dotnet` usage, and VS task hosting.

### Fixed

- **MSB3100** during CLI build/run with multi-TFM: removed cases where an MSBuild `<MSBuild ... Properties="...">` could receive a bare TFM (`net8.0`) instead of `name=value`. Tests and scripts now **run the built DLL** and (when building directly) use `-f` or solution-wide build to avoid malformed `Properties=`.
- Eliminated false analyzer warnings in Core for nullable flow around namespace trimming.

### Notes / Compatibility

- Output is **deterministic and equivalent** across Core TFMs. CLI output matches across `net8.0` / `net9.0` (enforced by tests).
- MSBuild task host selection:
  - **VS/MSBuild.exe** → `net472` task (maps to Core `netstandard2.0`)
  - **dotnet build** → `net8.0` task (maps to Core `net8.0`)
- Guidance: prefer running the **built CLI artifact** (`dotnet path\to\Xml2Doc.Cli.dll`) for repeatable results in CI and local scripts.

---

## [1.3.1](https://github.com/mod-posh/Xml2Doc/releases/tag/v1.3.1) - 2025-10-26

Small bugfix release focused on fixing broken internal links and tightening anchor/label consistency. No new features.

### Fixed

- Per‑type member links now point to the correct type page + member anchor.
  - Previously, links could truncate to the first namespace segment (e.g., `Xml2Doc.md#...`). We now derive the containing type via the last dot before the parameter list, producing correct targets like `Xml2Doc.Core.MarkdownRenderer.md#method-rendertodirectorystring`.
- Anchor/href matching for nested generics
  - Link fragments for complex signatures now exactly match emitted anchors, including the closing `)`, and with `{}` normalized to `[]` and C# aliases applied.
- Method generic arity in labels
  - `ShortLabelFromCref` now renders method generic arity tokens (e.g., ````1`` → `<T1>`) so labels like `Transform<T1>(...)` align with expectations and tests.

### Internal

- Refined `CrefToMarkdown` for member link target calculation (use last dot before `(`).
- Kept anchors stable via `IdToAnchor` (token‑aware aliasing, `{}` → `[]`, lowercased).
- Added and hardened tests for internal linking across per‑type/single‑file modes and nested generics.
- Removed unused usings and performed minor code hygiene across Core/CLI/MSBuild.

---

## [1.3.0](https://github.com/mod-posh/Xml2Doc/releases/tag/v1.3.0) - 2025-10-26

This release focuses on correctness and predictability: stable links/anchors across modes, depth‑aware generic formatting, and paragraph‑preserving normalization. It also expands tests and documentation accordingly.

### Added

- Token‑aware aliasing for framework types so identifiers like `StringComparer` remain intact while true tokens (e.g., `System.String`) are aliased to C# keywords.
- Depth‑aware generic argument splitting in labels and headers. Nested generics like `Dictionary<string, List<Dictionary<string, int>>>` now render correctly in member headers and `<see/>` labels.
- Explicit member anchors are emitted consistently; in single‑file mode, types also have heading‑based anchors for reliable in‑document navigation.
- New sample type `Xml2Doc.Sample.AliasingPlayground` to validate token‑aware aliasing and signature rendering.
- New tests:
  - `AliasingTests` to ensure identifiers containing aliasable substrings are not corrupted.
  - `NormalizationTests` to verify paragraph preservation, intra‑line trimming, and code fence protection.
  - `NestedGenericsTests` to validate depth‑aware formatting in headers and labels.

### Changed

- XML → Markdown normalization:
  - Preserves paragraph breaks (blank lines) and code fences verbatim.
  - Collapses soft line wraps within a paragraph to a single space.
  - Trims excess spaces/tabs within lines and removes stray spaces before punctuation in prose.
- `ShortTypeDisplay` now recognizes constructed generic types and delegates to a depth‑aware formatter, applying aliases and trimming namespaces for compact display.
- Clarified link behavior:
  - Per‑type output links to files produced by `FileNameMode` and anchors within them.
  - Single‑file output links to heading slugs for types and explicit anchors for members.

### Fixed

- Nested generic `<see/>` labels no longer degrade into malformed text (e.g., eliminated artifacts like `Int32}}}`); labels now correctly apply aliases and trim namespaces.
- Prevented accidental alias replacement inside larger identifiers (e.g., `StringComparer` no longer becomes `stringComparer`).
- Stabilized Markdown output by trimming leading indentation in prose and composing paragraphs predictably, reducing snapshot churn.

### Internal

- `ApplyAliases` refactored to token‑aware regexes for both fully‑qualified (`System.String`) and short names (`String`).
- Consolidated generic formatting via `ShortenSignatureType` and used in more display paths.
- Snapshot seeds updated to include the new `AliasingPlayground` page and refreshed index; tests aligned with normalization behavior.

---

## [1.2.1](https://github.com/mod-posh/Xml2Doc/releases/tag/v1.2.1) - 2025-10-24

This release is a focused bugfix to clean up how nested generic types and parentheses render in Markdown, along with some related test and snapshot fixes. No new features — just making the existing behavior finally *correct*.

## Fixed

- **Trailing parentheses and braces**

  - Eliminated artifacts like `Int32)` and `XMember})` that appeared in method headers.
  - Signatures such as `Flatten(IEnumerable<IEnumerable<XItem>>)` now render with balanced angle brackets and no stray symbols.
- **Alias link formatting**

  - Fixed malformed reference links in alias lines — e.g.
    `Alias that calls [Add(int, int)](...)` now renders cleanly without extra parentheses.
- **Section parsing during tests**

  - Updated `RenderSnapshots` section extraction logic to properly capture multi-level headings in single-file outputs.
  - Prevents cases where `# Mathx` or similar headings couldn’t be found or truncated early.
- **Cross-platform snapshot consistency**

  - Line endings and spacing normalized across all verified markdowns.
  - Reduces false diffs when running tests on Linux or Windows.
- **Snapshot seed refresh**

  - `snapshot_seed.ps1` updated to regenerate reference docs with corrected renderer output.

## Internal

- Refined regex checks for generic parameter lists in `RenderSnapshots`.
- Cleaned up `MarkdownRenderer` short-name logic to remove lingering parentheses on primitive types.
- All snapshot tests (`Mathx`, `GenericPlayground`, `XItem`, and `index`) now pass consistently.

---

## [1.2.0](https://github.com/mod-posh/Xml2Doc/releases/tag/v1.2.0) - 2025-10-24

### Added

- **Grouped Members Rendering** — Members are now grouped by type (Methods, Properties, Events, etc.) for cleaner, more readable Markdown output.
- **Expanded Test Coverage** — Introduced a dedicated test project validating Core, CLI, and MSBuild output end-to-end.
- **CLI Experience Enhancements** — Added improved help text, descriptive flag output, and clearer validation for incorrect parameters.
- **Configuration Validation** — The CLI and MSBuild integrations now validate config files and gracefully handle missing or invalid values.
- **Markdown Rendering Improvements** — Better handling of `<typeparam>`, `<returns>`, and `<value>` elements, and more consistent formatting for parameter and return sections.
- **Improved Project Metadata** — Added uniform tags, repository URLs, and license metadata across all `.csproj` files.
- **Per-Type vs Single-File Output Consistency** — Both modes now share identical structure and styling rules for predictable output.
- **Enhanced Logging for MSBuild** — Output now includes clear file paths and generation summaries.

### Changed

- Refactored renderer to separate type-level and member-level rendering.
- Standardized internal naming conventions across Core/CLI/MSBuild projects.
- Updated CLI argument parsing and error handling for better consistency.
- Cleaned up namespace trimming and display logic for more readable type names.
- Snapshot tests updated to reflect grouped output and new formatting rules.

### Fixed

- Fixed incorrect handling of certain XML tags (`<value>`, `<typeparam>`, `<returns>`).
- Resolved path issues when working with nested namespaces and relative output paths.
- Corrected CLI config parsing where missing keys would previously throw errors.
- Fixed inconsistent Markdown spacing between type and member documentation blocks.

---

## [1.1.0](https://github.com/mod-posh/Xml2Doc/releases/tag/v1.1.0) - 2025-10-24

### Added

- Support for `<remarks>`, `<example>`, `<seealso>`, `<exception>`, and `<inheritdoc/>` tags.
- Method overload grouping for cleaner, consolidated output.
- New CLI option `--config` to load JSON configuration files.
- New CLI options `--file-names` and `--single` for flexible output modes.
- Added `RendererOptions` class with more granular configuration (filename mode, namespace trimming, language, etc.).
- Added full **snapshot test suite** validating per-type and single-file output.
- Added new **MSBuild properties**:
  - `Xml2Doc_SingleFile`
  - `Xml2Doc_OutputFile`
  - `Xml2Doc_OutputDir`
  - `Xml2Doc_FileNameMode`
  - `Xml2Doc_RootNamespaceToTrim`
  - `Xml2Doc_CodeBlockLanguage`
- Added NuGet package metadata including license, icon, project URL, and README integration.

### Changed

- Updated all projects to target **.NET 9.0**.
- Improved display names for generic types and shortened namespace output.
- Standardized built-in type aliasing (`System.String` → `string`, etc.).
- Reorganized project structure for consistency across Core/CLI/MSBuild.
- Cleaned up `.csproj` files and centralized shared properties into `Directory.Build.props`.

### Fixed

- Addressed `PackageTags` element issues in older project files.
- Resolved type display inconsistencies for nested generics and collections.
- Fixed missing README visibility in NuGet packages.

---

## [1.0.0](https://github.com/mod-posh/Xml2Doc/releases/tag/v1.0.0) - 2025-10-22

### Added

- Core library to load and render XML documentation to Markdown.
- CLI tool (`Xml2Doc.Cli`) with `--xml` and `--out` parameters.
- MSBuild integration task to auto-generate Markdown after build.
- Default per-type Markdown output with generated `index.md`.
- Initial .NET 8.0 target and project scaffolding.
