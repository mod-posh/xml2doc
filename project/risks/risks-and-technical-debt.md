# Risks and Technical Debt

## Documentation drift

Xml2Doc exposes the same capabilities through Core, CLI, and MSBuild, so examples can become stale when only one host guide is updated.

Mitigation:

- treat `Xml2Doc.md` as the current user-facing reference;
- keep package READMEs aligned with the repository version;
- review project/architecture pages during release preparation;
- validate example syntax against current source rather than historical docs.

## Generated-output compatibility

Changes to anchors, filenames, ordering, whitespace, or line endings can create broad downstream diffs even when APIs remain source-compatible.

Mitigation: snapshots/parity tests, explicit line-ending policy, deterministic ordering, and replaceable rendering services with backward-compatible defaults.

## MSBuild package-host compatibility

The task must load under both full-framework Visual Studio MSBuild and `dotnet` SDK MSBuild. Package layout errors can pass source-tree tests while failing clean consumers.

Mitigation: dual task TFMs and clean package integration tests, including a packaged repository aggregation owner.

## Incremental invalidation gaps

A missing input identity, reference file, renderer option, or output-ledger check can leave stale Markdown while MSBuild considers generation up to date.

Mitigation: explicit fingerprint inputs, output-ledger validation, separate aggregate lifecycle state, and integration tests for package/build scenarios.

## Multi-project ownership ambiguity

Independent projects writing a shared index are inherently race-prone.

Mitigation: one explicit aggregation owner, `XML2DOC007` ownership validation, and `Xml2Doc_GenerateIndex=false` as the compatibility pattern for independent shared-directory writers.

## Aggregate duplicate ownership

Two primary XML inputs may contain the same documentation member because of incorrect project/input boundaries.

Mitigation: deterministic failure with `XML2DOC006`; never resolve duplicates by caller order.

## Future technical debt

Future features should avoid introducing host-specific renderers or bypassing the runner/ownership contracts. New cross-cutting behavior should be represented by an issue/ADR when it changes an established compatibility boundary.
