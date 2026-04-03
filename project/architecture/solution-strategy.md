[LLAMARC42-METADATA]
Type: Architecture

Concepts: [
  "separation of concerns",
  "Core owns semantics",
  "host parity",
  "deterministic rendering",
  "multi-targeting"
]

Scope: System

Confidence: Observed

Source: [
  "docs",
  "code"
]
[/LLAMARC42-METADATA]

# Solution Strategy

## Fundamental Approach

xml2doc follows a strict **Core-owns-semantics** architecture. The Core library is the single source of rendering truth. CLI and MSBuild are thin hosts that only translate their native inputs (arguments, MSBuild properties) into `RendererOptions` and invoke Core.

This separation ensures:
- Behavioral parity between all hosts
- A single place to fix rendering bugs
- Independent versioning and testing of hosts

## Key Architectural Decisions

### 1. Core as the Sole Rendering Engine

All XML parsing, Markdown generation, link resolution, anchor computation, and output planning live in `Xml2Doc.Core`. Neither `Xml2Doc.Cli` nor `Xml2Doc.MSBuild` contain rendering logic.

**Evidence:** `MarkdownRenderer`, `RendererOptions`, `ILinkResolver`, `Xml2Doc` (model), and `InheritDocResolver` are all defined in Core.

### 2. Determinism Before Cleverness

Output correctness and reproducibility are prioritized over formatting aesthetics. The same XML input must always produce the same Markdown output. This is enforced by snapshot tests and a cross-TFM equivalence test.

### 3. Stable Public Contracts

Links and anchors are treated as part of the public API. Once a rendered anchor exists and is published, changing it is a breaking change. Explicit member anchors are emitted alongside heading-based anchors to guarantee stable fragment links.

### 4. Multi-Framework Support by Host Role

Different hosts have different framework requirements:

| Host | Frameworks | Reason |
|------|-----------|--------|
| Core | NS2.0, net8, net9 | NS2.0 bridges to net472 MSBuild host |
| CLI | net8, net9 | Modern runtime only |
| MSBuild | net472, net8 | VS/MSBuild.exe needs net472; dotnet build uses net8 |

The net472 target was chosen after evaluating alternatives (issue #46). It is the minimum that works with Visual Studio's MSBuild runtime.

### 5. Incremental Build by Fingerprinting

Rather than relying on MSBuild's built-in incremental logic, the MSBuild task implements its own fingerprinting: a SHA-256 hash of the XML file content combined with a hash of all `RendererOptions` values. This ensures skipping is correct even when options change without the XML changing.

### 6. Test-Driven Regression Safety

Rendering output is protected by three test layers:
- **Snapshot tests:** committed `.verified.md` files compared on every run
- **Unit tests:** aliasing, normalization, nested generics, internal linking
- **Integration tests:** CLI and MSBuild end-to-end via PowerShell scripts and GitHub Actions

### 7. Diagnostics as First-Class Output

Diagnostic codes are intended to surface warnings and errors explicitly — as CLI stderr and MSBuild task output — rather than silently ignoring issues like unresolved `cref` or malformed XML. This is confirmed as a goal (ADR-009) and is in progress.

## Architecture Evolution

| Milestone | Version | Focus |
|-----------|---------|-------|
| M1 | 1.0.0 | Foundation: Core/CLI/MSBuild structure |
| M2 | 1.1.0 | RendererOptions and host parity |
| M3 | 1.2.0 | Output quality and snapshot regression safety |
| M4 | 1.2.1 | Signature and cref hardening |
| M5 | 1.3.x | Link and anchor contract stabilization |
| M6 | 1.4.x | Build and platform maturity (current) |
| M7 | Future | Structured diagnostics, pluggable anchors |

> **Cross-reference:** [system-context.md](system-context.md) · [container-view.md](container-view.md) · [decisions/architecture-decisions.md](../decisions/architecture-decisions.md)
