# Xml2Doc.Cli

Command-line interface for Xml2Doc, part of the **mod-posh** organization.

## Overview

`Xml2Doc.Cli` converts C# XML documentation into Markdown using the `Xml2Doc.Core` engine.
It’s ideal for developers and CI systems that want quick, repeatable Markdown docs from existing XML output.

## Target Frameworks (multi-TFM)

The CLI is **multi-targeted** and ships for:

- `net8.0`
- `net9.0`

> The rendered Markdown is identical across these TFMs. Choose whichever runtime you have available on the machine that runs the tool.

### Running the CLI (pick one TFM)

Build first, then run the produced DLL for the desired framework:

```bash
# Build both TFMs (default for the project)
dotnet build Xml2Doc/src/Xml2Doc.Cli/Xml2Doc.Cli.csproj -c Release

# Run the built artifact (net8.0)
dotnet Xml2Doc/src/Xml2Doc.Cli/bin/Release/net8.0/Xml2Doc.Cli.dll --xml path\to\MyLib.xml --out .\docs

# Or run the net9.0 artifact
dotnet Xml2Doc/src/Xml2Doc.Cli/bin/Release/net9.0/Xml2Doc.Cli.dll --xml path\to\MyLib.xml --out .\docs
````

> Tip: Prefer running the built DLL as shown above. Using `dotnet run` can be confusing with multi-targeted projects; if you do use it, you **must** specify a single `--framework` that exists in the csproj.

## Usage

```bash
# Per-type documentation
Xml2Doc.exe --xml .\bin\Release\net9.0\MyLib.xml --out .\docs

# Single combined file
Xml2Doc.exe --xml .\bin\Release\net9.0\MyLib.xml --out .\docs\api.md --single --file-names clean
```

### Options

| Option                           | Description                                                   |
| -------------------------------- | ------------------------------------------------------------- |
| `--xml <path>`                   | Path to the XML documentation file                            |
| `--out <path>`                   | Output directory (multi-file) or output file (single-file)    |
| `--single`                       | Combine all documentation into a single Markdown file         |
| `--file-names <verbatim\|clean>` | Filename mode — preserve generics or shorten to readable form |
| `--rootns <namespace>`           | Optional root namespace to trim                               |
| `--lang <language>`              | Code block language (default: `csharp`)                       |
| `--config <file>`                | Path to a JSON configuration file                             |
| `--help`                         | Display help text                                             |

### Example Config File

**`xml2doc.json`**

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

Run with:

```bash
Xml2Doc.exe --config xml2doc.json
```

CLI flags always override values from the config file.

## Notes for CI

- Build once (`dotnet build -c Release`), then run the desired **built** CLI DLL (net8.0 or net9.0).
- Output is deterministic across TFMs, so your pipelines can choose the runtime that’s already available.
