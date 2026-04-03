[LLAMARC42-METADATA]
Type: Risk

Concepts: [
  "technical debt",
  "unimplemented options",
  "future work",
  "inheritdoc limitations",
  "diagnostics",
  "API surface stability"
]

Scope: System

Confidence: Mixed

Source: [
  "code",
  "docs",
  "issues"
]
[/LLAMARC42-METADATA]

# Risks and Technical Debt

## Risk Summary

| ID | Description | Likelihood | Impact | Status |
|----|-------------|-----------|--------|--------|
| R-01 | Declared options not yet implemented | High | Medium | Known / accepted |
| R-02 | `<inheritdoc>` cross-assembly resolution | Medium | Low | Known limitation |
| R-03 | Diagnostics not yet surfaced | Medium | Medium | In progress |
| R-04 | `RendererOptions` API surface includes unimplemented fields | High | Low | Known / accepted |
| R-05 | xml2xdoc.json legacy config artifact | Low | Low | Accepted for removal |
| R-06 | Windows-only CI constraint | Medium | Low | Known constraint |
| R-07 | No SymbolIndex or two-phase pipeline | Low | Unknown | Not yet needed |

---

## R-01: Declared Options Not Yet Implemented

**Description:** `RendererOptions` declares several options that are not currently implemented in `MarkdownRenderer`. These fields are part of the public API surface but have no effect on rendering.

**Unimplemented options:**
| Option | Declared Default | Notes |
|--------|-----------------|-------|
| `TemplatePath` | `null` | Wrapping template file |
| `FrontMatterPath` | `null` | Prepended front matter |
| `AutoLink` | `false` | Heuristic auto-linking in prose |
| `AliasMapPath` | `null` | Custom type alias map from file |
| `ExternalDocs` | `null` | External documentation base URL |

**Risk:** Consumers who set these options may expect behavior that does not occur. No error or warning is emitted when these options are set to non-default values.

**Mitigation:** Document clearly (done in `overview/scope.md` and `components/core.md`). Consider emitting a diagnostic warning when unimplemented options are non-default.

**Developer status:** Accepted as future work.

---

## R-02: Heuristic `<inheritdoc>` Resolution

**Description:** `InheritDocResolver` uses a heuristic to find inherited members by trimming type ID segments. It does not load or reflect compiled binaries.

**Consequence:** `<inheritdoc>` references to members in other assemblies (e.g., base classes from `System.*` or other NuGet packages) are silently unresolved.

**Risk:** Documentation for types using cross-assembly inheritance may be incomplete without warning.

**Mitigation:** Known limitation per ADR-001 (out of scope). Document clearly. A future diagnostic could warn when `<inheritdoc>` cannot be resolved.

---

## R-03: Diagnostics Not Yet Surfaced

**Description:** ADR-009 specifies that unresolved `cref`, malformed XML, and duplicate anchors should be surfaced as structured diagnostics via CLI stderr and MSBuild task messages. This is not fully implemented.

**Risk:** Issues in input XML (bad cref references, malformed tags) are currently silently ignored or cause incorrect output without notification to the developer.

**Mitigation:** ADR-009 is an active architectural commitment. Implementation is in progress.

**Developer confirmation:** Diagnostic codes are intended to surface warnings/errors as CLI stderr and MSBuild output.

---

## R-04: `RendererOptions` API Surface Stability

**Description:** `RendererOptions` is a `sealed record` with positional parameters. Adding new parameters is a source-level breaking change for consumers who construct `RendererOptions` using positional syntax.

**Risk:** Growing the options surface (adding new features) may break downstream consumers who construct options directly.

**Mitigation:** Adding named parameters with defaults is the least-breaking approach. Consumers should use object initializer or `with` expression syntax rather than positional constructor calls.

---

## R-05: xml2xdoc.json Legacy Config File

**Description:** The `xml2xdoc.json` file in the repository root uses a legacy key naming scheme (`Xml`, `Out`, `Single`, `FileNames`, `RootNamespace`, `CodeLanguage`). These differ from the current canonical CLI flag names.

**Risk:** Confusion about the correct config file format. The file itself points to a build artifact path that is not present in the repository.

**Mitigation:** The developer has confirmed this is an old artifact that can be removed. It should be deleted in a cleanup PR to avoid misleading new contributors.

---

## R-06: Windows-Only CI Constraint

**Description:** CI workflows use `windows-latest` runners. The `net472` MSBuild task tests require Windows.

**Risk:** Platform-specific test failures if CI is ever migrated to Linux runners. Linux cannot run `net472` binaries.

**Mitigation:** The `net472` requirement is architectural (MSBuild.exe support). Any Linux CI expansion should only cover `net8.0`/`net9.0` targets.

---

## R-07: No SymbolIndex or Two-Phase Pipeline

**Description:** A SymbolIndex or two-phase rendering pipeline (build symbol table first, then render) does not exist in the current implementation. It has been discussed as a potential architecture addition.

**Risk:** Low. The current single-pass approach works correctly for same-assembly documentation. Cross-assembly linking is out of scope.

**Mitigation:** If cross-file linking or richer cref resolution is needed in the future, a SymbolIndex could be introduced. The developer notes this is unclear in scope for now.

---

## Technical Debt

### TD-01: `--diff` Flag (Reserved, Unimplemented)

The CLI accepts `--diff` as a flag (it appears in help output and argument parsing) but it has no implementation. It is reserved for future use.

### TD-02: Unused Compiled Regexes

`MarkdownRenderer` contains several `static readonly Regex` fields (`GitHubDrop`, `KramdownDrop`, `GfmDrop`, `CollapseDash`) that are marked as "unused placeholder for potential micro-optimization." These are dead code in the current version.

### TD-03: ADR Status Drift

ADR-009 and ADR-010 were marked "Proposed" in their source files, but the developer has confirmed they are active decisions. The source files should be updated to reflect `Status: Active`.

> **Cross-reference:** [overview/scope.md](../overview/scope.md) · [quality/quality-requirements.md](../quality/quality-requirements.md) · [decisions/architecture-decisions.md](../decisions/architecture-decisions.md)
