# Xml2Doc.Core

Core library for the Xml2Doc toolset, part of the **mod-posh** organization.

## Overview

`Xml2Doc.Core` is the engine that powers all Xml2Doc tools — including the CLI and MSBuild integration.
It handles XML parsing, type resolution, link conversion, and Markdown rendering in a way that’s simple to extend or reuse.

## Features

- Parses `<summary>`, `<remarks>`, `<example>`, `<seealso>`, `<exception>`, and `<inheritdoc/>`.
- Converts `<see>` and `<paramref>` into inline Markdown links and code spans.
- Cleans up namespaces and shortens generic types (e.g. `List<T>` instead of `System.Collections.Generic.List<T>`).
- Aliases built-in types (`System.String` → `string`, `System.Int32` → `int`) with token‑aware replacement to avoid corrupting identifiers (e.g., keeps `StringComparer` intact).
- Supports overload grouping for cleaner docs.
- Supports both **per-type** and **single-file** Markdown output with consistent link resolution.
- Emits stable, explicit HTML anchors for all members; in single-file output, types also get heading‑based anchors.
- Depth‑aware generic formatting in labels and signatures so nested generics (e.g., `Dictionary<string, List<Dictionary<string, int>>>`) render correctly.
- Paragraph‑preserving normalization: preserves paragraph breaks and fenced code blocks, trims excess spaces within lines, and fixes stray spaces before punctuation.
- Provides rich configuration through `RendererOptions`:
  - Filename mode (`Verbatim` or `CleanGenerics`)
  - Root namespace trimming (display-only)
  - Code block language (default: `csharp`)
  - Output mode selection (single vs. multi-file)
- Includes snapshot-tested output for consistent Markdown generation.

## Anchors and link behavior

- Per-type output (`RenderToDirectory`)
  - Types link to per-type files produced by the selected filename mode.
  - Members link to anchors within the type file: `Type.md#member-anchor`.
- Single-file output (`RenderToSingleFile` / `RenderToString`)
  - Types link to the in-document heading slug (derived from the visible type heading).
  - Members link to explicit in-document anchors.

Anchor strategy

- Members: each section begins with `<a id="..."></a>` computed from the member ID by:
  - applying C# aliases (e.g., `System.Int32` → `int`),
  - normalizing XML-doc generic braces `{}` → `[]` for HTML safety,
  - lowercasing the result (stable identifiers).
- Types (single-file only): each type section also emits an anchor derived from the visible heading text (GitHub-like slug).

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
