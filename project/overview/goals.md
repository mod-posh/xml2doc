[LLAMARC42-METADATA]
Type: Overview

Concepts: [
  "deterministic output",
  "link stability",
  "regression safety",
  "host parity",
  "incremental build"
]

Scope: System

Confidence: Observed

Source: [
  "docs",
  "code"
]
[/LLAMARC42-METADATA]

# Goals

## Quality Goals

The following goals are listed in priority order. They are derived from the project Constitution, ADRs, and observed code behavior.

### 1. Deterministic Output

**Goal:** Identical input always produces identical output, regardless of execution order, parallelism, or platform.

**Status:** Observed. Output is enforced to be equivalent across `net8.0` and `net9.0` CLI builds (cross-TFM consistency test). Snapshot tests capture and protect exact rendered output.

**Why it matters:** Documentation embedded in version control should produce minimal diffs. Non-deterministic output creates noise in PRs and breaks incremental MSBuild.

---

### 2. Stable Links and Anchors

**Goal:** Links and anchors are part of the public contract. Once published, they must not change unless explicitly versioned.

**Status:** Observed. ADR-006 codifies this. Explicit member anchors are emitted; in single-file mode, heading-based anchors are also generated.

**Why it matters:** External references (other documents, wikis, sites) break when anchor slugs change silently.

---

### 3. Regression Safety

**Goal:** Any change to rendering behavior is caught by tests before release.

**Status:** Observed. The test suite uses snapshot tests (verified `.md` files committed to the repo), unit tests for specific behaviors (aliasing, normalization, generic formatting, linking), and integration tests via PowerShell scripts and GitHub Actions.

---

### 4. Host Parity

**Goal:** CLI flags and MSBuild properties expose the same `RendererOptions` surface. Behavior must not differ between hosts.

**Status:** Observed. Both CLI and MSBuild map their inputs to the same `RendererOptions` record passed to `MarkdownRenderer`.

---

### 5. Build Integration Efficiency

**Goal:** MSBuild integration must not re-generate documentation unless inputs change.

**Status:** Observed. The `Xml2Doc_ComputeFingerprint` target computes a SHA-256 hash of the XML file combined with the option set. Generation is skipped when the fingerprint matches the previous run.

---

### 6. Multi-Platform Compatibility

**Goal:** Support `dotnet build` (net8.0/net9.0) and Visual Studio / MSBuild.exe (net472) build hosts without behavioral differences.

**Status:** Observed. The MSBuild task auto-selects its TFM based on `MSBuildRuntimeType`. Core targets `netstandard2.0` for compatibility with the net472 task host.

---

## Success Criteria

| Criterion | How It Is Measured |
|-----------|-------------------|
| Output correctness | Snapshot tests match committed `.verified.md` files |
| Link stability | `InternalLinkingTests` assert href == emitted anchor |
| Cross-TFM equivalence | Cross-TFM consistency test in CI |
| Incremental build | Fingerprint check skips redundant task execution |
| Integration | CLI and MSBuild integration test scripts in `scripts/` |

> **Cross-reference:** [scope.md](scope.md) · [quality/quality-requirements.md](../quality/quality-requirements.md) · [decisions/architecture-decisions.md](../decisions/architecture-decisions.md)
