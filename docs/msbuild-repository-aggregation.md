# MSBuild repository aggregation

Use repository aggregation when more than one project contributes XML documentation to one Markdown output set. The key rule is simple: **one MSBuild project owns the aggregate output**. Participating projects produce XML documentation; the owner calls Xml2Doc once with all of those XML inputs.

This avoids the unsafe pattern where independent projects all write the same `index.md` and whichever project finishes last wins.

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
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\src\Alpha\Alpha.csproj" />
    <ProjectReference Include="..\src\Zulu\Zulu.csproj" />

    <PackageReference Include="Xml2Doc.MSBuild"
                      Version="VERSION"
                      PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Build the owner normally:

```powershell
dotnet build .\docs\ApiDocs.csproj -c Release
```

`Xml2Doc_Aggregate` runs after the owner build. XML documentation next to resolved project-reference assemblies is collected automatically, normalized, de-duplicated, and passed to Core's multi-input aggregation path in one logical operation.

Every referenced project that participates must emit XML documentation:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

If an expected XML file is missing, aggregation fails instead of silently producing an incomplete repository index.

## Explicit XML inputs

Inputs that are not project references can be added explicitly:

```xml
<ItemGroup>
  <Xml2Doc_AggregateXml Include="$(RepoRoot)\artifacts\External.Contracts.xml" />
</ItemGroup>
```

Automatic project-reference XML and explicit `Xml2Doc_AggregateXml` items are combined before Core loads the aggregate model.

## Index ownership

The aggregation owner should be the only invocation that writes the aggregate `index.md`. For projects that still run their own Xml2Doc generation into the same directory, disable their index generation:

```xml
<PropertyGroup>
  <Xml2Doc_OutputDir>$(RepoRoot)\docs\api</Xml2Doc_OutputDir>
  <Xml2Doc_GenerateIndex>false</Xml2Doc_GenerateIndex>
</PropertyGroup>
```

A cleaner repository setup is to disable per-project Markdown generation entirely and let the owner generate the complete output once:

```xml
<PropertyGroup>
  <Xml2Doc_Enabled>false</Xml2Doc_Enabled>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

When the owner uses project references, `Xml2Doc_AggregateValidateIndexOwnership` checks referenced Xml2Doc projects before their normal project-reference build. If a referenced project is enabled, targets the same output directory, and still owns `index.md`, the build fails with `XML2DOC007` and tells the project to disable `Xml2Doc_GenerateIndex` or `Xml2Doc_Enabled`.

Set `Xml2Doc_AggregateValidateIndexOwnership=false` only when repository orchestration already guarantees exclusive index ownership.

## Determinism

Core canonicalizes aggregate XML paths before parsing them and emits the combined model in stable ordinal order. The MSBuild aggregate report records the canonical input list as `xmlInputs`.

Repository integration coverage builds the same two-project owner once with parallel MSBuild scheduling and once with `/m:1`, then requires identical generated file sets and identical bytes. It also checks that both projects appear in `index.md` in stable ordinal order.

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

The owner reuses the normal renderer properties such as `Xml2Doc_SingleFile`, `Xml2Doc_OutputDir`, `Xml2Doc_OutputFile`, `Xml2Doc_FileNameMode`, `Xml2Doc_GenerateIndex`, `Xml2Doc_LineEndings`, and pruning options.

The aggregate fingerprint includes the participating input identities plus rendering options. The XML files themselves are MSBuild target inputs, so changed XML, changed participation, changed rendering options, or a missing recorded output causes the owner target to regenerate.
