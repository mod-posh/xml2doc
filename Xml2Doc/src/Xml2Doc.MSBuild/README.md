# Xml2Doc.MSBuild

MSBuild integration for Xml2Doc, part of the **mod-posh** organization.

## Overview

`Xml2Doc.MSBuild` converts compiler-generated C# XML documentation into Markdown automatically during a build.

Version `2.3.1` supports two models and adds configuration-scoped cleanup of normal and aggregate incremental state:

- normal per-project generation from one compiler XML file;
- repository aggregation, where one owner project renders multiple participating projects into one deterministic documentation set and one unified index.

## Task hosts

The package contains task assemblies for:

- `net472` — Visual Studio 2022 / full-framework MSBuild hosts;
- `net8.0` — `dotnet` SDK MSBuild hosts.

The package selects the appropriate task assembly automatically. Generated Markdown is expected to remain equivalent across supported task hosts for the same inputs and options.

## Install

```xml
<ItemGroup>
  <PackageReference Include="Xml2Doc.MSBuild" Version="2.3.1" PrivateAssets="all" />
</ItemGroup>
```

Projects that contribute compiler XML must enable:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

## Normal project generation

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>

    <Xml2Doc_Enabled>true</Xml2Doc_Enabled>
    <Xml2Doc_OutputDir>$(MSBuildProjectDirectory)\docs</Xml2Doc_OutputDir>
    <Xml2Doc_FileNameMode>clean</Xml2Doc_FileNameMode>
    <Xml2Doc_LineEndings>lf</Xml2Doc_LineEndings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Xml2Doc.MSBuild" Version="2.3.1" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

### Normal-generation properties

| Property | Default | Description |
| --- | --- | --- |
| `Xml2Doc_Enabled` | `true` | Enable or disable normal per-project generation. |
| `Xml2Doc_SingleFile` | `false` | Generate one combined file instead of per-type files. |
| `Xml2Doc_OutputFile` | `$(MSBuildProjectDirectory)\docs\api.md` | Single-file output path. |
| `Xml2Doc_OutputDir` | `$(MSBuildProjectDirectory)\docs` | Per-type output directory. |
| `Xml2Doc_GenerateIndex` | `true` | Generate `index.md` in per-type mode. |
| `Xml2Doc_FileNameMode` | `clean` | `clean` or `verbatim`. |
| `Xml2Doc_RootNamespaceToTrim` | Empty | Namespace prefix trimmed from display names. |
| `Xml2Doc_TrimRootNamespaceInFileNames` | `false` | Also trim the root namespace from filenames. |
| `Xml2Doc_CodeBlockLanguage` | `csharp` | Fenced-code language. |
| `Xml2Doc_AnchorAlgorithm` | `default` | `default`, `github`, `gfm`, or `kramdown`. |
| `Xml2Doc_Toc` | `false` | Emit member tables of contents. |
| `Xml2Doc_NamespaceIndex` | `false` | Emit namespace index output. |
| `Xml2Doc_BasenameOnly` | `false` | Use basename-only output names/links. |
| `Xml2Doc_ParallelDegree` | `1` | Maximum per-type render parallelism. |
| `Xml2Doc_PruneStaleFiles` | `false` | Remove stale output owned by this invocation. Per-type mode only. |
| `Xml2Doc_ManifestIdentity` | Empty | Stable identity required for stale pruning. |
| `Xml2Doc_LineEndings` | `lf` | `lf`, `crlf`, or `native`. |
| `Xml2Doc_ReportPath` | `$(Xml2Doc_OutputDir)\xml2doc-report.json` | JSON report path. |
| `Xml2Doc_ReportIncludeTimestamp` | `false` | Include a report timestamp. |
| `Xml2Doc_DryRun` | `false` | Plan without writing Markdown. |
| `Xml2Doc_Diff` | `false` | Reserved; currently no effect in the MSBuild task. |
| `Xml2Doc_Dump` | `false` | Log evaluated generation paths/settings. |
| `Xml2Doc_LogChosenTask` | `false` | Log selected task TFM/assembly. |

`Xml2Doc_Toc`, `Xml2Doc_NamespaceIndex`, and `Xml2Doc_BasenameOnly` are assigned by package defaults. When enabling them globally, set them in the project file or `Directory.Build.targets` rather than relying on an earlier `Directory.Build.props` value.

## Referenced XML for `<inheritdoc />`

XML documentation beside resolved project-reference assemblies is loaded automatically for inheritance lookup without generating pages for those referenced symbols.

Additional reference XML can be supplied explicitly:

```xml
<ItemGroup>
  <Xml2Doc_ReferenceXml Include="path\to\Contracts.xml" />
</ItemGroup>
```

Reference XML participates in incremental fingerprinting. Missing explicit reference files and unresolved inheritance targets are reported as warnings.

## Repository aggregation owner

Use one small project as the owner when multiple projects contribute to one documentation set:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>

    <!-- The owner orchestrates generation; it does not document itself. -->
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <Xml2Doc_Enabled>false</Xml2Doc_Enabled>
    <Xml2Doc_AggregateEnabled>true</Xml2Doc_AggregateEnabled>

    <Xml2Doc_OutputDir>$(MSBuildProjectDirectory)\..\docs\api</Xml2Doc_OutputDir>
    <Xml2Doc_FileNameMode>clean</Xml2Doc_FileNameMode>
    <Xml2Doc_LineEndings>lf</Xml2Doc_LineEndings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Alpha\Alpha.csproj" />
    <ProjectReference Include="..\Zulu\Zulu.csproj" />
    <PackageReference Include="Xml2Doc.MSBuild" Version="2.3.1" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Every participating project must emit compiler XML:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

When the owner builds, `Xml2Doc_Aggregate` resolves project references, derives their XML documentation paths, canonicalizes the primary inputs, loads them through Core aggregation, and renders one output set.

If an expected primary XML file is missing, aggregation fails rather than silently producing incomplete documentation.

### Explicit primary aggregate inputs

Inputs that are not project references can be added with `Xml2Doc_AggregateXml`:

```xml
<ItemGroup>
  <Xml2Doc_AggregateXml Include="$(RepoRoot)\artifacts\External.Contracts.xml" />
</ItemGroup>
```

`Xml2Doc_AggregateXml` creates documentation pages. `Xml2Doc_ReferenceXml` remains reference-only and is used for inheritance/reference resolution.

### Index ownership

The aggregation owner should be the only invocation that writes the aggregate `index.md`.

The cleanest participant configuration is:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <Xml2Doc_Enabled>false</Xml2Doc_Enabled>
</PropertyGroup>
```

If a participant still generates its own type pages into the same directory, delegate index ownership:

```xml
<PropertyGroup>
  <Xml2Doc_GenerateIndex>false</Xml2Doc_GenerateIndex>
</PropertyGroup>
```

With project-reference participants, `Xml2Doc_AggregateValidateIndexOwnership` checks for conflicting index ownership before normal referenced-project builds begin. A conflicting participant fails with `XML2DOC007` and identifies the project/output path.

Set `Xml2Doc_AggregateValidateIndexOwnership=false` only when higher-level orchestration already guarantees exclusive ownership.

### Aggregate properties

| Property | Default | Description |
| --- | --- | --- |
| `Xml2Doc_AggregateEnabled` | `false` | Make this project the repository aggregation owner. |
| `Xml2Doc_AggregateValidateIndexOwnership` | `true` | Validate referenced projects do not also own the aggregate index. |
| `Xml2Doc_AggregateReportPath` | `$(Xml2Doc_OutputDir)\xml2doc-aggregate-report.json` | Aggregate report path. |
| `Xml2Doc_AggregateOutputStamp` | `$(IntermediateOutputPath)xml2doc.aggregate.stamp` | Aggregate successful-generation stamp. |
| `Xml2Doc_AggregateFingerprintFile` | `$(IntermediateOutputPath)xml2doc.aggregate.fingerprint.txt` | Aggregate input/options fingerprint. |
| `Xml2Doc_AggregateOutputLedger` | `$(IntermediateOutputPath)xml2doc.aggregate.outputs.txt` | Aggregate generated-file ledger. |

The aggregation owner reuses normal renderer properties such as output mode, output paths, filename mode, root namespace trimming, anchor algorithm, index generation, pruning, manifest identity, parallelism, and line endings.

Aggregate primary input identities and explicit reference XML identities participate in fingerprinting. Primary/reference files are MSBuild target inputs, so changing the XML, participation, significant rendering options, or a recorded generated file causes regeneration. With `Xml2Doc_LineEndings=native`, the host newline policy also participates in the fingerprint.

## Determinism

Core sorts aggregate inputs canonically and combines symbols in stable ordinal order. The aggregate report records the canonical primary list as `xmlInputs`.

Repository integration tests build the same owner with normal parallel scheduling and with `/m:1`, on Windows and Linux, and require identical generated file sets and identical bytes.

If two primary XML inputs define the same documentation member, generation fails with `XML2DOC006` rather than choosing an owner based on input order.

## Stale output and lifecycle files

For normal per-project generation, `Xml2Doc_OutputStamp`, `Xml2Doc_FingerprintFile`, and `Xml2Doc_OutputLedger` default under `IntermediateOutputPath`.

Aggregation uses separate `xml2doc.aggregate.*` lifecycle files so the repository owner has one independent incremental state boundary.

`dotnet clean --configuration <Configuration>` removes the normal and aggregate Xml2Doc lifecycle files for the selected configuration. Generated Markdown, reports, ownership manifests, and other configurations' state remain intact. A subsequent build recomputes the fingerprint and regenerates documentation when required.

Stale Markdown pruning is supported only in per-type mode and requires a stable `Xml2Doc_ManifestIdentity`. Xml2Doc removes only paths recorded for that identity.

## Line endings

Generated Markdown uses LF on every platform by default:

```xml
<PropertyGroup>
  <Xml2Doc_LineEndings>lf</Xml2Doc_LineEndings>
</PropertyGroup>
```

Use `crlf` only when required by a downstream consumer. Use `native` only when host-specific bytes are intentional.

## Conditional generation

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <Xml2Doc_Enabled>true</Xml2Doc_Enabled>
</PropertyGroup>
<PropertyGroup Condition="'$(Configuration)' != 'Release'">
  <Xml2Doc_Enabled>false</Xml2Doc_Enabled>
</PropertyGroup>
```

## CI

Normal project build:

```powershell
dotnet build .\src\MyLibrary\MyLibrary.csproj -c Release
```

Repository aggregation owner:

```powershell
dotnet build .\docs\ApiDocs.csproj -c Release
```

## Troubleshooting

- **No files produced:** ensure the relevant project emits XML documentation with `GenerateDocumentationFile=true`.
- **Aggregate input missing:** ensure every participating project emits XML documentation and is included by project reference or `Xml2Doc_AggregateXml`.
- **`XML2DOC006`:** two primary aggregate inputs document the same member ID; fix the input boundary.
- **`XML2DOC007`:** a referenced project still claims `index.md` in the aggregate output; disable its normal generation or set `Xml2Doc_GenerateIndex=false`.
- **Unexpected task load failure:** use the current package and inspect the selected task with `<Xml2Doc_LogChosenTask>true</Xml2Doc_LogChosenTask>`.

For the repository-owner pattern in more detail, see [`docs/msbuild-repository-aggregation.md`](../../../docs/msbuild-repository-aggregation.md).
