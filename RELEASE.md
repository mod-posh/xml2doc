# 2.4.0

## Metadata and Output Extensibility

Xml2Doc 2.4.0 expands generated documentation with deterministic, machine-addressable metadata and configurable document layouts. These capabilities support static-site generators, search and indexing systems, retrieval pipelines, and versioned API knowledge bases while preserving Xml2Doc’s generic architecture.

## Release highlights

- Expose stable per-document identity and metadata to templates and programmatic front-matter providers.
- Identify generated type, namespace, namespace-overview, index, and single-file documents without requiring consumers to parse Markdown.
- Support generic caller-supplied metadata consistently through Core, CLI, JSON configuration, and MSBuild.
- Merge caller and document-derived metadata using deterministic validation, precedence, ordering, and YAML serialization.
- Introduce an authoritative Core-owned document path model shared by output planning, rendering, links, reports, manifests, pruning, and file writes.
- Provide an opt-in namespace-folder layout while retaining the existing flat layout as the backward-compatible default.
- Detect unsafe paths and output collisions before writing or modifying generated-output ownership state.
- Preserve deterministic output and host parity across Windows, Linux, and macOS.

## Architecture and compatibility

- Core owns document identity, metadata composition, path planning, link routing, and validation.
- CLI and MSBuild expose shared Core behavior without implementing independent rendering or layout semantics.
- Existing templates, literal front-matter files, output paths, links, and default Markdown remain compatible unless a new capability is explicitly selected.
- Xml2Doc exposes only metadata supported by authoritative inputs. Compiler XML does not identify whether a type is a class, interface, record, struct, or enum, so CLR declaration-kind metadata is not inferred.
- Document paths are output-root relative, traversal-safe, deterministic across platforms, and validated for case-insensitive collisions.
- Custom layouts select document paths; Core derives relative links from the resolved document plan so routing cannot diverge from output placement.

## Included issues

- #115 — Expose richer per-document metadata through `TemplateRenderContext`
- #116 — Support caller-supplied metadata for deterministic per-document front matter
- #117 — Add a pluggable document output-path and layout strategy

## NO LABEL

* issue-117: Add pluggable document output-path/layout strategy
* issue-116: Support caller-supplied metadata for deterministic per-document front matter
* issue-115: Expose richer per-document metadata through TemplateRenderContext

