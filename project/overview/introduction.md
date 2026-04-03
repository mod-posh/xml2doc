[LLAMARC42-METADATA]
Type: Overview

Concepts: [
  "xml2doc",
  "XML documentation",
  "Markdown generation",
  "target audience",
  ".NET developer"
]

Scope: System

Confidence: Observed

Source: [
  "code",
  "docs"
]
[/LLAMARC42-METADATA]

# Introduction

## What Is xml2doc?

xml2doc is a .NET tool that transforms XML documentation files (`.xml`) — produced automatically by the C# compiler when `<GenerateDocumentationFile>true</GenerateDocumentationFile>` is set — into deterministic, linkable Markdown (`.md`) files.

It is not a static site generator, a documentation hosting platform, or a prose editing system. It performs one well-defined transformation: XML → Markdown.

## Who Is It For?

xml2doc targets any .NET developer who:

- Wants to publish API documentation as Markdown (e.g., in a GitHub repository, a wiki, or a docs site)
- Needs documentation generation integrated into the build pipeline (MSBuild / `dotnet build`)
- Wants reproducible, diff-able documentation output

The tool is actively used by the author across several projects and is designed so that any .NET developer can adopt it without special tooling beyond the standard .NET SDK.

## Delivery Mechanisms

xml2doc provides three entry points for the same Core engine:

| Entry Point | Use Case |
|------------|----------|
| **CLI tool** (`xml2doc`) | Manual invocation, scripting, CI pipelines |
| **MSBuild task** (`Xml2Doc.MSBuild`) | Automatic generation on every `dotnet build` or VS build |
| **Core library** (`Xml2Doc.Core`) | Embedding the engine in other tools |

## Current State

Version **1.4.0** (unreleased as of documentation date) completes multi-framework support and MSBuild maturity. The project has been through six milestone iterations:

1. Foundation — basic rendering
2. Renderer options and host parity
3. Output quality and regression safety
4. Signature and cref hardening
5. Link and anchor contract stabilization
6. Build and platform maturity (current)

A seventh milestone (structured diagnostics, pluggable anchor algorithms) is planned.

> **Cross-reference:** [goals.md](goals.md) · [scope.md](scope.md) · [architecture/solution-strategy.md](../architecture/solution-strategy.md)
