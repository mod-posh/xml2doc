# Xml2Doc.MSBuild

MSBuild integration for Xml2Doc, part of the **mod-posh** organization.

## Overview

`Xml2Doc.MSBuild` adds automatic documentation generation to your build.
After a successful compile, it converts the compiler-generated XML docs into Markdown using `Xml2Doc.Core`.

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
  <PackageReference Include="Xml2Doc.MSBuild" Version="1.1.0" PrivateAssets="all" />
</ItemGroup>
````

That’s it—on successful build, docs are generated according to the properties below.

---

## Configuration (MSBuild properties)

| Property                      | Description                                                                |
| ----------------------------- | -------------------------------------------------------------------------- |
| `Xml2Doc_Enabled`             | Enable/disable generation. Default: `true`.                                |
| `Xml2Doc_SingleFile`          | `true` = generate one combined Markdown file; `false` = per-type files.    |
| `Xml2Doc_OutputFile`          | Output file path when `SingleFile=true` (e.g. `$(ProjectDir)docs\api.md`). |
| `Xml2Doc_OutputDir`           | Output directory when `SingleFile=false` (e.g. `$(ProjectDir)docs`).       |
| `Xml2Doc_FileNameMode`        | `verbatim` (keep generic arity) or `clean` (friendly generic names).       |
| `Xml2Doc_RootNamespaceToTrim` | Optional namespace prefix trimmed from display names.                      |
| `Xml2Doc_CodeBlockLanguage`   | Code block language for fenced blocks (default `csharp`).                  |

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

**Per-type files (good for large APIs)**

```xml
<PropertyGroup>
  <Xml2Doc_SingleFile>false</Xml2Doc_SingleFile>
  <Xml2Doc_OutputDir>$(ProjectDir)docs</Xml2Doc_OutputDir>
  <Xml2Doc_FileNameMode>clean</Xml2Doc_FileNameMode>
</PropertyGroup>
```

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
- If you don’t see output, check **Build Output** for the Xml2Doc task messages and verify your project emits XML docs (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`).

---

## CI notes

- Works with `dotnet build` (task will load the **`net8.0`** target).
- Recommended pattern:

  ```bash
  dotnet build MySolution.sln -c Release
  ```

- Outputs are reproducible across hosts (VS vs. dotnet).

---

## Troubleshooting

- **No files produced**: ensure your project actually generates an XML doc file for the build config/TFM in use.
- **Want to disable temporarily?** Set `<Xml2Doc_Enabled>false</Xml2Doc_Enabled>` in your `.csproj` or via `/p:Xml2Doc_Enabled=false`.

---

## Versioning / Support

- Task hosts: **`net472`** (VS/MSBuild.exe), **`net8.0`** (`dotnet` SDK).
- Backed by `Xml2Doc.Core` targets: `netstandard2.0;net8.0;net9.0` with identical rendering across TFMs.
