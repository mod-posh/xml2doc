# Xml2Doc.Core

Core library for the Xml2Doc toolset, part of the **mod-posh** organization.

## Overview

`Xml2Doc.Core` is the engine that powers all Xml2Doc tools — including the CLI and MSBuild integration.
It handles XML parsing, type resolution, link conversion, and Markdown rendering in a way that’s simple to extend or reuse.

## Features

- Parses `<summary>`, `<remarks>`, `<example>`, `<seealso>`, `<exception>`, and `<inheritdoc/>`
- Converts `<see>` and `<paramref>` into inline Markdown links and code spans
- Cleans up namespaces and shortens generic types (e.g. `List<T>` instead of `System.Collections.Generic.List<T>`)
- Aliases built-in types (`System.String` → `string`, `System.Int32` → `int`)
- Supports overload grouping for cleaner docs
- Supports both **per-type** and **single-file** Markdown output
- Provides rich configuration through `RendererOptions`:
  - Filename mode (`Verbatim` or `CleanGenerics`)
  - Root namespace trimming
  - Code block language (default: `csharp`)
  - Output mode selection (single vs. multi-file)
- Includes snapshot-tested output for consistent Markdown generation

## Example Usage

```csharp
using Xml2Doc.Core;

var model = XmlDocModel.Load("MyLibrary.xml");
var options = new RendererOptions(
    FileNameMode: FileNameMode.CleanGenerics,
    RootNamespaceToTrim: "MyCompany.MyProduct",
    CodeBlockLanguage: "csharp"
);

var renderer = new MarkdownRenderer(model, options);

// Generate one file per type
renderer.RenderToDirectory("./docs");

// Or combine everything into one Markdown file
renderer.RenderToSingleFile("./docs/api.md");
````

## Version History

**1.1.0 Highlights**

- Added support for `<remarks>`, `<example>`, `<seealso>`, `<exception>`, and `<inheritdoc/>`
- Added overload grouping for related members
- Improved type display names and generic formatting
- Added full snapshot test coverage to ensure consistent Markdown output
- Updated to target **.NET 9.0**

````
