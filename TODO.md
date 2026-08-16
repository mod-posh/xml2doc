# Xml2Doc Roadmap

This roadmap groups the current open issues into focused release milestones. The current stable release is `2.0.3`. Milestone versions are proposed and may be adjusted before release, but the dependency order should remain stable.

## Release sequence

1. `2.0.3` — Documentation and Lifecycle Correctness (released)
2. `2.1.0` — Rendering Extensibility (current)
3. `2.2.0` — Diagnostics and Pipeline
4. `2.3.0` — Multi-project Aggregation

## 2.0.3 — Documentation and Lifecycle Correctness

Released focused correctness fixes on top of `2.0.2`.

- [x] [#69 — Render `<see langword="..."/>` correctly in generated Markdown](https://github.com/mod-posh/xml2doc/issues/69)
- [x] [#68 — Resolve `/// <inheritdoc />` content when generating Markdown](https://github.com/mod-posh/xml2doc/issues/68)
- [x] [#77 — Make stale-output ownership manifests portable across checkout paths](https://github.com/mod-posh/xml2doc/issues/77)
- [x] [#79 — MSBuild incremental state does not regenerate a missing generated Markdown file](https://github.com/mod-posh/xml2doc/issues/79)

Completion criteria:

- Valid XML documentation constructs no longer produce missing or incomplete Markdown.
- Regression tests cover language keywords, explicit inheritance references, interface implementations, and overloaded members.
- Existing `<see cref="..."/>` rendering remains unchanged.
- Repository-relative output ownership can survive different absolute checkout locations.
- Manifest paths remain deterministic for stable project identities.
- A manifest cannot authorize traversal or deletion outside the current output root.
- Existing manifests migrate safely or fail with an actionable compatibility message.
- Tests cover Windows and Unix checkout roots, repository relocation, and multiple projects sharing one output directory.
- Documentation defines which lifecycle metadata is portable and which files should remain ignored.

## 2.1.0 — Rendering Extensibility

Extract renderer behavior behind configurable services while preserving the default output contract.

- [x] [#34 — Pluggable anchor algorithms (`IAnchorGenerator`)](https://github.com/mod-posh/xml2doc/issues/34)
- [x] [#35 — Template hook and optional YAML front matter](https://github.com/mod-posh/xml2doc/issues/35)
- [x] [#36 — Auto-link types and members in free text (`IAutoLinker`)](https://github.com/mod-posh/xml2doc/issues/36)
- [x] [#37 — Configurable aliasing (`IAliasProvider`)](https://github.com/mod-posh/xml2doc/issues/37)
- [x] [#38 — External documentation fallback for unresolved crefs](https://github.com/mod-posh/xml2doc/issues/38)
- [x] [#40 — Extract `ISignatureRenderer` and improve signatures](https://github.com/mod-posh/xml2doc/issues/40)
- [x] [#43 — Matrix snapshots and anchor parity checks](https://github.com/mod-posh/xml2doc/issues/43)

Completion criteria:

- Default rendering remains compatible with the `2.0.x` baseline except for explicitly documented fixes.
- Built-in and consumer-provided services use the same rendering pipeline.
- Snapshot and parity tests cover per-type and single-file modes.
- Auto-linking does not modify fenced or inline code.

Dependencies:

- #43 supplies regression coverage for #34 and #36.
- CLI exposure for these services is tracked in #44 and completes in `2.2.0`.

## 2.2.0 — Diagnostics and Pipeline

Add structured CI diagnostics and complete the runner-based parallel and incremental pipeline.

- [ ] [#39 — Structured diagnostics surfaced to CI](https://github.com/mod-posh/xml2doc/issues/39)
- [ ] [#42 — Two-phase pipeline, parallel rendering, and incremental writes](https://github.com/mod-posh/xml2doc/issues/42)
- [ ] [#44 — Expose remaining Core features through CLI flags](https://github.com/mod-posh/xml2doc/issues/44)

Completion criteria:

- Core emits stable diagnostic identifiers with severity and context.
- CLI and MSBuild present warnings and errors appropriately for CI.
- Serial and parallel rendering produce identical output.
- Unchanged files are not rewritten.
- Reports distinguish planned, written, skipped, and pruned files.
- All remaining Core extension points have documented CLI/configuration equivalents where appropriate.

Dependencies:

- #44 depends on the extension points delivered by #35–#39 and the runner delivered by #42.
- #39 should define the diagnostic contract before CLI and MSBuild mappings are finalized.

## 2.3.0 — Multi-project Aggregation

Provide first-class deterministic aggregation for solutions where multiple projects publish documentation to one output directory.

- [ ] [#63 — Multiple projects targeting one output directory overwrite `index.md` nondeterministically](https://github.com/mod-posh/xml2doc/issues/63)

Completion criteria:

- A repository or solution aggregation mode consumes multiple XML documentation inputs.
- One canonically ordered index contains all participating projects.
- Parallel and serial builds produce identical output.
- Conflicting ownership produces an actionable diagnostic.
- Existing single-project behavior and the `GenerateIndex=false` mitigation remain supported.

## Milestone workflow

For each milestone:

- [ ] Confirm every included issue has current scope and acceptance criteria.
- [ ] Assign the issues and pull requests to the milestone.
- [ ] Add regression coverage with each implementation slice.
- [ ] Complete documentation and migration notes.
- [ ] Close the milestone only after all required checks pass.
- [ ] Tag the release commit with the milestone version.
- [ ] Verify the tag-triggered release workflow and generated release notes.
