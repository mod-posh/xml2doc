[LLAMARC42-METADATA]
Type: Workflow

Concepts: [
  "key scenarios",
  "per-type output",
  "single-file output",
  "dry-run",
  "MSBuild integration",
  "namespace trimming",
  "anchor algorithm"
]

Scope: System

Confidence: Observed

Source: [
  "code",
  "docs"
]
[/LLAMARC42-METADATA]

# Key Scenarios

## Scenario 1: Generate Per-Type Markdown via CLI

**Actor:** .NET developer or CI pipeline  
**Goal:** Produce one Markdown file per documented type in a `docs/api/` folder

```
xml2doc --xml MyLib.xml --out docs/api/ --file-names clean --rootns MyLib
```

**What happens:**
1. CLI parses flags → `FileNameMode.CleanGenerics`, `RootNamespaceToTrim = "MyLib"`
2. `Xml2Doc.Load("MyLib.xml")` builds the member dictionary
3. `MarkdownRenderer.RenderToDirectory("docs/api/")` writes:
   - One `.md` file per type (e.g., `MyLib.Widget.md` → `Widget.md` with namespace trimmed in display)
   - `index.md` listing all types
4. Files are written; CLI returns exit 0

**Result:** `docs/api/Widget.md`, `docs/api/index.md`, etc.

---

## Scenario 2: Generate Single Consolidated File via CLI

**Actor:** Developer who wants all documentation in one file  
**Goal:** Produce a single `api.md` with all types

```
xml2doc --xml MyLib.xml --out docs/api.md --single
```

**What happens:**
1. `--single` sets single-file mode
2. `MarkdownRenderer.RenderToSingleFile("docs/api.md")` writes all types into one file
3. Type anchors use `HeadingSlug` algorithm; member anchors use `IdToAnchor`
4. `<see cref>` links resolve to in-document anchors (`#...`)

**Result:** `docs/api.md` containing all types with internal navigation links

---

## Scenario 3: MSBuild Auto-Generation on Build

**Actor:** Developer who wants documentation generated automatically  
**Goal:** Documentation is regenerated whenever the code changes

**Setup (in `.csproj`):**
```xml
<PackageReference Include="Xml2Doc.MSBuild" Version="1.4.0" PrivateAssets="all" />
```

**What happens on `dotnet build`:**
1. Compiler generates `MyLib.xml` (via `<GenerateDocumentationFile>true</GenerateDocumentationFile>`)
2. After `CoreCompile`, `Xml2Doc_ComputeFingerprint` runs:
   - Computes SHA-256 of `MyLib.xml`
   - Compares fingerprint with stored value
   - If unchanged: skips generation
3. If changed: `Xml2Doc_Generate` invokes `GenerateMarkdownFromXmlDoc`
4. Core renders files to `$(MSBuildProjectDirectory)\docs` (default)
5. `xml2doc-report.json` is written

**Result:** Markdown files are always in sync with the compiled API; incremental skipping avoids unnecessary work.

---

## Scenario 4: Dry Run (Plan Without Writing)

**Actor:** Developer checking what files would be generated  
**Goal:** Understand output without modifying the file system

```
xml2doc --xml MyLib.xml --out docs/api/ --dry-run
```

**What happens:**
1. CLI parses flags; `--dry-run` is set
2. `MarkdownRenderer.PlanOutputs("docs/api/")` returns the list of files that *would* be written
3. CLI prints the file list to stdout
4. No files are written; exits with code 0

---

## Scenario 5: Namespace Trimming

**Actor:** Developer with deeply namespaced types  
**Goal:** Display `Widget` instead of `MyOrg.MyLib.Widget` in all headings and links

```
xml2doc --xml MyOrg.MyLib.xml --out docs/ --rootns MyOrg.MyLib --trim-rootns-filenames
```

**What happens:**
- `RootNamespaceToTrim = "MyOrg.MyLib"` strips the prefix from display labels
- `TrimRootNamespaceInFileNames = true` also strips it from output filenames
- `Widget.md` is produced instead of `MyOrg.MyLib.Widget.md`
- Headings read `Widget` instead of `MyOrg.MyLib.Widget`

---

## Scenario 6: GitHub-Compatible Anchors

**Actor:** Developer publishing Markdown to GitHub  
**Goal:** Ensure heading anchors match GitHub's rendering algorithm

```
xml2doc --xml MyLib.xml --out docs/ --anchor-algorithm github
```

**What happens:**
- `AnchorAlgorithm.Github` is selected
- `HeadingSlug` applies: Unicode normalization → diacritic removal → lowercase → whitespace to dash → strip non-`[a-z0-9-]`
- Links from `<see>` tags use GitHub-compatible fragments

---

## Scenario 7: MSBuild with Visual Studio

**Actor:** Developer building inside Visual Studio  
**Goal:** Documentation generates when building in VS, not just `dotnet build`

**What happens:**
- MSBuild host is VS / MSBuild.exe (`MSBuildRuntimeType != "Core"`)
- `UsingTask` condition selects the `net472` task assembly
- `net472` task loads Core via `netstandard2.0` TFM
- Rendering behavior is identical to `dotnet build`

This multi-target approach was established in v1.4.0 (issue #46).

---

## Scenario 8: JSON Report Output

**Actor:** CI pipeline wanting a machine-readable manifest of generated files  
**Goal:** Know exactly which files were generated in this run

```
xml2doc --xml MyLib.xml --out docs/ --report build/xml2doc-report.json
```

**What happens:**
- After rendering, CLI writes `xml2doc-report.json` listing all generated file paths
- MSBuild also writes this report when `Xml2Doc_ReportPath` is set (default: `docs\xml2doc-report.json`)

---

## Scenario 9: Cross-TFM Equivalence Verification (Testing)

**Actor:** CI system  
**Goal:** Verify that net8.0 and net9.0 CLI produce identical output

**What happens:**
1. CI builds CLI for both `net8.0` and `net9.0`
2. Each TFM renders the same XML fixture
3. Output Markdown is compared with EOL normalization
4. Test fails if any difference exists (other than line endings)

This is a regression guard ensuring multi-targeting does not introduce output divergence.

> **Cross-reference:** [workflows/runtime-flows.md](runtime-flows.md) · [components/cli.md](../components/cli.md) · [components/msbuild.md](../components/msbuild.md)
