[LLAMARC42-METADATA]
Type: Quality

Concepts: [
  "snapshot testing",
  "unit testing",
  "integration testing",
  "determinism",
  "cross-TFM equivalence",
  "regression safety",
  "link stability"
]

Scope: System

Confidence: Observed

Source: [
  "code",
  "docs"
]
[/LLAMARC42-METADATA]

# Quality Requirements

## Quality Goals Summary

| Goal | Priority | Measured By |
|------|----------|-------------|
| Deterministic output | Highest | Cross-TFM test; snapshot tests |
| Correct links and anchors | High | `InternalLinkingTests`; ADR-006 |
| Regression-free rendering | High | Snapshot tests; `.verified.md` files |
| Build integration reliability | High | MSBuild integration test scripts |
| Host parity | Medium | Shared `RendererOptions`; both hosts tested |
| Diagnostic surfacing | Medium | In progress (ADR-009) |

---

## Test Architecture

### Layer 1: Snapshot Tests

**File:** `Xml2Doc.Tests/RenderSnapshots.cs`

Snapshot tests compare actual rendered output against committed `.verified.md` files in `__snapshots__/`. A test fails if actual output deviates from the snapshot.

Tests:
- `SingleFile_CleanNames_Basic()` — single-file consolidated output
- `PerType_CleanNames_Basic()` — per-type rendering with clean file names
- `Generic_BraceHandling_IsClean()` — generic brace normalization

**Maintenance:** When a rendering change is intentional, `seed-snapshots.ps1` regenerates snapshot files. The new snapshots are reviewed and committed.

### Layer 2: Unit Tests

**File:** `Xml2Doc.Tests/`

| Test Class | What It Tests |
|-----------|---------------|
| `AliasingTests` | Token-aware aliasing: framework types aliased (`System.String` → `string`) without corrupting identifiers like `StringComparer` |
| `NormalizationTests` | XML → Markdown normalization: paragraphs preserved, intra-line whitespace collapsed, code blocks kept verbatim |
| `NestedGenericsTests` | Depth-aware generic splitting: `Dictionary<string, List<Dictionary<string, int>>>` renders correctly in headers and labels |
| `InternalLinkingTests` | Link resolution: per-type links → file+anchor; single-file links → in-document anchors; href == emitted anchor |

### Layer 3: Integration Tests

**Scripts:** `scripts/test-cli-integration.ps1`, `scripts/test-msbuild-integration.ps1`  
**CI:** `cli-integration.yml`, `msbuild-integration.yml`

Integration tests exercise the built artifacts end-to-end:
- Build the entire solution
- Run CLI with various option combinations
- Run MSBuild build with the task enabled
- Validate output file structure and content
- Check file naming modes, namespace trimming, anchor correctness
- Verify incremental behavior (fingerprint skipping)
- Verify report file generation

### Test Infrastructure

| Component | Purpose |
|-----------|---------|
| `Xml2Doc.Sample` | Controlled XML documentation fixture with known types for deterministic testing |
| `update-fixtures.ps1` | Rebuilds `Xml2Doc.Sample.xml` from source; output committed to `Assets/` |
| `seed-snapshots.ps1` | Regenerates `.verified.md` snapshots from CLI artifact |
| `Assets/Xml2Doc.Sample.xml` | Committed fixture used by all unit and snapshot tests |

---

## Specific Quality Requirements

### QR-1: Deterministic Output

Identical input XML with identical `RendererOptions` must always produce byte-identical Markdown output (modulo line endings). This is enforced by:
- Snapshot tests (committed expected output)
- Cross-TFM consistency test (net8.0 vs net9.0 CLI output compared)

### QR-2: Stable Anchors

Member anchors must not change between renders for the same member ID and same `AnchorAlgorithm`. This is enforced by:
- `InternalLinkingTests.PerType_Links_ResolveToFileAndAnchors()`
- `InternalLinkingTests.SingleFile_Links_ResolveToInDocumentAnchors()`
- `InternalLinkingTests.NestedGeneric_MemberLinkTarget_MatchesAnchor()`

### QR-3: Correct cref Resolution

Every `<see cref="...">` in the input must produce a correct Markdown link. Correctness means:
- Per-type mode: link href points to the correct `.md` file with the correct anchor
- Single-file mode: link href is a valid in-document anchor

### QR-4: Cross-TFM Behavioral Equivalence

CLI behavior must be identical on `net8.0` and `net9.0`. This is tested by building and running both, then diffing output.

### QR-5: Incremental Build Correctness

The MSBuild fingerprint must change when and only when inputs (XML content or rendering options) change. This prevents both missed regeneration (false negatives) and unnecessary regeneration (false positives).

### QR-6: Diagnostic Visibility

Warnings and errors encountered during rendering (e.g., unresolved `cref`, malformed XML) must be surfaced as observable output — CLI stderr or MSBuild task messages — rather than silently ignored. **Status: in progress (ADR-009).**

---

## Test Execution

```bash
# Run unit and snapshot tests
dotnet test Xml2Doc/Xml2Doc.sln --filter "Category!=Integration"

# Run CLI integration tests
./scripts/test-cli-integration.ps1

# Run MSBuild integration tests
./scripts/test-msbuild-integration.ps1

# Update XML fixture (after changing Xml2Doc.Sample)
./scripts/update-fixtures.ps1

# Regenerate snapshots (after intentional rendering changes)
./scripts/seed-snapshots.ps1
```

> **Cross-reference:** [overview/goals.md](../overview/goals.md) · [risks/risks-and-technical-debt.md](../risks/risks-and-technical-debt.md)
