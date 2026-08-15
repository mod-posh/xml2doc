# Xml2Doc

Xml2Doc converts C# compiler XML documentation into deterministic, linkable Markdown. It is available as a library, CLI, and MSBuild package.

## Current status

The current stable package line is `2.0.2`:

- `Xml2Doc.Core` provides parsing, rendering, output ownership, stale-file pruning, and line-ending normalization.
- `Xml2Doc.Cli` provides repeatable command-line generation for development and CI.
- `Xml2Doc.MSBuild` generates documentation automatically after compilation.
- Per-type and single-file output are supported.
- Markdown uses LF on every platform by default.
- Multiple projects can safely share an output directory when they have distinct manifest identities and disable their project-owned indexes.
- Unified multi-project index aggregation is planned for `2.3.0`. Until then, maintain or generate the repository index separately.
- Portable ownership manifests are implemented for the upcoming `2.0.3` release and migrate safe
  2.0.x manifests when they are next saved.

## Supported frameworks

| Component | Target frameworks | Purpose |
| --- | --- | --- |
| `Xml2Doc.Core` | `netstandard2.0`, `net8.0`, `net9.0` | Library and renderer |
| `Xml2Doc.Cli` | `net8.0`, `net9.0` | Command-line host |
| `Xml2Doc.MSBuild` | `net472`, `net8.0` | Visual Studio/MSBuild.exe and `dotnet build` task hosts |

The MSBuild package selects its task assembly automatically. Do not define a custom `GenerateMarkdownFromXmlDoc` target or manually call `Xml2Doc_ComputeFingerprint`.

## Install the MSBuild package

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Xml2Doc.MSBuild" Version="2.0.2" PrivateAssets="all">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

`GenerateDocumentationFile` must be `true`. The package imports its own generation and incremental-build targets.

## Where properties belong

Most Xml2Doc properties can be placed in either:

- A `.csproj` for project-specific behavior.
- A solution-level `Directory.Build.props` for shared behavior.

Project properties override `Directory.Build.props`. Avoid defining a property in both unless the override is intentional.

Keep string values such as the root namespace on one line. In 2.0.2, surrounding whitespace is preserved and can prevent prefix matching:

```xml
<Xml2Doc_RootNamespaceToTrim>Rackspace.BAT.Core</Xml2Doc_RootNamespaceToTrim>
```

The package currently assigns `Xml2Doc_Toc`, `Xml2Doc_NamespaceIndex`, and `Xml2Doc_BasenameOnly` unconditionally in its imported defaults. To enable them in 2.0.2, set them in the `.csproj` or `Directory.Build.targets`; a `Directory.Build.props` value may be overwritten.

## Complete MSBuild property reference

### Generation and output

| Property | Default | Values | Behavior |
| --- | --- | --- | --- |
| `GenerateDocumentationFile` | SDK-dependent | `true`, `false` | Required compiler property. Set it to `true` for participating projects. |
| `Xml2Doc_Enabled` | `true` | `true`, `false` | Enables or disables automatic generation. |
| `Xml2Doc_SingleFile` | `false` | `true`, `false` | `false` writes one file per type; `true` writes one combined file. |
| `Xml2Doc_OutputDir` | `$(MSBuildProjectDirectory)\docs` | Directory path | Output root for per-type mode. |
| `Xml2Doc_OutputFile` | `$(MSBuildProjectDirectory)\docs\api.md` | File path | Output used in single-file mode. |
| `Xml2Doc_GenerateIndex` | `true` | `true`, `false` | Writes `index.md` in per-type mode. Set `false` when independent projects share a directory. |

Stale-file pruning is supported only in per-type mode. Single-file mode replaces its configured output directly.

Xml2Doc automatically loads XML documentation found beside referenced project
assemblies for `<inheritdoc />` resolution. Referenced members participate only in
inheritance lookup and do not generate additional Markdown pages. Extra XML files can
be supplied explicitly when an assembly and its documentation are not colocated:

```xml
<ItemGroup>
  <Xml2Doc_ReferenceXml Include="$(BaseOutputPath)Contracts\Contracts.xml" />
</ItemGroup>
```

Missing explicit files and unresolved `<inheritdoc />` targets produce MSBuild warnings.
Reference XML contents are included in incremental fingerprinting.

### Naming, links, and rendering

| Property | Default | Values | Behavior |
| --- | --- | --- | --- |
| `Xml2Doc_FileNameMode` | `clean` | `clean`, `verbatim` | `clean` removes generic arity and normalizes generic notation; `verbatim` preserves documentation identifiers. |
| `Xml2Doc_RootNamespaceToTrim` | Empty | Namespace prefix | Removes a matching prefix from displayed type names. |
| `Xml2Doc_TrimRootNamespaceInFileNames` | `false` | `true`, `false` | Also removes the configured root namespace from filenames. |
| `Xml2Doc_CodeBlockLanguage` | `csharp` | Fence language | Default fenced-code language. |
| `Xml2Doc_AnchorAlgorithm` | `default` | `default`, `github`, `gfm`, `kramdown` | Controls heading/member anchors. Changing it can break published fragment links. |
| `Xml2Doc_Toc` | `false` | `true`, `false` | Emits a member table of contents where supported. Set in `.csproj` or `Directory.Build.targets` in 2.0.2. |
| `Xml2Doc_NamespaceIndex` | `false` | `true`, `false` | Emits namespace index output in per-type mode. Set in `.csproj` or `Directory.Build.targets` in 2.0.2. |
| `Xml2Doc_BasenameOnly` | `false` | `true`, `false` | Drops namespace segments from filenames and increases collision risk. Set in `.csproj` or `Directory.Build.targets` in 2.0.2. |
| `Xml2Doc_LineEndings` | `lf` | `lf`, `crlf`, `native` | Normalizes generated Markdown at the output boundary. `lf` is deterministic across hosts. |

Filename processing order is: filename-mode normalization, optional root-namespace trimming, then optional basename-only reduction.

### Ownership and stale cleanup

| Property | Default | Values | Behavior |
| --- | --- | --- | --- |
| `Xml2Doc_PruneStaleFiles` | `false` | `true`, `false` | After successful per-type generation, removes files previously owned by the same identity that are no longer generated. |
| `Xml2Doc_ManifestIdentity` | Empty | Stable string | Required for pruning. Use a distinct stable value for every project sharing an output directory, such as `$(MSBuildProjectName)`. |

Xml2Doc deletes only stale paths recorded in the prior manifest for the same identity. It does not claim hand-authored documents or files owned by another project.

Lifecycle metadata is stored under:

```text
<output-root>/.xml2doc/
├── manifests/
│   └── <identity-sha256>.json
└── transactions/
```

The transactions directory is normally empty after a successful build. Temporary child directories
stage stale files while the manifest is replaced and are then removed. Starting in 2.0.3, commit
`.xml2doc/manifests` when generated Markdown is versioned so a clean checkout retains ownership
history. Always ignore `.xml2doc/transactions`; it is local, best-effort staging state.

### Reports and diagnostics

| Property | Default | Values | Behavior |
| --- | --- | --- | --- |
| `Xml2Doc_ReportPath` | `$(Xml2Doc_OutputDir)\xml2doc-report.json` | File path | Writes a JSON report. Use unique names for projects sharing a directory. Reports are not needed for pruning. |
| `Xml2Doc_ReportIncludeTimestamp` | `false` | `true`, `false` | Adds a timestamp. Leave false for deterministic reports. |
| `Xml2Doc_DryRun` | `false` | `true`, `false` | Plans output without writing Markdown; a configured report can still be written. |
| `Xml2Doc_Dump` | `false` | `true`, `false` | Logs evaluated paths and key generation inputs. |
| `Xml2Doc_LogChosenTask` | `false` | `true`, `false` | Logs the selected task TFM and assembly path. |
| `Xml2Doc_Diff` | `false` | Reserved | Has no effect in 2.0.2. |

Reports and transaction staging may be ignored:

```gitignore
docs/xml2doc-report*.json
docs/.xml2doc/transactions/
```

### Incremental-build controls

These are advanced settings; most consumers should retain the defaults.

| Property | Default | Behavior |
| --- | --- | --- |
| `Xml2Doc_OutputStamp` | `$(IntermediateOutputPath)xml2doc.stamp` | Successful-generation stamp used by MSBuild input/output tracking. Relative paths are rooted under `IntermediateOutputPath`. |
| `Xml2Doc_FingerprintFile` | `$(IntermediateOutputPath)xml2doc.fingerprint.txt` | Stores a fingerprint of XML content and significant options. Relative paths are rooted under `IntermediateOutputPath`. |

Do not add an `Xml2Doc_WriteFingerprint` target. The imported targets already manage fingerprinting and generation.

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
  <Xml2Doc_LineEndings>lf</Xml2Doc_LineEndings>
</PropertyGroup>
```

## Recommended shared-output configuration

Put shared settings in the solution-level `Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <Xml2Doc_Enabled>true</Xml2Doc_Enabled>
    <Xml2Doc_SingleFile>false</Xml2Doc_SingleFile>
    <Xml2Doc_OutputDir>$(MSBuildThisFileDirectory)docs</Xml2Doc_OutputDir>

    <Xml2Doc_FileNameMode>clean</Xml2Doc_FileNameMode>
    <Xml2Doc_RootNamespaceToTrim>Rackspace.BAT.Core</Xml2Doc_RootNamespaceToTrim>
    <Xml2Doc_TrimRootNamespaceInFileNames>true</Xml2Doc_TrimRootNamespaceInFileNames>
    <Xml2Doc_CodeBlockLanguage>csharp</Xml2Doc_CodeBlockLanguage>

    <Xml2Doc_GenerateIndex>false</Xml2Doc_GenerateIndex>
    <Xml2Doc_PruneStaleFiles>true</Xml2Doc_PruneStaleFiles>
    <Xml2Doc_ManifestIdentity>$(MSBuildProjectName)</Xml2Doc_ManifestIdentity>

    <Xml2Doc_ReportPath>$(Xml2Doc_OutputDir)\xml2doc-report-$(MSBuildProjectName).json</Xml2Doc_ReportPath>
    <Xml2Doc_LineEndings>lf</Xml2Doc_LineEndings>
  </PropertyGroup>
</Project>
```

Each participating `.csproj` still needs:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Xml2Doc.MSBuild" Version="2.0.2" PrivateAssets="all">
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

Retaining a project-distinguishing namespace segment prevents collisions. Trimming `Rackspace.BAT.Core`, for example, produces:

```text
Abstractions.Attributes.RequiredAttribute.md
Engine.Execution.BatExecutionService.md
```

Do not enable `Xml2Doc_BasenameOnly` for a shared flat directory unless duplicate type names are impossible.

Each invocation sees only its own compiler XML, so it cannot safely build an index containing the other projects. Keep `Xml2Doc_GenerateIndex=false` and maintain `docs/index.md` separately until multi-input aggregation is available.

## Conditional generation

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <Xml2Doc_Enabled>true</Xml2Doc_Enabled>
</PropertyGroup>
<PropertyGroup Condition="'$(Configuration)' != 'Release'">
  <Xml2Doc_Enabled>false</Xml2Doc_Enabled>
</PropertyGroup>
```

Disable it for one command:

```powershell
dotnet build -p:Xml2Doc_Enabled=false
```

Inspect evaluated configuration:

```powershell
dotnet msbuild .\src\MyProject\MyProject.csproj `
  -getProperty:Xml2Doc_Enabled `
  -getProperty:Xml2Doc_OutputDir `
  -getProperty:Xml2Doc_RootNamespaceToTrim `
  -getProperty:Xml2Doc_GenerateIndex `
  -getProperty:Xml2Doc_PruneStaleFiles `
  -getProperty:Xml2Doc_ManifestIdentity `
  -getProperty:Xml2Doc_LineEndings
```

## Single-file configuration

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <Xml2Doc_SingleFile>true</Xml2Doc_SingleFile>
  <Xml2Doc_OutputFile>$(MSBuildProjectDirectory)\docs\api.md</Xml2Doc_OutputFile>
  <Xml2Doc_FileNameMode>clean</Xml2Doc_FileNameMode>
  <Xml2Doc_RootNamespaceToTrim>MyCompany.MyProduct</Xml2Doc_RootNamespaceToTrim>
  <Xml2Doc_LineEndings>lf</Xml2Doc_LineEndings>
</PropertyGroup>
```

Do not enable stale pruning in single-file mode; validation will fail.

## CLI quick start

```powershell
dotnet tool install --global Xml2Doc.Cli --version 2.0.2

xml2doc `
  --xml .\bin\Release\net9.0\MyLibrary.xml `
  --out .\docs `
  --file-names clean `
  --rootns MyCompany.MyProduct `
  --line-endings lf
```

Safe per-type pruning additionally requires:

```powershell
--prune-stale --manifest-id MyCompany.MyLibrary
```

Use `--single` when `--out` names one combined file. Pruning is unavailable in single-file mode.

## Troubleshooting

### Full namespaces remain in filenames

Embedded newlines or indentation can prevent an exact prefix match in 2.0.2. Inspect the evaluated values:

```powershell
dotnet msbuild .\MyProject.csproj `
  -getProperty:Xml2Doc_RootNamespaceToTrim `
  -getProperty:Xml2Doc_TrimRootNamespaceInFileNames
```

### Shared projects overwrite `index.md`

Set `Xml2Doc_GenerateIndex=false` for every independent project using the directory. A combined index requires a separate aggregation step until 2.3.0.

### Removed source types leave Markdown behind

Use per-type mode with pruning enabled and a stable manifest identity. The first run establishes ownership; files predating the manifest require one manual cleanup.

### The task cannot load `Xml2Doc.Core`

Use `Xml2Doc.MSBuild` 2.0.2 or later. Version 2.0.2 packages the task's runtime dependencies beside each task assembly.

### Debug task selection or paths

Temporarily enable:

```xml
<Xml2Doc_Dump>true</Xml2Doc_Dump>
<Xml2Doc_LogChosenTask>true</Xml2Doc_LogChosenTask>
```

## Roadmap

See [TODO.md](TODO.md) and [the ADR index](docs/adr/README.md). Notable planned work includes:

- `2.0.3`: documentation and lifecycle correctness ([#68](https://github.com/mod-posh/xml2doc/issues/68), [#69](https://github.com/mod-posh/xml2doc/issues/69), and [#77](https://github.com/mod-posh/xml2doc/issues/77)).
- `2.1.0`: rendering extensibility.
- `2.2.0`: diagnostics and pipeline improvements.
- `2.3.0`: deterministic multi-project aggregation and a unified index.

Report bugs and feature requests in the [GitHub issue tracker](https://github.com/mod-posh/xml2doc/issues).
