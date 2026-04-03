[LLAMARC42-METADATA]
Type: Decision

Concepts: [
  "ADR",
  "architecture decision record",
  "scope",
  "solution structure",
  "output modes",
  "configuration model",
  "regression strategy",
  "link stability",
  "MSBuild incremental",
  "multi-targeting",
  "structured diagnostics",
  "pluggable anchors"
]

Scope: System

Confidence: Mixed

Source: [
  "docs",
  "code"
]
[/LLAMARC42-METADATA]

# Architecture Decisions

This document summarizes all Architecture Decision Records (ADRs) for xml2doc. ADRs are the source of truth for architectural choices. If code and an ADR disagree, the ADR takes precedence (or a new ADR must supersede it).

For the original ADR files, see `docs/adr/` in the repository.

---

## ADR Index

| ADR | Title | Status |
|-----|-------|--------|
| ADR-001 | Scope and Non-Goals | Active |
| ADR-002 | Solution Structure | Active |
| ADR-003 | Output Modes | Active |
| ADR-004 | Shared Configuration Model | Active |
| ADR-005 | Regression Strategy | Active |
| ADR-006 | Link and Anchor Stability | Active |
| ADR-007 | MSBuild Incremental Generation | Active |
| ADR-008 | Multi-Target Host Compatibility | Active |
| ADR-009 | Structured Diagnostics | Active |
| ADR-010 | Pluggable Anchor Algorithms | Active |

> **Note on status:** ADR-009 and ADR-010 were previously marked "Proposed" in the source files. The developer has confirmed these decisions are **active** — they reflect current intent and direction, not speculative future work. The source ADR files should be updated accordingly.

---

## ADR-001 — Scope and Non-Goals

**Status:** Active

**Decision:** xml2doc is responsible for transforming .NET XML documentation into Markdown. The project will not expand into a full documentation platform.

**Consequences:**
- Design remains focused on deterministic Markdown generation
- No static site generation, hosting, or prose editing
- `<inheritdoc>` resolution is heuristic (no binary reflection)

---

## ADR-002 — Solution Structure

**Status:** Active

**Decision:** The repository contains three components: Core, CLI, MSBuild. Core owns semantics and rendering. Hosts only expose Core behavior.

**Consequences:**
- All rendering bugs are fixed in Core
- CLI and MSBuild contain no rendering logic
- Adding a new host requires only a thin translation layer

---

## ADR-003 — Output Modes

**Status:** Active

**Decision:** Two supported output modes: (1) per-type documents (one `.md` per type + `index.md`), (2) single-file document. Both modes share the same semantic rendering rules.

**Consequences:**
- Link resolution differs between modes (file-based vs in-document anchors)
- `LinkContext.SingleFile` flag controls resolver strategy
- `PlanOutputs` covers both modes for dry-run and reporting

---

## ADR-004 — Shared Configuration Model

**Status:** Active

**Decision:** Rendering behavior is controlled by `RendererOptions` in Core. Hosts expose this through CLI flags and MSBuild properties.

**Consequences:**
- `RendererOptions` is a `sealed record` — immutable and value-comparable
- CLI and MSBuild must map their native inputs to `RendererOptions` exactly
- New rendering options are added to `RendererOptions` first, then exposed by hosts

---

## ADR-005 — Regression Strategy

**Status:** Active

**Decision:** The project uses snapshot tests, sample project tests, and unit tests to protect rendering output from regressions.

**Consequences:**
- Committed `.verified.md` snapshot files capture expected output
- The `Xml2Doc.Sample` project provides a controlled XML fixture
- `seed-snapshots.ps1` regenerates snapshots when intentional changes are made
- `update-fixtures.ps1` updates the XML fixture from the Sample project

---

## ADR-006 — Link and Anchor Stability

**Status:** Active

**Decision:** Links and anchors are part of the public contract. The renderer must emit stable anchors and correct link targets.

**Consequences:**
- Explicit member anchors are emitted independently of heading text
- `IdToAnchor` is deterministic: same member ID always produces the same anchor
- Changing anchor computation is a breaking change requiring a new ADR or version bump
- `ILinkResolver` (internal) is confirmed stable in its current form

---

## ADR-007 — MSBuild Incremental Generation

**Status:** Active

**Decision:** MSBuild integration supports incremental execution using fingerprinting (SHA-256 of XML + options), input comparison, and stamp file generation.

**Consequences:**
- Documentation is not regenerated unless the XML or options change
- The fingerprint combines content hash + serialized option values
- A `.stamp` file is written for MSBuild's own incremental tracking

---

## ADR-008 — Multi-Target Host Compatibility

**Status:** Active

**Decision:** The project targets multiple frameworks to support both `dotnet build` (net8.0/net9.0) and Visual Studio / MSBuild.exe (net472).

**Consequences:**
- Core targets `netstandard2.0` to be loadable by the `net472` task
- NS2.0 API constraints apply to Core code (no `AsSpan`, `Index`/`Range`, certain `Split` overloads)
- The net472 + net8 pairing was chosen after evaluating alternatives (issue #46)
- CI runs on `windows-latest` to support net472 tests

---

## ADR-009 — Structured Diagnostics

**Status:** Active

**Decision:** Structured diagnostics for issues (unresolved `cref`, malformed XML, duplicate anchors) should be surfaced consistently in CLI and MSBuild.

**Implementation Status:** In progress. The developer has confirmed that diagnostic codes are intended to surface warnings and errors as CLI stderr and MSBuild task output. This is not yet fully implemented but is an active architectural commitment.

**Consequences:**
- Diagnostics must not be silently swallowed
- CLI should write diagnostics to stderr with structured codes
- MSBuild task should emit diagnostics as MSBuild warnings/errors
- This enables consumer tooling to parse and act on diagnostic output

---

## ADR-010 — Pluggable Anchor Algorithms

**Status:** Active

**Decision:** Allow alternate anchor slug algorithms (GitHub/GFM, Kramdown) in addition to the default algorithm.

**Implementation Status:** Implemented. `AnchorAlgorithm` enum is defined with `Default`, `Github`, `Kramdown`, and `Gfm` values. `HeadingSlug` in `MarkdownRenderer` applies the selected algorithm. The default behavior is backward compatible.

**Consequences:**
- Consumers can select the algorithm that matches their Markdown renderer
- Default algorithm remains unchanged (no breaking change)
- Adding new algorithms requires only extending the enum and `HeadingSlug`

---

## Governance Reminder

Per the project Constitution:

> If code and ADRs disagree: (1) the ADR is the source of truth, (2) code must be updated, or (3) a new ADR must supersede the old one.

> **Cross-reference:** [architecture/solution-strategy.md](../architecture/solution-strategy.md) · [overview/scope.md](../overview/scope.md)
