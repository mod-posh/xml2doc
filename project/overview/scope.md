# Scope

## In scope

Xml2Doc currently supports:

- C# compiler XML documentation as primary input.
- Single-input and deterministic multi-input model loading.
- Reference-only XML for inheritance and cref resolution.
- Per-type and single-file Markdown output.
- Stable type/member links and selectable anchor algorithms.
- Namespace trimming, filename modes, TOCs, namespace indexes, and basename-only links.
- Templates, deterministic front matter, configurable aliases, auto-linking, external documentation fallback, and signature rendering services.
- Structured diagnostics surfaced consistently through CLI and MSBuild.
- Planning, reports, dry run, diff, bounded parallel rendering, incremental writes, and safe stale-output pruning.
- CLI configuration through command-line arguments and JSON.
- MSBuild normal per-project generation and opt-in repository aggregation.
- Deterministic aggregate ownership diagnostics (`XML2DOC006` and `XML2DOC007`).

## Not currently a goal

The project does not attempt to be:

- a general-purpose Markdown site generator;
- a Roslyn source-analysis replacement for compiler XML documentation;
- an arbitrary merge engine for independently generated Markdown;
- a documentation hosting service;
- an MSBuild solution-wide implicit aggregator with no declared owner.

Repository aggregation is explicit by design: one owner selects multiple primary XML inputs and renders one output set.

Future scope belongs in GitHub issues and milestones before it is represented as committed roadmap work.
