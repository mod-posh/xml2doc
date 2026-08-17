# Technical Constraints

## Target frameworks

- Core: `netstandard2.0`, `net8.0`, `net9.0`.
- CLI: `net8.0`, `net9.0`.
- MSBuild task: `net472`, `net8.0`.

Source changes must remain compatible with the lowest target in each component. The MSBuild package must not bundle MSBuild-owned toolset assemblies.

## Deterministic output

Generated Markdown is a compatibility surface. Equivalent inputs/options must remain stable across supported hosts. LF is the default line-ending policy; `native` is intentionally host-dependent.

Aggregate input order and MSBuild scheduling order must not affect generated ordering or bytes.

## Host separation

Core owns parsing/rendering semantics. CLI and MSBuild may implement host-specific validation, reporting, exit/status behavior, and incremental orchestration but must not fork Markdown rendering logic.

## Output safety

Stale pruning must never delete arbitrary unowned files. Manifest identities and normalized relative paths define the deletion boundary.

MSBuild incremental state must be invalidated when a required generated output is missing.

## CI and platform coverage

Repository validation runs across supported operating systems where behavior is platform-sensitive. Current suites include Windows, Linux, and macOS test coverage, plus Windows/Linux package and aggregation integration where appropriate.

## Release versioning

`Directory.Build.props` is the repository version source used by build/pack/release workflows. Release-preparation documentation and examples should use the version being prepared rather than historical package versions.
