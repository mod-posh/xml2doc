| Latest Version | Nuget.org | Issues | Testing | License | Discord |
|-----------------|-----------------|----------------|----------------|----------------|----------------|
| [![Latest Version](https://img.shields.io/github/v/tag/mod-posh/xml2doc)](https://github.com/mod-posh/xml2doc/tags) | [![Nuget.org](https://img.shields.io/nuget/dt/Xml2Doc.Core?label=Xml2Doc.Core)](https://www.nuget.org/packages/Xml2Doc.Core)<br/>[![Nuget.org](https://img.shields.io/nuget/dt/Xml2Doc.Cli?label=Xml2Doc.Cli)](https://www.nuget.org/packages/Xml2Doc.Cli)<br/>[![Nuget.org](https://img.shields.io/nuget/dt/Xml2Doc.MSBuild?label=Xml2Doc.MSBuild)](https://www.nuget.org/packages/Xml2Doc.MSBuild) | [![GitHub issues](https://img.shields.io/github/issues/mod-posh/xml2doc)](https://github.com/mod-posh/xml2doc/issues) | [![Merge Test Workflow](https://github.com/mod-posh/xml2doc/actions/workflows/test.yml/badge.svg)](https://github.com/mod-posh/xml2doc/actions/workflows/test.yml) | [![GitHub license](https://img.shields.io/github/license/mod-posh/xml2doc)](https://github.com/mod-posh/xml2doc/blob/master/LICENSE) | [![Discord Server](https://assets-global.website-files.com/6257adef93867e50d84d30e2/636e0b5493894cf60b300587_full_logo_white_RGB.svg)](https://discord.com/channels/1044305359021555793/1044305781627035811) |
# Xml2Doc

Xml2Doc aims to make **developer documentation** for .NET projects easier to maintain, more readable, and always up-to-date — without relying on brittle third-party tools.

* ✅ Built on modern .NET (9.0+)
* ✅ 100% open source under GPL-3.0
* ✅ Works anywhere: local builds, CI/CD pipelines, or automated doc workflows

---

## 📖 Overview

**Xml2Doc** is a modern, extensible toolchain that transforms the XML documentation produced by the C# compiler into clean, readable Markdown.
It can be used as a **library**, a **command-line tool**, or integrated directly into your **build pipeline** via MSBuild.

Xml2Doc includes:

* **Xml2Doc.Core** — The engine that parses XML and renders Markdown.
* **Xml2Doc.Cli** — A command-line tool (`Xml2Doc.Cli.exe`) for quick, repeatable conversions.
* **Xml2Doc.MSBuild** — An MSBuild integration that automatically generates Markdown after each build.

---

## ✨ Features

* Converts XML doc comments (`<summary>`, `<remarks>`, `<example>`, `<seealso>`, `<exception>`, `<inheritdoc/>`, etc.) to clean Markdown.
* Automatically links `<see cref="..."/>` and `<paramref name="..."/>` references.
* **Overload grouping:** related methods render under a single heading with their individual signatures.
* Smarter display names — shortens namespaces and formats generics like `List<T>`.
* Built-in aliases for .NET primitive types (`System.String` → `string`, etc.).
* Supports both **per-type** and **single-file** Markdown output.
* Configurable code block language (default: `csharp`).
* Filename modes:
  * `verbatim` — preserves full .NET type names (with backticks for generics).
  * `clean` — removes arity/backticks for prettier names.
* Namespace trimming for cleaner doc paths.
* JSON-based config file support for CLI users (`--config xml2doc.json`).
* Snapshot-tested Markdown output for stability across versions.
* Fully compatible with **.NET 9.0** SDK and modern MSBuild hosts.

---

## 🧱 Project Structure

```

Xml2Doc/
├─ src/
│   ├─ Xml2Doc.Core/         # Core parser and Markdown renderer
│   ├─ Xml2Doc.Cli/          # CLI entrypoint (Xml2Doc.Cli.exe / dotnet tool)
│   └─ Xml2Doc.MSBuild/      # MSBuild task & build integration
├─ tests/
│   ├─ Xml2Doc.Tests/        # Snapshot tests for Markdown output
│   └─ Xml2Doc.Sample/       # Example library for validation
├─ Directory.Build.props
├─ Xml2Doc.sln
├─ README.md
└─ CHANGELOG.md

````

---

## 🚀 Quick Start

### 1️⃣ Generate XML documentation in your project

Add this to your `.csproj`:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
````

Then build:

```bash
dotnet build
```

---

### 2️⃣ Use the CLI

Run directly via the compiled executable:

```bash
Xml2Doc.Cli.exe --xml ./bin/Debug/net9.0/MyLib.xml --out ./docs
```

Or, if installed as a .NET tool:

```bash
dotnet tool install -g Xml2Doc.Cli
xml2doc --xml ./bin/Debug/net9.0/MyLib.xml --out ./docs/api.md --single --file-names clean
```

Output example:

```
docs/
 ├─ index.md
 └─ MyNamespace.MyType.md
```

**Config file usage**

```bash
Xml2Doc.Cli.exe --config xml2doc.json
```

Where `xml2doc.json` might look like:

```json
{
  "Xml": "src/MyLib/bin/Release/net9.0/MyLib.xml",
  "Out": "docs/api.md",
  "Single": true,
  "FileNames": "clean",
  "RootNamespace": "MyCompany.MyProduct",
  "CodeLanguage": "csharp"
}
```

---

### 3️⃣ Integrate with MSBuild

Add this to your project’s `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Xml2Doc.MSBuild" Version="1.1.0" PrivateAssets="all" />
</ItemGroup>
```

#### Optional MSBuild properties

| Property                      | Description                                           |
| ----------------------------- | ----------------------------------------------------- |
| `Xml2Doc_Enabled`             | Enable or disable Markdown generation (default: true) |
| `Xml2Doc_SingleFile`          | Combine all output into a single Markdown file        |
| `Xml2Doc_OutputFile`          | Path for the merged Markdown file                     |
| `Xml2Doc_OutputDir`           | Directory for per-type docs                           |
| `Xml2Doc_FileNameMode`        | `verbatim` or `clean`                                 |
| `Xml2Doc_RootNamespaceToTrim` | Namespace prefix to trim for cleaner names            |
| `Xml2Doc_CodeBlockLanguage`   | Code block language (`csharp` by default)             |

Example configuration:

```xml
<PropertyGroup>
  <Xml2Doc_SingleFile>true</Xml2Doc_SingleFile>
  <Xml2Doc_OutputFile>$(ProjectDir)\docs\api.md</Xml2Doc_OutputFile>
  <Xml2Doc_FileNameMode>clean</Xml2Doc_FileNameMode>
  <Xml2Doc_RootNamespaceToTrim>MyCompany.MyProduct</Xml2Doc_RootNamespaceToTrim>
</PropertyGroup>
```

---

## 🧪 Testing

The project uses **xUnit** + **Shouldly** with snapshot-based tests.
Each test validates Markdown output for both per-type and single-file modes to ensure that future refactors preserve formatting and structure.

Run all tests:

```bash
dotnet test -c Release
```

---

## 💡 Part of mod-posh

This project is maintained under the **mod-posh** organization — a collection of developer tooling and automation projects built by and for engineers who want their workflows to be both powerful and minimal.

```
