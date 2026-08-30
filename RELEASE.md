# 2.3.0 — Multi-project Aggregation

Provide first-class deterministic aggregation for solutions where multiple projects publish documentation to one shared output directory.

## Scope

- Consume multiple XML documentation inputs through one aggregation boundary.
- Generate one canonically ordered index containing every participating project.
- Ensure serial and parallel aggregation produce identical output.
- Detect conflicting output or member ownership with actionable diagnostics.
- Preserve existing single-project behavior.
- Preserve `Xml2Doc_GenerateIndex=false` as the supported compatibility mitigation.

## Issues

- [ ] #63 — Multiple projects targeting one output directory overwrite `index.md` nondeterministically

## Completion criteria

- Native aggregation produces a deterministic unified index.
- Type and index ordering is stable and ordinal.
- Parallel and serial integration tests produce byte-identical output.
- Conflicting ownership fails with a stable structured diagnostic.
- CLI and MSBuild configuration are documented.
- All GitHub Actions checks pass.

## BUG, AREA:MSBUILD

* issue-63: MSBuild: multiple projects targeting one output directory overwrite `index.md` nondeterministically

