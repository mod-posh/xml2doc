# Xml2Doc.Cli

Command-line interface for Xml2Doc, part of the **mod-posh** organization.

## Overview

`Xml2Doc.Cli` converts C# XML documentation into deterministic Markdown using `Xml2Doc.Core`.
Version `2.4.0` adds deterministic caller metadata and built-in multi-document layout selection while preserving single-input and aggregate defaults.

The CLI is multi-targeted for:

- `net8.0`
- `net9.0`

Rendered Markdown is expected to be identical across supported CLI TFMs for the same options and inputs.

## Install as a .NET tool

```powershell
dotnet tool install --global Xml2Doc.Cli --version 2.4.0
```

The installed command is `xml2doc`.

## Basic usage

Generate per-type documentation from one XML file:

```powershell
xml2doc `
  --xml .\bin\Release\net9.0\MyLibrary.xml `
  --out .\docs
```

Generate one combined Markdown file:

```powershell
xml2doc `
  --xml .\bin\Release\net9.0\MyLibrary.xml `
  --out .\docs\api.md `
  --single `
  --file-names clean
```

Aggregate multiple projects into one deterministic output set by repeating `--xml`:

```powershell
xml2doc `
  --xml .\src\ProjectA\bin\Release\net9.0\ProjectA.xml `
  --xml .\src\ProjectB\bin\Release\net9.0\ProjectB.xml `
  --out .\docs `
  --file-names clean `
  --line-endings lf
```

One XML input uses the compatible single-input loading path. Two or more primary inputs use Core aggregation. Input paths are canonicalized before loading so input order does not control output order.

If two primary inputs define the same XML documentation member, generation fails with `XML2DOC006` instead of selecting an owner based on argument order.

## Options

| Option | Description |
| --- | --- |
| `--xml <path>` | Primary XML documentation input. Repeat to aggregate multiple files. |
| `--out <path>` | Output directory, or output file when used with `--single`. |
| `--single` | Generate one consolidated Markdown file. |
| `--file-names <verbatim\|clean>` | Filename mode. |
| `--rootns <namespace>` | Trim a namespace prefix from displayed type names. |
| `--trim-rootns-filenames` | Also trim the root namespace from filenames. |
| `--lang <language>` | Fenced-code language. Default: `csharp`. |
| `--anchor-algorithm <mode>` | `default`, `github`, `gfm`, or `kramdown`. |
| `--template <path>` | Apply a file-based template. |
| `--front-matter <path>` | Prepend configured front matter. |
| `--metadata <key=value>` | Add generic caller metadata. Repeat for multiple values. |
| `--auto-link` | Enable safe free-text symbol linking. |
| `--alias-map <path>` | Load an additional alias map. |
| `--external-docs <base-url>` | Route unresolved references to an external documentation base URL. |
| `--toc` | Emit member tables of contents. Directory output only. |
| `--namespace-index` | Emit namespace index/pages. Directory output only. |
| `--no-index` | Suppress the per-type `index.md`. |
| `--basename-only` | Use basename-only output names and links. |
| `--parallel <N>` | Maximum per-type render parallelism. Must be positive. |
| `--prune-stale` | Remove stale files owned by the selected manifest identity. Directory output only. |
| `--manifest-id <identity>` | Stable ownership identity required with `--prune-stale`. |
| `--line-endings <style>` | `lf` (default), `crlf`, or `native`. |
| `--layout <mode>` | `flat` (default) or `namespace-folders`. |
| `--report <path>` | Write a JSON execution report. |
| `--dry-run` | Plan output without writing Markdown. |
| `--diff` | Compare generated output with current files without modifying them. |
| `--config <path>` | Load JSON configuration. CLI arguments take precedence. |
| `--help`, `-h` | Display help. |

`--dry-run` and `--diff` are mutually exclusive. `--toc`, `--namespace-index`, and `--prune-stale` require directory output.

Exit codes:

- `0` — success, or no differences for `--diff`.
- `1` — invalid command-line/configuration input.
- `2` — diagnostic or runtime error.
- `3` — differences found by `--diff`.

## JSON configuration

Single-input example:

```json
{
  "Xml": "src/MyLib/bin/Release/net9.0/MyLib.xml",
  "Out": "docs/api.md",
  "Single": true,
  "FileNames": "clean",
  "RootNamespace": "MyCompany.MyProduct",
  "CodeLanguage": "csharp",
  "Metadata": {
    "package": "MyCompany.MyProduct",
    "tags": ["api", "stable"],
    "version": "2.4.0"
  },
  "LineEndings": "lf"
}
```

Multi-input aggregation example:

```json
{
  "XmlInputs": [
    "src/ProjectA/bin/Release/net9.0/ProjectA.xml",
    "src/ProjectB/bin/Release/net9.0/ProjectB.xml"
  ],
  "Out": "docs",
  "FileNames": "clean",
  "GenerateIndex": true,
  "Parallel": 4,
  "LineEndings": "lf"
}
```

When no CLI `--xml` arguments are supplied, non-empty `XmlInputs` takes precedence over the legacy single-input `Xml` property. Repeated CLI `--xml` arguments take precedence over both configuration properties.

Run a configuration file with:

```powershell
xml2doc --config .\xml2doc.json
```

Configuration supports the same applicable values as the CLI surface, including `TrimRootNamespaceInFileNames`, `Report`, `DryRun`, `Diff`, `AnchorAlgorithm`, `Template`, `FrontMatter`, `Metadata`, `AutoLink`, `AliasMap`, `ExternalDocs`, `Toc`, `NamespaceIndex`, `GenerateIndex`, `Parallel`, `BasenameOnly`, `PruneStaleFiles`, `ManifestIdentity`, `LineEndings`, and `Layout`.

Repeated `--metadata key=value` arguments override matching keys from the JSON `Metadata` object.
Caller metadata produces deterministic YAML front matter containing Core-derived `documentId`,
`documentKind`, `namespace`, `symbol`, and `outputPath` values. Those document keys are
authoritative. Metadata cannot be combined with the literal `--front-matter` file mode.

Unknown JSON properties and invalid values are rejected rather than ignored silently.

## Reports, dry run, and diff

When `--report` is configured, reports include deterministic planned and actual result sets plus runner timing information. Aggregate reports include canonical `xmlInputs` while retaining the compatible `xml` field for the first canonical input.

Dry runs do not modify Markdown or ownership state. Reports leave actual-result arrays empty and populate `wouldWrite` and `wouldDelete` as applicable.

`--diff` performs a non-mutating comparison against current generated output. The report/console result classifies added, changed, unchanged, and removed files. Removed files are limited to stale outputs owned by the selected manifest identity when pruning is enabled.

## Stale-output ownership

For safe pruning in directory mode, use a stable identity:

```powershell
xml2doc `
  --xml .\bin\Release\net9.0\MyLibrary.xml `
  --out .\docs `
  --prune-stale `
  --manifest-id MyCompany.MyLibrary
```

Only paths recorded by the manifest for that exact identity can be removed. Untracked files and files owned by other identities are preserved. Pruning is unavailable with `--single`.

## Diagnostics

CLI diagnostics are written to standard error in the stable form:

```text
xml2doc <severity> <code>: <message>
```

Source locations and member IDs are included when available. Warnings do not fail generation.

Aggregation-specific diagnostics include:

- `XML2DOC006` — multiple primary XML inputs define the same documentation member.

The remaining stable diagnostic IDs are documented in the repository-level [Xml2Doc.md](../../../Xml2Doc.md).

## Running a locally built CLI

Build the project, then run one produced TFM explicitly:

```powershell
dotnet build .\Xml2Doc\src\Xml2Doc.Cli\Xml2Doc.Cli.csproj -c Release

dotnet .\Xml2Doc\src\Xml2Doc.Cli\bin\Release\net9.0\Xml2Doc.Cli.dll `
  --xml .\path\to\MyLibrary.xml `
  --out .\docs
```

Using the built DLL avoids ambiguity when working directly with a multi-targeted executable project.

## Determinism notes

- Markdown uses LF on every platform by default.
- Aggregate input paths are canonicalized before loading.
- Per-type rendering may use `--parallel`, but output ordering remains deterministic.
- CLI reports omit timestamps by default so equivalent invocations remain comparable.
