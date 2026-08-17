# Container View

Xml2Doc is organized into three shipped components plus tests/scripts/documentation around them.

## Xml2Doc.Core

A reusable library that owns XML parsing, models, reference/inheritance resolution, diagnostics, rendering, extensibility services, aggregation, planning, and output lifecycle behavior.

Core does not depend on CLI or MSBuild.

## Xml2Doc.Cli

A .NET tool/executable that maps command-line and JSON configuration onto Core. It owns CLI validation, process exit codes, console diagnostics, report configuration, and input precedence.

Repeated primary XML inputs are passed to Core aggregation rather than merged after rendering.

## Xml2Doc.MSBuild

A NuGet-delivered task integration for project builds. It contains:

- normal `GenerateMarkdownFromXmlDoc` single-project task/targets;
- `GenerateMarkdownFromXmlDocs` multi-input task;
- an opt-in repository aggregation target;
- incremental fingerprints/stamps/output ledgers;
- project-reference/reference-XML discovery and ownership validation.

## Validation/supporting assets

- `Xml2Doc.Tests` protects Core, CLI, MSBuild, snapshot, diagnostics, lifecycle, and aggregation contracts.
- PowerShell integration scripts exercise clean package consumers and repository build scenarios.
- GitHub Actions runs cross-platform tests and release workflows.
- `docs/` and `project/` contain user, architecture, ADR, roadmap, and generated API documentation.

## Dependency direction

```text
CLI ───────┐
           ▼
         Core
           ▲
MSBuild ───┘
```

Host integrations depend on Core; Core does not depend on either host.
