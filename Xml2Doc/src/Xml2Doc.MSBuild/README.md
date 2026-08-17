# Xml2Doc.MSBuild

MSBuild integration for Xml2Doc, part of the **mod-posh** organization.

## Overview

`Xml2Doc.MSBuild` adds automatic documentation generation to your build.
After a successful compile, it converts the compiler-generated XML docs into Markdown using `Xml2Doc.Core`.

For repositories that need one documentation set from multiple projects, the package also supports an explicit repository aggregation owner. The owner consumes all participating XML documentation in one Core aggregation operation and produces one deterministic combined index.

### Multi-framework support (task host)

The task assembly is multi-targeted:

- **`net472`** — used by **Visual Studio 2022** MSBuild (full .NET Framework host)
- **`net8.0`** — used by the **`dotnet` SDK** MSBuild

This means:

- Building from **VS UI / MSBuild.exe** → task runs on `net472`.
- Building from **`dotnet build`** → task runs on `net8.0`.

> Output Markdown is identical regardless of host TFM. The package maps to the appropriate `Xml2Doc.Core` target under the hood.

---

## Setup

Add a package reference to your project:

```xml
<ItemGroup>
  <PackageReference Include="Xml2Doc.MSBuild" Version="2.2.0" PrivateAssets="all" />
</ItemGroup>
```

That’s it—on successful build, docs are generated according to the properties below.

---

## Configuration (MSBuild properties)

| Property | Description |
| --- | --- |
| `Xml2Doc_Enabled` | Enable/disable normal per-project generation. Default: `true`. |
| `Xml2Doc_SingleFile` | `true` = generate one combined Markdown file; `false` = per-type files. |
| `Xml2Doc_OutputFile` | Output file path when `SingleFile=true`. |
| `Xml2Doc_OutputDir` | Output directory when `SingleFile=false`. |
| `Xml2Doc_GenerateIndex` | Generate `index.md` in per-type mode. Default: `true`. |
| `Xml2Doc_FileNameMode` | `verbatim` or `clean`. |
| `Xml2Doc_RootNamespaceToTrim` | Optional namespace prefix trimmed from display names. |
| `Xml2Doc_CodeBlockLanguage` | Code block language for fenced blocks. Default: `csharp`. |
| `Xml2Doc_PruneStaleFiles` | Remove stale files owned by this invocation. Default: `false`. |
| `Xml2Doc_ManifestIdentity` | Stable identity required when stale-output pruning is enabled. |
| `Xml2Doc_LineEndings` | Markdown newlines: `lf`, `crlf`, or `native`. |
| `Xml2Doc_AggregateEnabled` | Make this project the repository aggregation owner. Default: `false`. |
| `Xml2Doc_AggregateValidateIndexOwnership` | Validate referenced Xml2Doc projects do not also own the aggregate `index.md`. Default: `true`. |
| `Xml2Doc_AggregateReportPath` | Aggregate report path. Default: `$(Xml2Doc_OutputDir)\xml2doc-aggregate-report.json`. |

Referenced-project XML documentation found beside project output assemblies is loaded
automatically for `<inheritdoc />` resolution without generating pages for referenced
types. Additional reference documentation files can be supplied as items:

```xml
<ItemGroup>
  <Xml2Doc_ReferenceXml Include="path\to\Contracts.xml" />
</ItemGroup>
```

### Examples

**Single file (good for READMEs / wikis)**

```xml
<PropertyGroup>
  <Xml2Doc_SingleFile>true</Xml2Doc_SingleFile>
  <Xml2Doc_OutputFile>$(ProjectDir)docs\api.md</Xml2Doc_OutputFile>
  <Xml2Doc_FileNameMode>clean</Xml2Doc_FileNameMode>
  <Xml2Doc_RootNamespaceToTrim>MyCompany.MyProduct</Xml2Doc_RootNamespaceToTrim>
</PropertyGroup>
```

**Repository aggregation owner**

Use one project as the owner when multiple projects contribute to the same documentation set. The owner can use project references as automatic aggregate inputs:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>

    <Xml2Doc_Enabled>false</Xml2Doc_Enabled>
    <Xml2Doc_AggregateEnabled>true</Xml2Doc_AggregateEnabled>
    <Xml2Doc_OutputDir>$(MSBuildProjectDirectory)\..\docs\api</Xml2Doc_OutputDir>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Alpha\Alpha.csproj" />
    <ProjectReference Include="..\Zulu\Zulu.csproj" />
    <PackageReference Include="Xml2Doc.MSBuild" Version="2.2.0" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Each participating project must set `GenerateDocumentationFile=true`. When the owner builds, `Xml2Doc_Aggregate` collects the XML documentation beside the resolved project-reference assemblies, calls Core aggregation once, and writes one deterministic output set.

Inputs that are not project references can be added explicitly:

```xml
<ItemGroup>
  <Xml2Doc_AggregateXml Include="path\to\External.Contracts.xml" />
</ItemGroup>
```

The owner should be the only invocation that writes the aggregate `index.md`. A participating project that also uses Xml2Doc and writes to the same `Xml2Doc_OutputDir` should either disable normal Markdown generation:

```xml
<PropertyGroup>
  <Xml2Doc_Enabled>false</Xml2Doc_Enabled>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

or keep its type-page generation but delegate index ownership:

```xml
<PropertyGroup>
  <Xml2Doc_GenerateIndex>false</Xml2Doc_GenerateIndex>
</PropertyGroup>
```

For project-reference participants, the owner validates this before normal referenced-project builds begin. A conflicting index owner fails with `XML2DOC007` and identifies the project and shared output path. Set `Xml2Doc_AggregateValidateIndexOwnership=false` only if higher-level repository orchestration already guarantees exclusive ownership.

Aggregate lifecycle state is kept separate from ordinary project generation through `xml2doc.aggregate.stamp`, `xml2doc.aggregate.fingerprint.txt`, and `xml2doc.aggregate.outputs.txt` under the owner's intermediate output directory. The aggregate report includes canonical `xmlInputs`.

**Per-type files (good for large APIs)**

```xml
<PropertyGroup>
  <Xml2Doc_SingleFile>false</Xml2Doc_SingleFile>
  <Xml2Doc_OutputDir>$(ProjectDir)docs</Xml2Doc_OutputDir>
  <Xml2Doc_FileNameMode>clean</Xml2Doc_FileNameMode>
</PropertyGroup>
```

**Safely prune stale per-type output**

```xml
<PropertyGroup>
  <Xml2Doc_SingleFile>false</Xml2Doc_SingleFile>
  <Xml2Doc_OutputDir>$(ProjectDir)docs</Xml2Doc_OutputDir>
  <Xml2Doc_PruneStaleFiles>true</Xml2Doc_PruneStaleFiles>
  <Xml2Doc_ManifestIdentity>$(MSBuildProjectFullPath)</Xml2Doc_ManifestIdentity>
</PropertyGroup>
```

Use an identity that remains stable for the same invocation. Only files recorded by that exact
identity can be removed; hand-authored files and files owned by other builds are preserved.
Pruning is supported only for per-type output.

**Line-ending policy**

Generated Markdown uses LF on every platform by default. Consumers that require another policy can
select it explicitly:

```xml
<PropertyGroup>
  <Xml2Doc_LineEndings>crlf</Xml2Doc_LineEndings>
</PropertyGroup>
```

Use `native` only for host-specific compatibility. A `.gitattributes` rule such as
`*.md text eol=lf` can reinforce repository policy but is not required for deterministic output.

**Only generate in Release**

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <Xml2Doc_Enabled>true</Xml2Doc_Enabled>
</PropertyGroup>
<PropertyGroup Condition="'$(Configuration)' != 'Release'">
  <Xml2Doc_Enabled>false</Xml2Doc_Enabled>
</PropertyGroup>
```

---

## Visual Studio notes

- The package includes a **`net472`** task so it runs inside **Visual Studio 2022** builds without extra tooling.
- If you don’t see output, check **Build Output** for the Xml2Doc task messages and verify participating projects emit XML docs (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`).

---

## CI notes

- Works with `dotnet build` (task will load the **`net8.0`** target).
- Repository aggregation is designed so parallel and serial project scheduling produce the same aggregate bytes.
- Recommended pattern:

  ```powershell
  dotnet build MySolution.sln -c Release
  dotnet build .\docs\ApiDocs.csproj -c Release
  ```

---

## Troubleshooting

- **No files produced**: ensure participating projects generate XML documentation.
- **Aggregate input missing**: build the participating project and ensure `GenerateDocumentationFile=true`.
- **`XML2DOC007`**: a referenced project is still configured to own `index.md` in the aggregate output. Disable `Xml2Doc_GenerateIndex` or normal Xml2Doc generation for that project.
- **Want to disable temporarily?** Set `<Xml2Doc_Enabled>false</Xml2Doc_Enabled>` for normal project generation or `<Xml2Doc_AggregateEnabled>false</Xml2Doc_AggregateEnabled>` for the owner.

---

## Versioning / Support

- Task hosts: **`net472`** (VS/MSBuild.exe), **`net8.0`** (`dotnet` SDK).
- Backed by `Xml2Doc.Core` targets: `netstandard2.0;net8.0;net9.0` with identical rendering across TFMs.
