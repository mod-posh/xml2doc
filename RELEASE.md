# 2.3.0 — Multi-project Aggregation

## Objective

Provide first-class deterministic aggregation for repositories where multiple projects contribute XML documentation to one shared Markdown output set.

This milestone builds on the structured diagnostics and runner pipeline delivered in `2.2.0`.

## Included issue

- [x] #63 — Multiple projects targeting one output directory overwrite `index.md` nondeterministically

## Added

### Core aggregation

- Added a multi-input aggregation boundary that accepts multiple primary XML documentation files.
- Canonicalized, de-duplicated, and ordinally sorted primary input paths before loading.
- Added one combined model and unified index for all participating projects.
- Added deterministic duplicate-member ownership failure through `XML2DOC006`.
- Preserved the existing single-input Core loading path.

### CLI aggregation

- `--xml` may now be repeated to aggregate multiple primary XML documentation files.
- JSON configuration supports `XmlInputs` while retaining the compatible single-input `Xml` property.
- CLI `--xml` arguments take precedence over both JSON input properties.
- Aggregate reports include canonical `xmlInputs` while retaining `xml` for the first canonical input.
- One input continues to use the compatible single-input path; two or more use Core aggregation.

### MSBuild repository aggregation

- Added an opt-in repository aggregation owner using `Xml2Doc_AggregateEnabled=true`.
- Added automatic primary XML collection from resolved `ProjectReference` outputs.
- Added explicit `Xml2Doc_AggregateXml` primary inputs.
- Kept `Xml2Doc_ReferenceXml` separate for inheritance/reference resolution only.
- Added separate aggregate stamp, fingerprint, output ledger, and report defaults.
- Added `XML2DOC007` when a referenced Xml2Doc project still claims the aggregate `index.md`.
- Normalized output-directory ownership comparisons so equivalent trailing-separator forms cannot bypass validation.
- Included primary/reference XML identities, significant renderer options, and host-native newline policy in aggregate incremental tracking.

## Determinism and compatibility

- One aggregate invocation owns one output set and one unified index.
- Type and index ordering is stable and ordinal.
- Parallel and serial MSBuild aggregation are required to produce identical file sets and bytes.
- Existing single-project behavior remains compatible.
- `Xml2Doc_GenerateIndex=false` remains supported as the compatibility mitigation for independent projects sharing an output directory without a unified index.

## Tests

- Added Core tests for canonical multi-input ordering and deterministic duplicate-member failure.
- Added CLI end-to-end tests for repeated `--xml`, JSON `XmlInputs`, precedence, and input-order independence.
- Added MSBuild task/unit coverage for aggregate ordering, missing input failure, packaging, and ownership wiring.
- Added Windows and Linux repository integration coverage comparing normal parallel scheduling with `/m:1` byte-for-byte.
- Expanded package integration to build an aggregation owner from the packed `Xml2Doc.MSBuild` NuGet package.

## Documentation

- Updated `Xml2Doc.md` and root README for `2.3.0`.
- Updated CLI and MSBuild package READMEs with functional `2.3.0` examples.
- Added and refreshed the repository aggregation owner guide.
- Updated the changelog and roadmaps to reflect the released `2.2.0` work and completed `2.3.0` implementation.

## Release process

- [x] Merge Core aggregation.
- [x] Merge CLI aggregation.
- [x] Merge MSBuild repository aggregation.
- [x] Update `VersionPrefix` to `2.3.0`.
- [x] Update user documentation, package examples, changelog, and roadmap.
- [ ] Update and close #63 after its checklist reflects the merged implementation.
- [ ] Close milestone 14 after all required checks pass.
- [ ] Verify the milestone-triggered tag and GitHub release.
- [ ] Verify `Xml2Doc.Core`, `Xml2Doc.Cli`, and `Xml2Doc.MSBuild` `2.3.0` packages on NuGet.
- [ ] Verify the generated README and versioned API documentation.
