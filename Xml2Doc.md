# Xml2Doc

Xml2Doc converts C# compiler XML documentation into deterministic, linkable Markdown. It is available as a library, CLI, and MSBuild package.

## Current status

The current stable package line is `2.1.0`. This release adds programmatic rendering extension
points while preserving the default `2.0.x` output contract:

- `Xml2Doc.Core` provides parsing, rendering, output ownership, stale-file pruning, and line-ending normalization.
- `Xml2Doc.Cli` provides repeatable command-line generation for development and CI.
- `Xml2Doc.MSBuild` generates documentation automatically after compilation.
- Per-type and single-file output are supported.
- Markdown uses LF on every platform by default.
- Multiple projects can safely share an output directory when they have distinct manifest identities and disable their project-owned indexes.
- Unified multi-project index aggregation is planned for `2.3.0`. Until then, maintain or generate the repository index separately.
- Portable ownership manifests were added in `2.0.3` and migrate safe 2.0.x manifests when they
  are next saved.
- `2.1.0` makes anchors, aliases, templates, free-text linking, external symbol resolution, and
  signature formatting replaceable through `RendererOptions`.

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
    <PackageReference Include="Xml2Doc.MSBuild" Version="2.1.0" PrivateAssets="all">
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

Keep string values such as the root namespace on one line. Surrounding whitespace is preserved and can prevent prefix matching:

```xml
<Xml2Doc_RootNamespaceToTrim>Rackspace.BAT.Core</Xml2Doc_RootNamespaceToTrim>
```

The package currently assigns `Xml2Doc_Toc`, `Xml2Doc_NamespaceIndex`, and `Xml2Doc_BasenameOnly` unconditionally in its imported defaults. To enable them, set them in the `.csproj` or `Directory.Build.targets`; a `Directory.Build.props` value may be overwritten.

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
| `Xml2Doc_Toc` | `false` | `true`, `false` | Emits a member table of contents where supported. Set in `.csproj` or `Directory.Build.targets`. |
| `Xml2Doc_NamespaceIndex` | `false` | `true`, `false` | Emits namespace index output in per-type mode. Set in `.csproj` or `Directory.Build.targets`. |
| `Xml2Doc_BasenameOnly` | `false` | `true`, `false` | Drops namespace segments from filenames and increases collision risk. Set in `.csproj` or `Directory.Build.targets`. |
| `Xml2Doc_ParallelDegree` | `1` | Positive integer | Maximum concurrent per-type renders. Values greater than `1` opt into bounded parallel generation; custom rendering extensions must be thread-safe. |
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
| `Xml2Doc_Diff` | `false` | Reserved | Has no effect in `2.1.0`. |

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
| `Xml2Doc_OutputLedger` | `$(IntermediateOutputPath)xml2doc.outputs.txt` | Records generated files so a missing output invalidates the incremental stamp. Keep this under the project/configuration-specific intermediate directory. |

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
  <Xml2Doc_ParallelDegree>4</Xml2Doc_ParallelDegree>
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
  <PackageReference Include="Xml2Doc.MSBuild" Version="2.1.0" PrivateAssets="all">
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

## Rendering extensibility in 2.1.0

`Xml2Doc.Core` consumers can replace individual rendering services through `RendererOptions`.
All extension points are optional; omitting them preserves the built-in output contract.

| Option | Extension point | Default behavior |
| --- | --- | --- |
| `AliasProvider` | `IAliasProvider` | C# keyword aliases from `DefaultAliasProvider` |
| `AnchorGenerator` | `IAnchorGenerator` | Selected built-in `AnchorAlgorithm` |
| `TemplateRenderer` | `ITemplateRenderer` | Built-in layout, or file-based template/front matter when paths are configured |
| `FrontMatter` | Metadata callback | No generated YAML front matter |
| `AutoLinker` | `IAutoLinker` | `SimpleAutoLinker` when `AutoLink=true` |
| `ExternalSymbolResolver` | `IExternalSymbolResolver` | Base-URL resolver when `ExternalDocs` is configured |
| `SignatureRenderer` | `ISignatureRenderer` | `DefaultSignatureRenderer` |
| `SignatureStyle` | Signature detail switches | Compatibility output without parameter names, constraints, or default values |

Example:

```csharp
var options = new RendererOptions(
    AnchorGenerator: new MyAnchorGenerator(),
    AliasProvider: new MyAliasProvider(),
    AutoLink: true,
    AutoLinker: new MyAutoLinker(),
    LinkPolicy: LinkPolicy.PreferExternalForUnknown,
    ExternalSymbolResolver: new MyExternalSymbolResolver(),
    SignatureStyle: new SignatureStyle(
        IncludeParamNames: true,
        IncludeConstraints: true,
        IncludeDefaultValues: true),
    SignatureRenderer: new MySignatureRenderer());
```

File-based templates, front matter, auto-linking, alias maps, and built-in anchor algorithms are
available through the CLI. Custom service implementations and external link-policy selection
require direct use of `Xml2Doc.Core`; broader CLI and MSBuild exposure is tracked for `2.2.0`.

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
dotnet tool install --global Xml2Doc.Cli --version 2.1.0

xml2doc `
  --xml .\bin\Release\net9.0\MyLibrary.xml `
  --out .\docs `
  --file-names clean `
  --rootns MyCompany.MyProduct `
  --parallel 4 `
  --line-endings lf
```

Safe per-type pruning additionally requires:

```powershell
--prune-stale --manifest-id MyCompany.MyLibrary
```

Use `--external-docs <base-url>` to route unresolved `cref` targets to an
external documentation site. Known symbols continue to use internal links.
The equivalent JSON configuration property is `ExternalDocs`. When the option
is omitted, all links retain the existing internal-only behavior.

Structured diagnostics are written to standard error in a stable CI-friendly
format: `xml2doc <severity> <code>: <message>`. Source locations and member IDs
are included when available. Warnings do not fail generation; diagnostic errors
return exit code `2`. Invalid CLI arguments return exit code `1`.

CLI validation rejects unknown options, missing option values, unsupported
filename or anchor modes, non-positive parallelism, missing or malformed JSON
configuration, and unknown JSON properties. `--toc`, `--namespace-index`, and
`--prune-stale` require directory output. `--dry-run` and `--diff` are mutually
exclusive because diff already performs a non-mutating comparison. Validation
failures do not create or modify generated output.

When `--report` is configured, the CLI report includes deterministic
`plannedFiles`, `writtenFiles`, `skippedFiles`, and `prunedFiles` arrays plus
runner timing fields. Dry runs leave the actual-result arrays empty and populate
`wouldWrite` and `wouldDelete` without modifying Markdown or ownership state.
CLI reports omit timestamps so otherwise identical invocations do not differ
solely because of wall-clock time.

Use `--diff` to compare the Markdown that would be generated with the current
output without modifying generated files, ownership manifests, or transaction
state. The console summary and optional report classify absolute paths as
`addedFiles`, `changedFiles`, `unchangedFiles`, or `removedFiles`. Removed files
are limited to stale outputs owned by the selected manifest identity when
pruning is enabled. Diff returns exit code `0` when output is current and `3`
when differences are found, making it suitable for CI drift checks.

Use `--single` when `--out` names one combined file. Pruning is unavailable in single-file mode.

## Troubleshooting

### Full namespaces remain in filenames

Embedded newlines or indentation can prevent an exact prefix match. Inspect the evaluated values:

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

- `2.0.3`: released documentation and lifecycle correctness.
- `2.1.0`: released rendering extensibility.
- `2.2.0`: current diagnostics and pipeline milestone.
- `2.3.0`: deterministic multi-project aggregation and a unified index.

Report bugs and feature requests in the [GitHub issue tracker](https://github.com/mod-posh/xml2doc/issues).
