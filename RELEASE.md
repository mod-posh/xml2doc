# 2.1.0 — Rendering Extensibility

## Objective

Extract Xml2Doc rendering behavior behind configurable extension points while preserving the default output contract established by the `2.0.x` releases.

## Included issues

- [x] #34 — Pluggable anchor algorithms (`IAnchorGenerator`)
- [x] #35 — Template hook and optional YAML front matter
- [x] #36 — Auto-link types and members in free text (`IAutoLinker`)
- [x] #37 — Configurable aliasing (`IAliasProvider`)
- [x] #38 — External documentation fallback for unresolved crefs
- [x] #40 — Extract `ISignatureRenderer` and improve signatures
- [ ] #43 — Matrix snapshots and anchor parity checks

## Completion criteria

- Default rendering remains compatible with the `2.0.x` baseline except for explicitly documented fixes.
- Built-in and consumer-provided services use the same rendering pipeline.
- Snapshot and parity tests cover per-type and single-file modes.
- Every built-in anchor algorithm maintains parity between emitted anchors and generated links.
- Auto-linking does not modify fenced code, inline code, existing links, or partial identifiers.
- Consumer-provided rendering services can be supplied without modifying `MarkdownRenderer`.
- Documentation describes the available extension points and their default behavior.
- All required GitHub Actions checks pass.

## Release process

- Associate issues #34, #35, #36, #37, #38, #40, and #43 with this milestone.
- Merge the #43 closeout PR.
- Close the milestone after all required checks pass.
- Tag the release commit as `2.1.0`.
- Verify the tag-triggered release and generated release notes.

## TASK, AREA:TESTS

* issue-43: [Tests] Matrix snapshots + anchor parity checks

## TASK, AREA:CORE

* issue-40: [Core] Extract ISignatureRenderer (+ constraints/indexers/params)
* issue-38: [Core] External docs fallback for unresolved crefs
* issue-37: [Core] Configurable aliasing (IAliasProvider)
* issue-36: [Core] Auto-link types/members in free text (IAutoLinker)
* issue-35: [Core] Template hook + optional YAML front matter
* issue-34: [Core] Pluggable anchor algorithms (IAnchorGenerator)

