# Xml2Doc Roadmap

This roadmap tracks the release sequence and the work that is complete or planned in the repository.

The latest published release is `2.3.1`. The next planned releases are `2.4.0` — Metadata and Output Extensibility and `2.5.0` — API Surface Selection.

## Release sequence

1. `2.0.3` — Documentation and Lifecycle Correctness (released)
2. `2.1.0` — Rendering Extensibility (released)
3. `2.2.0` — Diagnostics and Pipeline (released)
4. `2.3.0` — Multi-project Aggregation (released)
5. `2.3.1` — Stabilization and Output Correctness (released)
6. `2.4.0` — Metadata and Output Extensibility (planned)
7. `2.5.0` — API Surface Selection (planned)

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

Released on August 17, 2026.

- [x] [#63 — Multiple projects targeting one output directory overwrite `index.md` nondeterministically](https://github.com/mod-posh/xml2doc/issues/63)

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
- [x] #63 closed after its issue checklist reflected the merged implementation.
- [x] Milestone 14 closed after required checks passed.
- [x] Milestone-triggered tag, GitHub release, NuGet packages, README refresh, release notes, and API documentation verified.

## 2.3.1 — Stabilization and Output Correctness

Released on August 30, 2026, with focused corrections for regressions and correctness defects affecting existing `2.3.0` behavior.

- [x] [#118 — Multi-input aggregation makes bare `<inheritdoc />` ambiguous across unrelated member signatures](https://github.com/mod-posh/xml2doc/issues/118)
- [x] [#119 — Xml2Doc.MSBuild incremental state survives `dotnet clean` and can leave generated Markdown stale](https://github.com/mod-posh/xml2doc/issues/119)
- [x] [#120 — Xml2Doc flattens XML documentation bullet lists into malformed Markdown](https://github.com/mod-posh/xml2doc/issues/120)

Architecture and quality notes:

- #118 belongs in Core inheritance resolution; prefer a contained resolver correction. Create an ADR only if the fix introduces a stronger symbol-ownership or relationship model.
- #119 belongs in the MSBuild lifecycle layer and must not delete generated Markdown during normal `Clean`.
- #120 belongs in Core XML-to-Markdown rendering and requires careful snapshot review because generated Markdown is a public output contract.
- Every issue requires focused regression coverage; #118 and #119 also require multi-input or multi-project integration coverage.

Milestone preparation:

- [x] Create the `2.3.1` milestone.
- [x] Assign #118, #119, and #120 to `2.3.1`.
- [x] Confirm issue scope and acceptance criteria before implementation.
- [x] Implement and close issues in the order #118, #119, #120.
- [x] Update the changelog and release documentation.
- [x] Prepare `VersionPrefix` for `2.3.1`.
- [x] Complete the standard milestone release workflow.

## 2.4.0 — Metadata and Output Extensibility

Planned architectural release for machine-addressable document metadata and deterministic output-layout extensibility.

- [x] [#115 — Expose richer per-document metadata through `TemplateRenderContext`](https://github.com/mod-posh/xml2doc/issues/115)
- [x] [#116 — Support caller-supplied metadata for deterministic per-document front matter](https://github.com/mod-posh/xml2doc/issues/116)
- [x] [#117 — Add pluggable document output-path/layout strategy](https://github.com/mod-posh/xml2doc/issues/117)

Implementation sequence and dependencies:

1. #115 defines the shared Core document identity and metadata context.
2. #116 builds on #115 and carries one metadata representation through Core, CLI, configuration, and MSBuild.
3. #117 introduces the authoritative document path model used by output planning, rendering, links, manifests, pruning, reports, and writes.

Architecture and quality notes:

- [ADR-014](docs/adr/ADR-014-deterministic-document-metadata.md) defines the accepted document
  metadata/context model and host-parity boundary for #115 and #116.
- [ADR-015](docs/adr/ADR-015-authoritative-document-paths.md) defines the accepted authoritative
  path plan for #117 because path generation and link routing are part of the public output contract.
- Keep all rendering, metadata composition, document identity, path planning, and link-resolution behavior in Core.
- CLI and MSBuild must expose shared Core capabilities without duplicating rendering behavior.
- Preserve the existing flat layout and existing template/front-matter behavior as backward-compatible defaults.
- Protect default output with unit, snapshot, parity, and sample-project coverage.

Milestone preparation:

- [x] Create the `2.4.0` milestone.
- [x] Assign #115, #116, and #117 to `2.4.0`.
- [x] Draft the required metadata and path/layout ADRs.
- [x] Review and accept the required ADRs before implementation.
- [x] Implement issues in dependency order: #115, #116, #117.
- [x] Update the roadmap, API documentation source, README examples, and migration guidance.
- [ ] Prepare `VersionPrefix` for `2.4.0`.
- [ ] Complete the standard milestone release workflow.

## 2.5.0 — API Surface Selection

Planned architectural release for defining which documented CLR symbols participate in generated output.

- [ ] [#125 — Add configurable API visibility filtering for generated documentation](https://github.com/mod-posh/xml2doc/issues/125)

Design sequence and dependency gates:

1. Decide the authoritative source of CLR accessibility metadata. Compiler-generated XML documentation identifies symbols but does not encode whether a type or member is public, internal, protected, or private.
2. Define a Core-owned symbol metadata model and deterministic enrichment boundary for single-input and aggregate loading.
3. Apply the selected visibility policy before output planning so rendering, indexes, TOCs, namespace indexes, reports, manifests, pruning, and writes consume the same filtered model.
4. Expose the shared Core policy consistently through CLI, JSON configuration, MSBuild task properties, and package properties.

Architecture and quality notes:

- #125 requires a new ADR because it changes the Core input/model contract, renderer configuration, output planning, host compatibility, and generated-output behavior.
- Keep accessibility discovery and filtering semantics in Core. CLI and MSBuild may supply inputs and expose options, but must not implement independent filtering.
- Preserve the current include-all-documented-symbols default unless the ADR deliberately defines and documents a versioned behavior change.
- Keep #125 separate from `TemplateRenderContext`: visibility determines whether a document exists and therefore must be resolved before per-document template metadata is created.
- Reuse the invocation-scoped ownership model from ADR-011 so changing visibility can safely prune only files owned by the same manifest identity.
- Protect both output modes and default compatibility with unit, snapshot, parity, sample-project, aggregation, and prune-manifest coverage.

Milestone preparation:

- [ ] Create the `2.5.0` milestone.
- [ ] Assign #125 to `2.5.0`.
- [ ] Confirm the accessibility metadata source and backward-compatible default before implementation.
- [ ] Draft and accept the required ADR before changing the Core model or public options.
- [ ] Update Core, CLI, JSON configuration, MSBuild, README/API documentation, and migration guidance together.
- [ ] Prepare `VersionPrefix` for `2.5.0`.
- [ ] Complete the standard milestone release workflow.

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
