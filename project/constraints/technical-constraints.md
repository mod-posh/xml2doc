[LLAMARC42-METADATA]
Type: Constraint

Concepts: [
  "multi-targeting",
  "netstandard2.0",
  "net472",
  "net8.0",
  "net9.0",
  "MSBuild runtime",
  "SDK version"
]

Scope: System

Confidence: Observed

Source: [
  "code",
  "docs"
]
[/LLAMARC42-METADATA]

# Technical Constraints

## .NET Target Frameworks

Target framework choices are deliberate architectural decisions (ADR-008) driven by the need to support both `dotnet build` and Visual Studio / MSBuild.exe.

| Component | Target Frameworks | Reason |
|-----------|------------------|--------|
| `Xml2Doc.Core` | `netstandard2.0`, `net8.0`, `net9.0` | NS2.0 required for the `net472` MSBuild task host; net8/net9 for CLI and modern features |
| `Xml2Doc.Cli` | `net8.0`, `net9.0` | CLI does not need to run inside VS/MSBuild |
| `Xml2Doc.MSBuild` | `net472`, `net8.0` | `net472` for Visual Studio / MSBuild.exe; `net8.0` for `dotnet build` |

### MSBuild Task Host Selection

The MSBuild task uses a `UsingTask` condition to automatically select the correct TFM:

- `MSBuildRuntimeType == "Core"` → loads `net8.0` task → uses `net8.0` Core
- Otherwise (VS / MSBuild.exe) → loads `net472` task → uses `netstandard2.0` Core

This was settled after working through issue #46, which found that net472 + net8 was the viable combination for broad MSBuild host support.

## SDK and Language Constraints

- **SDK version:** `9.0.308` (pinned in `global.json`, rollForward: `latestPatch`, no pre-release)
- **Language version:** C# 10 syntax is conditioned for NS2.0 builds; modern C# 12 features are used in net8/net9 projects
- **NS2.0 API constraints:** Code targeting `netstandard2.0` must avoid:
  - `AsSpan()` on strings
  - `Index` / `Range` operators
  - Certain `Split` overloads not present in NS2.0

## Build System

- **Build tool:** MSBuild / `dotnet build`
- **Central props:** `Directory.Build.props` centralizes version, authors, license, nullable settings, and implicit usings
- **Parallelism:** Solution builds with `BuildInParallel=false` for NuGet pack to avoid file contention
- **CI platform:** GitHub Actions, Windows runner (`windows-latest`)

## NuGet and Dependencies

### Xml2Doc.Core

| Package | Version | Scope |
|---------|---------|-------|
| `System.Text.Json` | 8.0.5 | All TFMs |
| `System.Text.Encodings.Web` | 8.0.0 | All TFMs |
| `System.Buffers` | 4.5.1 | NS2.0 only |
| `System.Memory` | 4.5.5 | NS2.0 only |
| `System.Numerics.Vectors` | 4.5.0 | NS2.0 only |
| `System.Runtime.CompilerServices.Unsafe` | 6.0.0 | NS2.0 only |
| `Microsoft.SourceLink.GitHub` | 8.0.0 | Build only (PrivateAssets=all) |

### Xml2Doc.MSBuild

| Package | Version | Scope |
|---------|---------|-------|
| `Microsoft.Build.Framework` | 17.11.* | Build only (PrivateAssets=all) |
| `Microsoft.Build.Utilities.Core` | 17.11.* | Build only (PrivateAssets=all) |
| `System.Text.Json` | 8.0.5 | PrivateAssets=all |

MSBuild build packages are private assets — they do not appear in the transitive dependency graph of consuming projects.

## Symbol and Source Packages

- Symbol format: `snupkg`
- SourceLink enabled (`PublishRepositoryUrl=true`, `EmbedUntrackedSources=true`)

## Platform Constraints

- **CI runner:** `windows-latest` (GitHub Actions). This is required because MSBuild net472 tests are Windows-specific.
- **Output path:** Single scalar `OutputPath` / `IntermediateOutputPath` to prevent MSBuild multi-TFM path errors (`HasTrailingSlash` issue).

> **Cross-reference:** [organizational-constraints.md](organizational-constraints.md) · [architecture/container-view.md](../architecture/container-view.md)
