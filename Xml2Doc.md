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
  * `clean` — removes arity/backticks for prettier names; normalizes generic braces and uses safe characters for files.
* Namespace trimming for cleaner display names and headings via `RootNamespaceToTrim`.
* Snapshot-tested Markdown output for stability across versions.
* Fully compatible with **.NET 9.0** SDK and modern MSBuild hosts.
* New in this version:
  * 🔗 Predictable link behavior for per-type vs single-file outputs (see below).
  * 🪝 Stable, explicit HTML anchors for every member; in single-file, types also get heading-based anchors.
  * 🧠 Token‑aware aliasing (won’t corrupt identifiers like `StringComparer`).
  * 🧩 Depth‑aware generic formatting in labels and signatures (nested generics render correctly).
  * ✍️ Paragraph‑preserving normalization (keeps paragraph breaks and code fences; trims excess inline spaces).

---

## 🔗 Link behavior and anchors

Xml2Doc generates links and anchors tailored to the output mode.

* Per-type output (`RenderToDirectory`)
  * Types: links go to per-type files produced by the selected filename mode.
  * Members: links go to anchors within the per-type file.
  * Examples (with `file-names: clean`):
    * Type `T:MyApp.Foo\`1` → `[Foo<T1>](MyApp.Foo.md)`
    * Method `M:MyApp.Foo\`1.Bar(System.String)` → `[Bar(string)](MyApp.Foo.md#myapp.foo.bar(string))`

* Single-file output (`RenderToSingleFile` / `RenderToString`)
  * Types: links go to the in-document heading slug for the rendered type heading.
  * Members: links go to explicit in-document member anchors.
  * Examples:
    * Type heading “Foo<T1>” → `[Foo<T1>](#foot1)`
    * Method `M:MyApp.Foo\`1.Bar(System.String)` → `[Bar(string)](#myapp.foo.bar(string))`

Anchors

* Members
  * Every member section emits an explicit anchor:
    * `<a id="myapp.foo.bar(string)"></a>`
  * Computed by:
    * Applying C# aliases (e.g., `System.Int32` → `int`)
    * Normalizing XML-doc generic braces `{}` to `[]` for HTML safety
    * Lowercasing for stability
  * Example:
    `M:MyApp.Foo.Baz(System.Collections.Generic.Dictionary{System.String,System.Int32})`
    → `myapp.foo.baz(dictionary[string,int])`

* Types (single-file only)
  * Each type section also emits an anchor derived from the visible heading text:
    * Heading “Foo<T1>” → slug `foot1` (GitHub-like rules: lowercase, spaces → `-`, drop non `[a-z0-9-]`)
  * Type links in single-file mode use this slug.

---

## ⚙️ Renderer options

* File name mode (`--file-names`, MSBuild: `Xml2Doc_FileNameMode`)
  * `verbatim`
    * Keeps the original doc ID detail in file names (including arity like `` `1 ``).
    * Example: `T:MyApp.Foo\`1` → `MyApp.Foo\`1.md`
  * `clean`
    * Strips generic arity and normalizes generic braces.
    * Replaces `< >` with `[ ]` to avoid filesystem issues.
    * Example: `T:MyApp.Foo\`1` → `MyApp.Foo.md`

* Root namespace trimming (`--root-namespace`, MSBuild: `Xml2Doc_RootNamespaceToTrim`)
  * Trims a configured root namespace from display names (headings, index entries, labels).
  * Does not affect file names.
  * Example:
    * `RootNamespaceToTrim = "MyApp"`
    * `T:MyApp.Core.Widget\`2` → heading “Widget<T1,T2>”; index shows “Widget<T1,T2>”
    * File (clean mode): `MyApp.Core.Widget.md`

* Code block language (`--code-language`, MSBuild: `Xml2Doc_CodeBlockLanguage`)
  * Sets the language for fenced code blocks (default: `csharp`).

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

New coverage in this version:

* Token‑aware aliasing (avoids corrupting identifiers like `StringComparer`).
* Depth‑aware nested generics in headers and labels.
* Paragraph‑preserving normalization and code fence protection.

Run all tests:

```bash
dotnet test -c Release
```

---

## 💡 Part of mod-posh

This project is maintained under the **mod-posh** organization — a collection of developer tooling and automation projects built by and for engineers who want their workflows to be both powerful and minimal.

```
