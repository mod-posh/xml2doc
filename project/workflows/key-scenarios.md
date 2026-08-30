# Key Scenarios

## Generate documentation during a project build

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <Xml2Doc_OutputDir>$(MSBuildProjectDirectory)\docs</Xml2Doc_OutputDir>
    <Xml2Doc_LineEndings>lf</Xml2Doc_LineEndings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Xml2Doc.MSBuild" Version="2.3.1" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Build with:

```powershell
dotnet build .\src\MyLibrary\MyLibrary.csproj -c Release
```

## Generate documentation from the CLI

```powershell
xml2doc `
  --xml .\bin\Release\net9.0\MyLibrary.xml `
  --out .\docs `
  --file-names clean `
  --line-endings lf
```

## Aggregate multiple XML files from the CLI

```powershell
xml2doc `
  --xml .\src\Alpha\bin\Release\net9.0\Alpha.xml `
  --xml .\src\Zulu\bin\Release\net9.0\Zulu.xml `
  --out .\docs `
  --file-names clean
```

The CLI routes two or more primary inputs through Core aggregation and produces one deterministic index.

## Aggregate a repository with MSBuild

Create one owner project:

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
    <ProjectReference Include="..\src\Alpha\Alpha.csproj" />
    <ProjectReference Include="..\src\Zulu\Zulu.csproj" />
    <PackageReference Include="Xml2Doc.MSBuild" Version="2.3.1" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Participating projects must emit XML documentation. If they also render into the same aggregate directory, disable their normal Xml2Doc generation or set `Xml2Doc_GenerateIndex=false` so the owner is the only index writer.

## Preview or compare output without mutation

```powershell
xml2doc --xml .\MyLibrary.xml --out .\docs --dry-run
xml2doc --xml .\MyLibrary.xml --out .\docs --diff
```

Diff returns exit code `3` when generated output differs from current files.

## Prune stale generated files safely

```powershell
xml2doc `
  --xml .\MyLibrary.xml `
  --out .\docs `
  --prune-stale `
  --manifest-id MyCompany.MyLibrary
```

Only files owned by the matching manifest identity are eligible for deletion.
