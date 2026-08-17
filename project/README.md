# Xml2Doc Project Documentation

This directory documents Xml2Doc's architecture, runtime flows, constraints, quality goals, risks, and terminology.

| Item | Current state |
| --- | --- |
| Repository version | `2.3.0` |
| Latest published release | `2.2.0` |
| Current release work | `2.3.0` multi-project aggregation closeout |
| Core TFMs | `netstandard2.0`, `net8.0`, `net9.0` |
| CLI TFMs | `net8.0`, `net9.0` |
| MSBuild task TFMs | `net472`, `net8.0` |

## Documentation map

- [`overview/`](overview/) — purpose, goals, and supported scope.
- [`architecture/`](architecture/) — system context, containers, components, and solution strategy.
- [`components/`](components/) — Core, CLI, and MSBuild responsibilities and contracts.
- [`workflows/`](workflows/) — key user scenarios and runtime flows.
- [`constraints/`](constraints/) — technical and organizational constraints.
- [`decisions/`](decisions/) — architecture decision summary; canonical ADRs live under [`../docs/adr/`](../docs/adr/).
- [`quality/`](quality/) — quality requirements and validation expectations.
- [`risks/`](risks/) — current risks and technical debt.
- [`glossary/`](glossary/) — project terminology.

For end-user usage, start with [`../Xml2Doc.md`](../Xml2Doc.md). For the `2.3.0` repository aggregation pattern, see [`../docs/msbuild-repository-aggregation.md`](../docs/msbuild-repository-aggregation.md).

## Current architecture at a glance

Xml2Doc has three public integration surfaces:

1. **Core** parses one or more primary compiler XML inputs, resolves references/inheritance, creates a deterministic model, and renders Markdown through replaceable rendering services.
2. **CLI** maps command-line/JSON configuration into Core options and runs the shared planning/rendering pipeline. Repeated `--xml` inputs use Core aggregation.
3. **MSBuild** integrates generation into project builds. Normal projects render their own compiler XML; an opt-in repository aggregation owner can collect project-reference XML and render one combined documentation set.

Determinism, diagnostics, output ownership, and incremental behavior are treated as output contracts rather than host-specific conveniences.
