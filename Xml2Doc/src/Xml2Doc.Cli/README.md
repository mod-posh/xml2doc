# Xml2Doc.Cli

Command-line interface for Xml2Doc, part of the **mod-posh** organization.

## Overview

`Xml2Doc.Cli.exe` provides a simple, script-friendly way to convert your C# XML documentation into Markdown.
It’s built on top of `Xml2Doc.Core` and is ideal for developers who want to generate documentation locally or in CI pipelines without custom code.

## Usage

```bash
Xml2Doc.Cli.exe --xml ./bin/Debug/net8.0/MyLib.xml --out ./docs
````

### Options

| Option                  | Description                                  |                                    |
| ----------------------- | -------------------------------------------- | ---------------------------------- |
| `--xml <path>`          | Path to the XML documentation file           |                                    |
| `--out <path>`          | Output directory or file (depending on mode) |                                    |
| `--single`              | Combine all output into one Markdown file    |                                    |
| `--file-names <verbatim | clean>`                                      | Choose how filenames are formatted |
| `--rootns <namespace>`  | Optional root namespace to trim              |                                    |
| `--lang <language>`     | Code block language (default: `csharp`)      |                                    |
| `--help`                | Display help text                            |                                    |

### Example

```bash
Xml2Doc.Cli.exe --xml ./bin/Debug/net8.0/MyLib.xml --out ./docs --single --file-names clean
```

Produces either a single Markdown file or per-type documentation pages, depending on your flags.
