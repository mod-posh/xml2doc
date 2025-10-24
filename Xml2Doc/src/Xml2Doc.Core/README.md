# Xml2Doc.Core

Core library for the Xml2Doc toolset, part of the **mod-posh** organization.

## Overview

`Xml2Doc.Core` provides the foundational logic for parsing C# XML documentation and rendering it into clean, readable Markdown.
This is the same engine used by both the CLI (`Xml2Doc.Cli.exe`) and the MSBuild integration.

## Features

- Parses `<summary>`, `<remarks>`, `<example>`, `<seealso>`, `<exception>`
- Converts `<see>` and `<paramref>` into Markdown links and inline code
- Cleans up type and method names, including generics like `List<T>`
- Aliases built-in types (`System.String` → `string`, etc.)
- Supports both per-type and single-file Markdown output
- Fully configurable through `RendererOptions`:
  - Filename mode (`Verbatim` or `CleanGenerics`)
  - Root namespace trimming
  - Code block language (`csharp` by default)

## Example Usage

```csharp
using Xml2Doc.Core;

var model = XmlDocModel.Load("MyLibrary.xml");
var options = new RendererOptions(FileNameMode: FileNameMode.CleanGenerics);
var renderer = new MarkdownRenderer(model, options);
renderer.RenderToDirectory("./docs");
````
