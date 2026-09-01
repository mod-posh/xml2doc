# CLI Component

`Xml2Doc.Cli` is the command-line host for Core. It targets `net8.0` and `net9.0` and installs as the `xml2doc` .NET tool.

## Responsibilities

- Parse command-line arguments and optional JSON configuration.
- Validate host-specific combinations before execution.
- Select one or more primary XML documentation inputs.
- Configure built-in renderer options and extension behaviors exposed by the CLI.
- Invoke Core's model loading/aggregation and runner pipeline.
- Emit structured diagnostics and stable process exit codes.
- Produce optional JSON execution reports.

## Primary input selection

- Repeated `--xml <path>` arguments are primary inputs and take highest precedence.
- When CLI XML arguments are absent, non-empty JSON `XmlInputs` is used.
- Legacy JSON `Xml` remains the compatible single-input fallback.
- One primary input uses `Xml2Doc.Load`; multiple primary inputs use `Xml2Doc.LoadAggregate`.

## Current option surface

The CLI exposes applicable Core behavior for output mode, filename mode, namespace trimming, code language, anchor algorithms, templates, front matter, caller metadata, auto-linking, alias maps, external documentation, TOCs, namespace indexes, index suppression, basename-only links, bounded parallelism, stale pruning, manifest identity, line endings, reports, dry run, and diff. Repeated `--metadata key=value` arguments override matching keys from the JSON `Metadata` object.

See [`../../Xml2Doc/src/Xml2Doc.Cli/README.md`](../../Xml2Doc/src/Xml2Doc.Cli/README.md) for the complete current option table and working examples.

## Runtime behavior

The CLI loads the model, creates `MarkdownRenderer` with `RendererOptions`, and uses the shared runner/reporting pipeline. Aggregate input paths are canonicalized by Core before loading, so argument order does not determine generated ordering.

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Success, including no differences for `--diff`. |
| `1` | Invalid CLI/configuration input. |
| `2` | Diagnostic or runtime failure. |
| `3` | Differences found by `--diff`. |

Warnings do not fail an otherwise successful invocation.
