# Xml2Doc.Cli

Command-line interface for Xml2Doc, part of the **mod-posh** organization.

## Overview

`Xml2Doc.exe` converts C# XML documentation into Markdown using the `Xml2Doc.Core` engine.
It’s perfect for developers or CI systems that want quick, repeatable Markdown docs from XML output without writing code.

## Usage

```bash
# Per-type documentation
Xml2Doc.exe --xml .\bin\Release\net9.0\MyLib.xml --out .\docs

# Single combined file
Xml2Doc.exe --xml .\bin\Release\net9.0\MyLib.xml --out .\docs\api.md --single --file-names clean
````

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

You can use a JSON file to define all options and simplify command-line usage:

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

Run it with:

```bash
Xml2Doc.exe --config xml2doc.json
```

CLI flags always override values from the config file.
