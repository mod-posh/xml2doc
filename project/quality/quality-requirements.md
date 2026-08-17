# Quality Requirements

## Determinism

Generated Markdown is treated as a compatibility surface.

- Supported Core/CLI target frameworks must produce equivalent output for equivalent inputs/options.
- LF is the default line-ending policy across operating systems.
- Per-type parallel rendering must be byte-equivalent to serial rendering.
- Multi-input aggregation must be independent of caller input order.
- MSBuild repository aggregation must be byte-equivalent under normal parallel scheduling and `/m:1`.

## Correctness

Regression coverage should protect:

- XML documentation parsing and supported tags;
- `<inheritdoc />` across primary/reference XML;
- links, anchors, signatures, generic formatting, and aliasing;
- per-type and single-file output;
- structured diagnostic IDs and severity;
- aggregate duplicate-member and index-ownership failures;
- package task loading for clean consumers.

## Incremental behavior

- Unchanged files should not be rewritten.
- Relevant XML/configuration changes must invalidate generation state.
- Missing recorded outputs must be recreated.
- Reference XML changes must invalidate inherited documentation output.
- Aggregate participation and host-native newline policy must participate in aggregate fingerprinting where they affect output.

## Output safety

Stale pruning may delete only files owned by the exact invocation identity. Manifest paths must remain normalized and traversal-safe across checkout relocation.

## Host usability

- CLI invalid input fails with actionable validation and stable exit codes.
- MSBuild errors/warnings use native logging and identify conflicting project/path context where possible.
- Package examples should run against the repository version being prepared.

## Release validation

The release workflow builds, tests, packs, validates clean package consumers, installs the CLI tool, and regenerates versioned API documentation. Documentation changes must remain compatible with repository Markdown lint rules.
