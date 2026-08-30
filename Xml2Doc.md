# Xml2Doc

Xml2Doc converts C# compiler XML documentation into deterministic, linkable Markdown. It is available as a library, CLI, and MSBuild package.

## Version 2.3.1

`2.3.1` is a stabilization release for the multi-project aggregation, MSBuild lifecycle, and Markdown rendering behavior introduced in `2.3.0`.

Highlights:

- Bare `<inheritdoc />` in aggregate inputs now resolves through a unique conventional interface type when unrelated members expose the same signature.
- `dotnet clean` removes configuration-scoped Xml2Doc incremental state while preserving generated Markdown and state for other configurations.
- XML documentation bullet lists render as distinct Markdown bullets while preserving inline markup and paragraph boundaries.
- `Xml2Doc.Core` can load multiple primary XML documentation inputs as one aggregate model.
- `Xml2Doc.Cli` accepts repeated `--xml` arguments or `XmlInputs` in JSON configuration.
- `Xml2Doc.MSBuild` supports an explicit repository aggregation owner with `Xml2Doc_AggregateEnabled=true`.
- Aggregate inputs are normalized, de-duplicated, and ordered deterministically before rendering.
- A single aggregate `index.md` can contain every participating project in stable ordinal order.
- Parallel and serial MSBuild aggregation are required to produce byte-identical output.
- `XML2DOC006` reports duplicate member ownership across primary XML inputs.
- `XML2DOC007` reports conflicting MSBuild ownership of the same aggregate `index.md`.
- The `2.2.0` structured diagnostics, runner-backed dry-run/diff/reporting, bounded parallel rendering, incremental writes, templates, front matter, auto-linking, alias maps, and external-documentation fallback remain available.

Existing single-project behavior remains compatible. `Xml2Doc_GenerateIndex=false` is still supported when independent project invocations intentionally share an output directory.

## Supported frameworks

| Component | Target frameworks | Purpose |
| --- | --- | --- |
| `Xml2Doc.Core` | `netstandard2.0`, `net8.0`, `net9.0` | Library and renderer |
| `Xml2Doc.Cli` | `net8.0`, `net9.0` | Command-line host |
| `Xml2Doc.MSBuild` | `net472`, `net8.0` | Visual Studio/MSBuild.exe and `dotnet build` task hosts |

The MSBuild package selects its task assembly automatically. Do not define a custom `GenerateMarkdownFromXmlDoc` target or manually call Xml2Doc's internal fingerprint targets.

## Install the MSBuild package

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Xml2Doc.MSBuild" Version="2.3.1" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

`GenerateDocumentationFile` must be `true` for projects whose compiler XML is used as documentation input. The package imports its own generation and incremental-build targets.

## MSBuild configuration

Most Xml2Doc properties can be placed in either a project file or a solution-level `Directory.Build.props`. Project properties override values imported earlier from `Directory.Build.props`.

The package currently assigns `Xml2Doc_Toc`, `Xml2Doc_NamespaceIndex`, and `Xml2Doc_BasenameOnly` to `false` in its imported defaults. Set those three in the project file or `Directory.Build.targets` when enabling them globally.

### Generation and output properties

| Property | Default | Behavior |
| --- | --- | --- |
| `GenerateDocumentationFile` | SDK-dependent | Compiler property. Set `true` for every project that contributes XML documentation. |
| `Xml2Doc_Enabled` | `true` | Enables normal per-project Markdown generation. |
| `Xml2Doc_SingleFile` | `false` | `false` writes one file per type; `true` writes one combined file. |
| `Xml2Doc_OutputDir` | `$(MSBuildProjectDirectory)\docs` | Output root for per-type mode. |
| `Xml2Doc_OutputFile` | `$(MSBuildProjectDirectory)\docs\api.md` | Output path for single-file mode. |
| `Xml2Doc_GenerateIndex` | `true` | Writes `index.md` in per-type mode. |
| `Xml2Doc_FileNameMode` | `clean` | `clean` or `verbatim`. |
| `Xml2Doc_RootNamespaceToTrim` | Empty | Namespace prefix trimmed from displayed type names. |
| `Xml2Doc_TrimRootNamespaceInFileNames` | `false` | Also trims the configured root namespace from filenames. |
| `Xml2Doc_CodeBlockLanguage` | `csharp` | Fenced-code language. |
| `Xml2Doc_AnchorAlgorithm` | `default` | `default`, `github`, `gfm`, or `kramdown`. |
| `Xml2Doc_Toc` | `false` | Emits member tables of contents where supported. |
| `Xml2Doc_NamespaceIndex` | `false` | Emits namespace index output in per-type mode. |
| `Xml2Doc_BasenameOnly` | `false` | Uses only basename segments for generated type files and links. |
| `Xml2Doc_ParallelDegree` | `1` | Maximum concurrent per-type renders. |
| `Xml2Doc_LineEndings` | `lf` | `lf`, `crlf`, or `native`. `lf` is deterministic across hosts. |
| `Xml2Doc_PruneStaleFiles` | `false` | Removes stale files previously owned by the same invocation. Per-type mode only. |
| `Xml2Doc_ManifestIdentity` | Empty | Stable identity required when pruning is enabled. |
| `Xml2Doc_ReportPath` | `$(Xml2Doc_OutputDir)\xml2doc-report.json` | JSON report path for normal project generation. |
| `Xml2Doc_ReportIncludeTimestamp` | `false` | Adds a timestamp to reports when enabled. |
| `Xml2Doc_DryRun` | `false` | Plans generation without writing Markdown. |
| `Xml2Doc_Diff` | `false` | Reserved in the MSBuild task; currently has no effect. |
| `Xml2Doc_Dump` | `false` | Logs evaluated paths and key generation inputs. |
| `Xml2Doc_LogChosenTask` | `false` | Logs the selected task TFM and assembly path. |

Stale-file pruning is supported only in per-type mode. Single-file mode replaces its configured output directly.

### Reference XML for inheritance

Xml2Doc automatically loads XML documentation found beside referenced project assemblies for `<inheritdoc />` resolution. Referenced members participate in inheritance lookup but do not generate additional Markdown pages.

Extra reference XML can be supplied explicitly:

```xml
<ItemGroup>
  <Xml2Doc_ReferenceXml Include="$(BaseOutputPath)Contracts\Contracts.xml" />
</ItemGroup>
```

Missing explicit reference files and unresolved `<inheritdoc />` targets produce warnings. Reference XML contents participate in incremental fingerprinting.

## Recommended single-project configuration

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <Xml2Doc_Enabled>true</Xml2Doc_Enabled>
  <Xml2Doc_SingleFile>false</Xml2Doc_SingleFile>
  <Xml2Doc_OutputDir>$(MSBuildProjectDirectory)\docs</Xml2Doc_OutputDir>
  <Xml2Doc_FileNameMode>clean</Xml2Doc_FileNameMode>
  <Xml2Doc_RootNamespaceToTrim>MyCompany.MyProduct</Xml2Doc_RootNamespaceToTrim>
  <Xml2Doc_TrimRootNamespaceInFileNames>true</Xml2Doc_TrimRootNamespaceInFileNames>
  <Xml2Doc_PruneStaleFiles>true</Xml2Doc_PruneStaleFiles>
  <Xml2Doc_ManifestIdentity>$(MSBuildProjectName)</Xml2Doc_ManifestIdentity>
  <Xml2Doc_ParallelDegree>4</Xml2Doc_ParallelDegree>
  <Xml2Doc_LineEndings>lf</Xml2Doc_LineEndings>
</PropertyGroup>
```

## Repository aggregation with MSBuild

Use one small project as the aggregation owner. Participating projects emit compiler XML; the owner performs the Markdown render once.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>

    <!-- The owner orchestrates documentation; it does not render its own compiler XML. -->
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <Xml2Doc_Enabled>false</Xml2Doc_Enabled>
    <Xml2Doc_AggregateEnabled>true</Xml2Doc_AggregateEnabled>

    <Xml2Doc_OutputDir>$(MSBuildProjectDirectory)\..\docs\api</Xml2Doc_OutputDir>
    <Xml2Doc_FileNameMode>clean</Xml2Doc_FileNameMode>
    <Xml2Doc_LineEndings>lf</Xml2Doc_LineEndings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\src\ProjectA\ProjectA.csproj" />
    <ProjectReference Include="..\src\ProjectB\ProjectB.csproj" />
    <PackageReference Include="Xml2Doc.MSBuild" Version="2.3.1" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Each participating project must emit XML documentation:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

If a participant also uses Xml2Doc and points at the aggregate output directory, either disable its normal Markdown generation:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <Xml2Doc_Enabled>false</Xml2Doc_Enabled>
</PropertyGroup>
```

or delegate only index ownership to the aggregate owner:

```xml
<PropertyGroup>
  <Xml2Doc_GenerateIndex>false</Xml2Doc_GenerateIndex>
</PropertyGroup>
```

The owner validates referenced Xml2Doc projects before normal referenced-project builds begin. A participant that still claims the same `index.md` fails with `XML2DOC007`.

Additional primary XML inputs that are not project references can be supplied with `Xml2Doc_AggregateXml`:

```xml
<ItemGroup>
  <Xml2Doc_AggregateXml Include="$(RepoRoot)\artifacts\External.Contracts.xml" />
</ItemGroup>
```

Primary aggregate inputs generate pages. `Xml2Doc_ReferenceXml` remains separate and is used only for inheritance/reference resolution.

### Aggregate properties

| Property | Default | Behavior |
| --- | --- | --- |
| `Xml2Doc_AggregateEnabled` | `false` | Makes the project the repository aggregation owner. |
| `Xml2Doc_AggregateValidateIndexOwnership` | `true` | Checks referenced Xml2Doc projects for conflicting `index.md` ownership. |
| `Xml2Doc_AggregateReportPath` | `$(Xml2Doc_OutputDir)\xml2doc-aggregate-report.json` | Aggregate JSON report path. |
| `Xml2Doc_AggregateOutputStamp` | `$(IntermediateOutputPath)xml2doc.aggregate.stamp` | Aggregate incremental-build stamp. |
| `Xml2Doc_AggregateFingerprintFile` | `$(IntermediateOutputPath)xml2doc.aggregate.fingerprint.txt` | Aggregate input/options fingerprint. |
| `Xml2Doc_AggregateOutputLedger` | `$(IntermediateOutputPath)xml2doc.aggregate.outputs.txt` | Recorded aggregate output paths. |

The aggregate owner reuses normal renderer settings such as output mode, filename mode, namespace trimming, index generation, line endings, pruning, and reporting timestamp behavior. Aggregate input identities, explicit reference XML identities, rendering options, and host-native newline policy participate in the aggregate fingerprint.

Build the owner normally:

```powershell
dotnet build .\docs\ApiDocs.csproj -c Release
```

The aggregate report includes canonical `xmlInputs`. Serial and parallel repository builds are expected to produce the same file set and bytes.

For a focused explanation of this pattern, see [docs/msbuild-repository-aggregation.md](docs/msbuild-repository-aggregation.md).

## Compatibility pattern for independent shared-output projects

Repositories do not have to adopt an aggregation owner immediately. Independent projects may continue to share a directory if each has a distinct manifest identity and does not attempt to generate a shared index:

```xml
<PropertyGroup>
  <Xml2Doc_OutputDir>$(MSBuildThisFileDirectory)docs</Xml2Doc_OutputDir>
  <Xml2Doc_GenerateIndex>false</Xml2Doc_GenerateIndex>
  <Xml2Doc_PruneStaleFiles>true</Xml2Doc_PruneStaleFiles>
  <Xml2Doc_ManifestIdentity>$(MSBuildProjectName)</Xml2Doc_ManifestIdentity>
  <Xml2Doc_ReportPath>$(Xml2Doc_OutputDir)\xml2doc-report-$(MSBuildProjectName).json</Xml2Doc_ReportPath>
</PropertyGroup>
```

This prevents index races but intentionally does not create a unified repository index. Prefer the aggregation-owner pattern when one complete index is required.

## CLI quick start

Install the .NET tool:

```powershell
dotnet tool install --global Xml2Doc.Cli --version 2.3.1
```

Generate per-type documentation from one XML file:

```powershell
xml2doc `
  --xml .\bin\Release\net9.0\MyLibrary.xml `
  --out .\docs `
  --file-names clean `
  --rootns MyCompany.MyProduct `
  --parallel 4 `
  --line-endings lf
```

Aggregate multiple projects directly from the CLI by repeating `--xml`:

```powershell
xml2doc `
  --xml .\src\ProjectA\bin\Release\net9.0\ProjectA.xml `
  --xml .\src\ProjectB\bin\Release\net9.0\ProjectB.xml `
  --out .\docs `
  --file-names clean `
  --line-endings lf
```

One XML input uses the compatible single-input loading path. Two or more primary inputs use deterministic Core aggregation.

### CLI options

| Option | Behavior |
| --- | --- |
| `--xml <path>` | Primary XML input. Repeat to aggregate multiple inputs. |
| `--out <path>` | Output directory, or output file with `--single`. |
| `--single` | Generate one combined Markdown file. |
| `--file-names <verbatim\|clean>` | Filename mode. |
| `--rootns <namespace>` | Trim a root namespace from display names. |
| `--trim-rootns-filenames` | Also trim the root namespace from filenames. |
| `--lang <language>` | Fenced-code language. |
| `--anchor-algorithm <mode>` | `default`, `github`, `gfm`, or `kramdown`. |
| `--template <path>` | Apply a file-based template. |
| `--front-matter <path>` | Prepend configured front matter. |
| `--auto-link` | Enable safe free-text symbol linking. |
| `--alias-map <path>` | Load an additional alias map. |
| `--external-docs <base-url>` | Route unresolved references to external documentation. |
| `--toc` | Emit member tables of contents. Directory output only. |
| `--namespace-index` | Emit namespace index/pages. Directory output only. |
| `--no-index` | Suppress per-type `index.md`. |
| `--basename-only` | Use basename-only output names and links. |
| `--parallel <N>` | Maximum per-type render parallelism. |
| `--prune-stale` | Remove stale output owned by the selected manifest identity. Directory output only. |
| `--manifest-id <identity>` | Stable ownership identity used with pruning. |
| `--line-endings <style>` | `lf`, `crlf`, or `native`. |
| `--report <path>` | Write a JSON execution report. |
| `--dry-run` | Plan without writing output. |
| `--diff` | Compare generated output with current files without modifying them. |
| `--config <path>` | Load JSON configuration. CLI arguments take precedence. |
| `--help`, `-h` | Show help. |

`--dry-run` and `--diff` are mutually exclusive. Diff returns exit code `0` when output is current and `3` when differences are found. Invalid CLI arguments return `1`; diagnostic/runtime errors return `2`.

### CLI JSON aggregation example

```json
{
  "XmlInputs": [
    "src/ProjectA/bin/Release/net9.0/ProjectA.xml",
    "src/ProjectB/bin/Release/net9.0/ProjectB.xml"
  ],
  "Out": "docs",
  "FileNames": "clean",
  "GenerateIndex": true,
  "Parallel": 4,
  "LineEndings": "lf"
}
```

`XmlInputs` takes precedence over the legacy single-input `Xml` property when no `--xml` arguments are supplied. Repeated CLI `--xml` arguments take precedence over both JSON properties.

When a CLI report is enabled for aggregate input, it includes canonical `xmlInputs` while retaining the compatible `xml` field for the first canonical input.

## Diagnostics

Xml2Doc uses stable diagnostic identifiers:

| ID | Meaning |
| --- | --- |
| `XML2DOC001` | Unresolved `cref`. |
| `XML2DOC002` | Duplicate generated anchor. |
| `XML2DOC003` | Malformed XML documentation input. |
| `XML2DOC004` | Documented symbol is missing a summary. |
| `XML2DOC005` | Unresolved `<inheritdoc />` target. |
| `XML2DOC006` | Multiple primary XML inputs define the same documentation member. |
| `XML2DOC007` | Multiple MSBuild projects claim the same generated aggregate index. |

CLI diagnostics use the stable format `xml2doc <severity> <code>: <message>`. Warnings do not fail generation; diagnostic errors return exit code `2`. MSBuild maps diagnostics to normal warning/error logging.

## Output ownership and incremental behavior

When stale pruning is enabled, Xml2Doc records output ownership under the output root:

```text
<output-root>/.xml2doc/
├── manifests/
│   └── <identity-sha256>.json
└── transactions/
```

Commit `.xml2doc/manifests` when generated Markdown is versioned and ownership must survive a clean checkout. Ignore `.xml2doc/transactions`; it is local best-effort staging state.

Normal MSBuild generation keeps its stamp, fingerprint, and output ledger under `IntermediateOutputPath`. Repository aggregation uses separate `xml2doc.aggregate.*` lifecycle files so one aggregate owner has one independent incremental state boundary.

Generated Markdown uses LF on every platform by default. Use `native` only when host-specific bytes are intentional.

## Rendering extensibility

Core consumers can replace individual rendering services through `RendererOptions`. Built-in extension points include:

- `IAnchorGenerator`
- `IAliasProvider`
- `ITemplateRenderer`
- `IAutoLinker`
- `IExternalSymbolResolver`
- `ISignatureRenderer`
- `SignatureStyle`

The CLI exposes the applicable built-in behaviors through flags and JSON configuration. Consumer-provided service implementations remain a direct `Xml2Doc.Core` integration concern.

## Troubleshooting

### Multiple projects overwrite `index.md`

Use a single repository aggregation owner with `Xml2Doc_AggregateEnabled=true`. If a referenced project still targets the same output and owns its index, Xml2Doc fails with `XML2DOC007`. Disable that project's normal generation or set `Xml2Doc_GenerateIndex=false`.

### Aggregate XML input is missing

Ensure every participating project has:

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

The aggregate owner fails rather than silently producing an incomplete index.

### Duplicate members across aggregate inputs

`XML2DOC006` means two primary XML inputs document the same member ID. Remove the duplicate primary input or correct the project/input boundary. Xml2Doc fails deterministically instead of selecting an owner based on input order.

### Removed source types leave Markdown behind

Use per-type mode with pruning enabled and a stable manifest identity. The first run establishes ownership; files predating the manifest require one manual cleanup.

### The MSBuild task cannot load `Xml2Doc.Core`

Use a current `Xml2Doc.MSBuild` package. Task runtime dependencies are packaged beside each task assembly for supported hosts.

### Debug task selection or evaluated paths

```xml
<PropertyGroup>
  <Xml2Doc_Dump>true</Xml2Doc_Dump>
  <Xml2Doc_LogChosenTask>true</Xml2Doc_LogChosenTask>
</PropertyGroup>
```

## Release history and roadmap

- `2.0.3` — documentation and lifecycle correctness.
- `2.1.0` — rendering extensibility.
- `2.2.0` — structured diagnostics and runner/pipeline completion.
- `2.3.0` — deterministic multi-project aggregation across Core, CLI, and MSBuild.
- `2.3.1` — aggregation, MSBuild clean, and Markdown list correctness fixes.

See [TODO.md](TODO.md), [docs/roadmap.md](docs/roadmap.md), and [the ADR index](docs/adr/README.md) for project history and future work.

Report bugs and feature requests in the [GitHub issue tracker](https://github.com/mod-posh/xml2doc/issues).
