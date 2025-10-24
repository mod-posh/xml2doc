# Xml2Doc.MSBuild

MSBuild integration for Xml2Doc, part of the **mod-posh** organization.

## Overview

`Xml2Doc.MSBuild` adds automatic documentation generation to your build process.
When enabled, it uses `Xml2Doc.Core` to convert the compiler-generated XML documentation into Markdown after each successful build.

## Setup

Add this to your project file:

```xml
<ItemGroup>
  <PackageReference Include="Xml2Doc.MSBuild" Version="1.1.0" PrivateAssets="all" />
</ItemGroup>
````

### Optional MSBuild Properties

| Property                      | Description                                              |
| ----------------------------- | -------------------------------------------------------- |
| `Xml2Doc_Enabled`             | Enables or disables Markdown generation (default: true)  |
| `Xml2Doc_SingleFile`          | Generates one combined Markdown file                     |
| `Xml2Doc_OutputFile`          | Target file when single-file mode is used                |
| `Xml2Doc_OutputDir`           | Target directory when generating per-type docs           |
| `Xml2Doc_FileNameMode`        | `verbatim` or `clean` (controls generic name formatting) |
| `Xml2Doc_RootNamespaceToTrim` | Optional namespace prefix to trim                        |
| `Xml2Doc_CodeBlockLanguage`   | Code block language (`csharp` by default)                |

### Example Configuration

```xml
<PropertyGroup>
  <Xml2Doc_SingleFile>true</Xml2Doc_SingleFile>
  <Xml2Doc_OutputFile>$(ProjectDir)\docs\api.md</Xml2Doc_OutputFile>
  <Xml2Doc_FileNameMode>clean</Xml2Doc_FileNameMode>
  <Xml2Doc_RootNamespaceToTrim>MyCompany.MyProduct</Xml2Doc_RootNamespaceToTrim>
</PropertyGroup>
```
