# MSBuild repository aggregation

Use repository aggregation when more than one project contributes XML documentation to one Markdown output set. The rule is simple: **one MSBuild project owns the aggregate output**. Participating projects emit compiler XML; the owner calls Xml2Doc once with all primary XML inputs.

This avoids the unsafe pattern where independent projects all write the same `index.md` and whichever project finishes last wins.

Repository aggregation is available in `Xml2Doc.MSBuild` `2.3.0`.

## Aggregation owner

Add `Xml2Doc.MSBuild` to a small repository-level project and reference every project that participates in the documentation set:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>

    <!-- This project orchestrates Xml2Doc; it does not render its own XML docs. -->
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <Xml2Doc_Enabled>false</Xml2Doc_Enabled>
    <Xml2Doc_AggregateEnabled>true</Xml2Doc_AggregateEnabled>

    <Xml2Doc_OutputDir>$(MSBuildProjectDirectory)\..\docs\api</Xml2Doc_OutputDir>
    <Xml2Doc_FileNameMode>clean</Xml2Doc_FileNameMode>
    <Xml2Doc_LineEndings>lf</Xml2Doc_LineEndings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\src\Alpha\Alpha.csproj" />
    <ProjectReference Include="..\src\Zulu\Zulu.csproj" />

    <PackageReference Include="Xml2Doc.MSBuild"
                      Version="2.3.1"
                      PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Build the owner normally:

```powershell
dotnet build .\docs\ApiDocs.csproj -c Release
```

`Xml2Doc_Aggregate` runs after the owner build. XML documentation next to resolved project-reference assemblies is collected automatically, canonicalized, de-duplicated, and passed to Core's multi-input aggregation path in one logical operation.

Every referenced project that participates must emit XML documentation:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

If an expected primary XML file is missing, aggregation fails instead of silently producing an incomplete repository index.

## Explicit primary XML inputs

Inputs that are not project references can be added explicitly:

```xml
<ItemGroup>
  <Xml2Doc_AggregateXml Include="$(RepoRoot)\artifacts\External.Contracts.xml" />
</ItemGroup>
```

Automatic project-reference XML and explicit `Xml2Doc_AggregateXml` items are combined as primary inputs before Core loads the aggregate model. Primary inputs produce Markdown pages.

Reference-only XML remains separate:

```xml
<ItemGroup>
  <Xml2Doc_ReferenceXml Include="$(RepoRoot)\artifacts\Framework.Contracts.xml" />
</ItemGroup>
```

`Xml2Doc_ReferenceXml` participates in inheritance/reference resolution but does not generate additional pages. Its identity and file changes participate in aggregate incremental tracking.

## Index ownership

The aggregation owner should be the only invocation that writes the aggregate `index.md`.

The cleanest participant setup is to emit compiler XML while disabling normal Markdown generation:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <Xml2Doc_Enabled>false</Xml2Doc_Enabled>
</PropertyGroup>
```

If a participating project still writes its own type pages into the aggregate directory, disable its index generation:

```xml
<PropertyGroup>
  <Xml2Doc_OutputDir>$(RepoRoot)\docs\api</Xml2Doc_OutputDir>
  <Xml2Doc_GenerateIndex>false</Xml2Doc_GenerateIndex>
</PropertyGroup>
```

When the owner uses project references, `Xml2Doc_AggregateValidateIndexOwnership` checks referenced Xml2Doc projects before their normal project-reference build. If a referenced project is enabled, targets the same normalized output directory, and still owns `index.md`, the build fails with `XML2DOC007` and identifies the conflicting project/output path.

Equivalent output paths with or without trailing directory separators are normalized before comparison.

Set `Xml2Doc_AggregateValidateIndexOwnership=false` only when repository orchestration already guarantees exclusive index ownership.

## Duplicate member ownership

If two primary XML inputs define the same XML documentation member ID, Core aggregation fails with `XML2DOC006`.

This is deliberate. Aggregation does not select a winning project based on input order because that would make output ownership nondeterministic.

## Determinism

Core canonicalizes aggregate primary XML paths before parsing them and emits the combined model in stable ordinal order. The MSBuild aggregate report records the canonical primary input list as `xmlInputs`.

Repository integration coverage builds the same owner once with normal parallel MSBuild scheduling and once with `/m:1`, on Windows and Linux. The workflow requires identical generated file sets, identical bytes, and stable index ordering.

## Aggregate lifecycle files

The aggregation owner uses separate lifecycle files from ordinary per-project generation:

| Property | Default |
| --- | --- |
| `Xml2Doc_AggregateEnabled` | `false` |
| `Xml2Doc_AggregateValidateIndexOwnership` | `true` |
| `Xml2Doc_AggregateReportPath` | `$(Xml2Doc_OutputDir)\xml2doc-aggregate-report.json` |
| `Xml2Doc_AggregateOutputStamp` | `$(IntermediateOutputPath)xml2doc.aggregate.stamp` |
| `Xml2Doc_AggregateFingerprintFile` | `$(IntermediateOutputPath)xml2doc.aggregate.fingerprint.txt` |
| `Xml2Doc_AggregateOutputLedger` | `$(IntermediateOutputPath)xml2doc.aggregate.outputs.txt` |

The owner reuses normal renderer properties such as `Xml2Doc_SingleFile`, `Xml2Doc_OutputDir`, `Xml2Doc_OutputFile`, `Xml2Doc_FileNameMode`, `Xml2Doc_RootNamespaceToTrim`, `Xml2Doc_GenerateIndex`, `Xml2Doc_PruneStaleFiles`, `Xml2Doc_ManifestIdentity`, `Xml2Doc_ParallelDegree`, `Xml2Doc_LineEndings`, and `Xml2Doc_MetadataFile`.

The aggregate fingerprint includes canonical primary input identities, explicit reference XML identities, significant rendering options, caller metadata content, and the host newline token when `Xml2Doc_LineEndings=native`. Primary/reference XML and caller metadata files are also target inputs, so content changes trigger regeneration.

A missing file recorded by the aggregate output ledger invalidates the aggregate stamp so the next build recreates the output.

`dotnet clean --configuration <Configuration>` removes the owner's `xml2doc.aggregate.*` lifecycle files for that configuration. It preserves aggregate Markdown, reports, manifests, and lifecycle state belonging to other configurations.

## Compatibility without an aggregation owner

Independent projects may still share one output directory if they have distinct ownership identities and all disable project-owned index generation:

```xml
<PropertyGroup>
  <Xml2Doc_OutputDir>$(RepoRoot)\docs\api</Xml2Doc_OutputDir>
  <Xml2Doc_GenerateIndex>false</Xml2Doc_GenerateIndex>
  <Xml2Doc_PruneStaleFiles>true</Xml2Doc_PruneStaleFiles>
  <Xml2Doc_ManifestIdentity>$(MSBuildProjectName)</Xml2Doc_ManifestIdentity>
</PropertyGroup>
```

This remains a supported compatibility mitigation, but it does not produce a unified repository index. Use an aggregation owner when one complete index is required.
