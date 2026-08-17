# Xml2Doc Roadmap

This roadmap tracks the release sequence and the work that is complete in the repository.

The latest published release is `2.2.0`. The repository is being prepared for `2.3.0` — Multi-project Aggregation.

## Release sequence

1. `2.0.3` — Documentation and Lifecycle Correctness (released)
2. `2.1.0` — Rendering Extensibility (released)
3. `2.2.0` — Diagnostics and Pipeline (released)
4. `2.3.0` — Multi-project Aggregation (release preparation)

## 2.0.3 — Documentation and Lifecycle Correctness

Released focused correctness fixes on top of `2.0.2`.

- [x] [#69 — Render `<see langword="..."/>` correctly in generated Markdown](https://github.com/mod-posh/xml2doc/issues/69)
- [x] [#68 — Resolve `/// <inheritdoc />` content when generating Markdown](https://github.com/mod-posh/xml2doc/issues/68)
- [x] [#77 — Make stale-output ownership manifests portable across checkout paths](https://github.com/mod-posh/xml2doc/issues/77)
- [x] [#79 — MSBuild incremental state does not regenerate a missing generated Markdown file](https://github.com/mod-posh/xml2doc/issues/79)

Key outcomes:

- inherited documentation and language-keyword rendering are complete;
- output ownership manifests are portable and traversal-safe;
- missing generated files invalidate MSBuild incremental state and are recreated;
- project-reference XML can participate in inheritance lookup.

## 2.1.0 — Rendering Extensibility

Rendering behavior was moved behind configurable services while preserving default output compatibility.

- [x] [#34 — Pluggable anchor algorithms (`IAnchorGenerator`)](https://github.com/mod-posh/xml2doc/issues/34)
- [x] [#35 — Template hook and optional YAML front matter](https://github.com/mod-posh/xml2doc/issues/35)
- [x] [#36 — Auto-link types and members in free text (`IAutoLinker`)](https://github.com/mod-posh/xml2doc/issues/36)
- [x] [#37 — Configurable aliasing (`IAliasProvider`)](https://github.com/mod-posh/xml2doc/issues/37)
- [x] [#38 — External documentation fallback for unresolved crefs](https://github.com/mod-posh/xml2doc/issues/38)
- [x] [#40 — Extract `ISignatureRenderer` and improve signatures](https://github.com/mod-posh/xml2doc/issues/40)
- [x] [#43 — Matrix snapshots and anchor parity checks](https://github.com/mod-posh/xml2doc/issues/43)

Key outcomes:

- anchors, aliases, templates, auto-linking, external links, and signatures use replaceable Core services;
- built-in and custom rendering services share the same rendering path;
- snapshot and parity tests protect per-type and single-file link behavior.

## 2.2.0 — Diagnostics and Pipeline

Released on August 17, 2026.

- [x] [#39 — Structured diagnostics surfaced to CI](https://github.com/mod-posh/xml2doc/issues/39)
- [x] [#42 — Two-phase pipeline, parallel rendering, and incremental writes](https://github.com/mod-posh/xml2doc/issues/42)
- [x] [#44 — Expose remaining Core features through CLI flags](https://github.com/mod-posh/xml2doc/issues/44)

Key outcomes:

- Core emits stable structured diagnostics and host integrations map them to CLI/MSBuild output;
- the runner coordinates planning, rendering, dry run, diff, reporting, and output lifecycle behavior;
- bounded parallel rendering remains byte-for-byte deterministic with serial rendering;
- unchanged files are skipped instead of rewritten;
- reports distinguish planned, written, skipped, pruned, and non-mutating comparison results;
- the CLI exposes applicable templates, front matter, auto-linking, alias maps, external docs, anchor modes, parallelism, reports, dry run, diff, and lifecycle controls.

## 2.3.0 — Multi-project Aggregation

Implementation is complete and merged; the remaining work is release closeout.

- [x] [#63 — Multiple projects targeting one output directory overwrite `index.md` nondeterministically](https://github.com/mod-posh/xml2doc/issues/63) — implementation complete; issue/milestone closeout remains.

Key outcomes:

- Core can load multiple primary XML documentation files through one deterministic aggregation boundary;
- CLI supports repeated `--xml` and JSON `XmlInputs` aggregation;
- MSBuild supports an explicit repository aggregation owner with `Xml2Doc_AggregateEnabled=true`;
- one canonically ordered index contains all participating projects;
- parallel and serial repository aggregation produce identical file sets and bytes;
- `XML2DOC006` rejects duplicate primary member ownership;
- `XML2DOC007` rejects conflicting aggregate index ownership;
- the existing `Xml2Doc_GenerateIndex=false` shared-directory mitigation remains supported.

Release closeout:

- [x] Core aggregation implementation merged.
- [x] CLI aggregation implementation merged.
- [x] MSBuild repository-owner implementation merged.
- [x] Cross-platform parallel/serial integration coverage added.
- [x] User documentation updated for `2.3.0`.
- [x] `VersionPrefix` prepared for `2.3.0`.
- [ ] Close #63 after its issue checklist reflects the merged implementation.
- [ ] Close milestone 14 after required checks pass.
- [ ] Verify the milestone-triggered tag, GitHub release, NuGet packages, README refresh, release notes, and API documentation.

## Milestone workflow

For each milestone:

- [ ] Confirm every included issue has current scope and acceptance criteria.
- [ ] Assign issues and pull requests to the milestone.
- [ ] Add regression coverage with each implementation slice.
- [ ] Complete documentation and migration notes.
- [ ] Update `Directory.Build.props` to the milestone version before release.
- [ ] Close the milestone only after all required checks pass.
- [ ] Verify the tag-triggered release workflow, NuGet packages, release notes, README, and generated API documentation.

Future work should be tracked in GitHub issues and assigned to a milestone before it is added to this roadmap.
