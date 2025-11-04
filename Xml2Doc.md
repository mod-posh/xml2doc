# Xml2Doc

Xml2Doc turns the XML documentation your C# compiler already emits into clean, linkable Markdown.
Use it as a **library**, a **CLI**, or via an **MSBuild task** right inside your build.

- ✅ Modern .NET support with deterministic output across TFMs
- ✅ Works locally, in CI, or inside Visual Studio
- ✅ Snapshot-tested, link- and anchor-stable Markdown

---

## 📖 Overview

Xml2Doc includes:

- **Xml2Doc.Core** — Engine that parses XML and renders Markdown.
- **Xml2Doc.Cli** — Command-line tool for repeatable conversions.
- **Xml2Doc.MSBuild** — Build integration that generates docs after a successful compile.

Key capabilities:

- Converts `<summary>`, `<remarks>`, `<example>`, `<seealso>`, `<exception>`, `<inheritdoc/>`.
- Auto-links `<see cref="…"/>` and `<paramref name="…"/>`.
- Overload grouping under a single heading.
- Short, readable generics (`List<T>`), token-aware primitive aliases (`System.String → string` without breaking identifiers).
- **Per-type** and **single-file** output modes.
- Stable, explicit anchors for members; GitHub-style heading slugs for types (single-file).
- Paragraph/code-fence preserving normalization.
- Filename modes: `verbatim` or `clean`.
- Configurable code block language (default `csharp`) and display-only root namespace trimming.

---

## 🧭 Multi-framework support

| Component           | Target Frameworks                               | Notes |
|--------------------|--------------------------------------------------|-------|
| **Xml2Doc.Core**   | `netstandard2.0; net8.0; net9.0`                 | Output is **identical** across TFMs (tested). |
| **Xml2Doc.Cli**    | `net8.0; net9.0`                                 | Build both; run the artifact for the TFM available on your machine. |
| **Xml2Doc.MSBuild**| `net472; net8.0`                                 | VS/MSBuild.exe hosts the **net472** task; `dotnet build` hosts the **net8.0** task. |

### Source/compat notes

- We keep C# 10 syntax in source (file-scoped namespaces/global usings) while avoiding runtime-only APIs missing on `netstandard2.0` (e.g., `Index`/`Range`, certain `Split` overloads).
- The MSBuild task maps to the correct Core TFM automatically (e.g., task `net472` → Core `netstandard2.0`).

---

## 🔗 Link behavior & anchors (summary)

- **Per-type (`RenderToDirectory`)**
  - Types link to per-type files; members link to anchors inside those files.
- **Single-file (`RenderToSingleFile` / `RenderToString`)**
  - Types use heading slugs; members use explicit in-document anchors.

Anchors are stable and case-normalized, with safe generic brace handling (e.g., `Dictionary{String,Int32}` → `dictionary[string,int]`).

---

## 🧱 Project layout

```

Xml2Doc/
├─ src/
│  ├─ Xml2Doc.Core/
│  ├─ Xml2Doc.Cli/
│  └─ Xml2Doc.MSBuild/
├─ tests/
│  ├─ Xml2Doc.Tests/
│  └─ Xml2Doc.Sample/
├─ Directory.Build.props
├─ Xml2Doc.sln
├─ README.md
└─ CHANGELOG.md

````

---

## 🚀 Quick start

### 1) Enable XML documentation in your project

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
````

Build your project to produce `MyLib.xml`.

---

### 2) Use the CLI

**Build both CLI TFMs** (default for the project), then **run the built artifact** you prefer:

```bash
# Build (Release)
dotnet build Xml2Doc/src/Xml2Doc.Cli/Xml2Doc.Cli.csproj -c Release

# Run the net8.0 artifact
dotnet Xml2Doc/src/Xml2Doc.Cli/bin/Release/net8.0/Xml2Doc.Cli.dll \
  --xml path/to/MyLib.xml --out ./docs

# Or the net9.0 artifact
dotnet Xml2Doc/src/Xml2Doc.Cli/bin/Release/net9.0/Xml2Doc.Cli.dll \
  --xml path/to/MyLib.xml --out ./docs
```

> Tip: Avoid `dotnet run` with multi-targeted projects. If you do use it, you **must** specify `--framework net8.0` (or `net9.0`) and ensure no custom target invokes MSBuild with a malformed `Properties=` value.

**Single-file example:**

```bash
dotnet Xml2Doc/src/Xml2Doc.Cli/bin/Release/net8.0/Xml2Doc.Cli.dll \
  --xml ./bin/Release/net9.0/MyLib.xml \
  --out ./docs/api.md --single --file-names clean
```

---

### 3) Use the MSBuild task (auto-generate on build)

Add to your library’s `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Xml2Doc.MSBuild" Version="1.1.0" PrivateAssets="all" />
</ItemGroup>
```

**Properties:**

| Property                      | Description                             |
| ----------------------------- | --------------------------------------- |
| `Xml2Doc_Enabled`             | Enable/disable (default `true`)         |
| `Xml2Doc_SingleFile`          | `true` → one Markdown file              |
| `Xml2Doc_OutputFile`          | Output file path (single-file mode)     |
| `Xml2Doc_OutputDir`           | Output directory (per-type mode)        |
| `Xml2Doc_FileNameMode`        | `verbatim` or `clean`                   |
| `Xml2Doc_RootNamespaceToTrim` | Trim prefix from display names          |
| `Xml2Doc_CodeBlockLanguage`   | Fenced code language (default `csharp`) |

**Examples:**

Single file:

```xml
<PropertyGroup>
  <Xml2Doc_SingleFile>true</Xml2Doc_SingleFile>
  <Xml2Doc_OutputFile>$(ProjectDir)docs\api.md</Xml2Doc_OutputFile>
  <Xml2Doc_FileNameMode>clean</Xml2Doc_FileNameMode>
  <Xml2Doc_RootNamespaceToTrim>MyCompany.MyProduct</Xml2Doc_RootNamespaceToTrim>
</PropertyGroup>
```

Per-type:

```xml
<PropertyGroup>
  <Xml2Doc_SingleFile>false</Xml2Doc_SingleFile>
  <Xml2Doc_OutputDir>$(ProjectDir)docs</Xml2Doc_OutputDir>
  <Xml2Doc_FileNameMode>clean</Xml2Doc_FileNameMode>
</PropertyGroup>
```

**Visual Studio note:** The package includes a **`net472`** task so VS 2022 (MSBuild.exe host) can execute it.
**`dotnet build`** uses the **`net8.0`** task host. Output Markdown is identical.

---

## 🧪 Tests & snapshots

- Snapshot tests cover both output modes and representative APIs.
- **Cross-TFM consistency test:** builds CLI for `net8.0` & `net9.0`, renders with each, and asserts identical output (normalized EOLs).
- (Optional, Windows-only) Task build check for `net472` guards the P2P TFM mapping.

**CI suggestion:**

```yaml
- run: dotnet build Xml2Doc.sln -c Release -m:1 -nr:false
- run: dotnet test tests/Xml2Doc.Tests/Xml2Doc.Tests.csproj -c Release --no-build
# On Windows, optionally:
- if: runner.os == 'Windows'
  run: dotnet build Xml2Doc/src/Xml2Doc.MSBuild/Xml2Doc.MSBuild.csproj -c Release -m:1 -v minimal
```

---

## 🧰 Troubleshooting

- **`MSB3100: Properties syntax invalid (netX.Y)`**
  A custom target is invoking the MSBuild task with `Properties="net8.0"` (missing `name=value`). Fix to:
  `Properties="TargetFramework=$(TargetFramework);Configuration=$(Configuration)"`.

- **File locks during multi-target builds**
  Build with `-m:1 -nr:false` to reduce handle contention:
  `dotnet build Xml2Doc.sln -c Release -m:1 -nr:false`.

---

## 📦 Maintenance & support

We track current .NET (e.g., `net8.0`, `net9.0`) and maintain `netstandard2.0` for broad library reach.
If you spot cross-TFM drift in output, please open an issue with a minimal XML sample plus expected vs actual Markdown.
