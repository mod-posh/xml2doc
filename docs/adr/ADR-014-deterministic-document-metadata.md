# ADR-014 Deterministic Document Metadata

## Status

Accepted

## Context

`TemplateRenderContext` currently exposes only rendered content, a display title, and a document
kind. Templates and programmatic front-matter providers therefore cannot identify a generated
document without parsing Markdown or reconstructing renderer behavior.

Issue #115 requires authoritative per-document metadata. Issue #116 builds on that model by
allowing callers to contribute generic metadata through Core, CLI, and MSBuild. Both capabilities
must remain deterministic and must not introduce package-, product-, feed-, or hosting-specific
concepts.

Compiler-generated XML documentation contains documentation IDs and prose, but it does not encode
the CLR declaration kind of a type. In particular, it cannot distinguish a class, interface,
record, struct, or enum. Xml2Doc must not infer or claim metadata that its inputs cannot establish.

## Decision

1. Core owns an immutable document descriptor for every rendered document. The descriptor contains
   the existing `TemplateDocumentKind`, a stable document identity, and applicable namespace and
   symbol identity. It does not depend on a physical output root.
2. Type-document identity is based on the authoritative XML documentation ID. Index, namespace,
   namespace-overview, and single-file documents receive explicit Core-defined logical identities.
   These identities are public metadata contracts and are protected by tests.
3. Logical output paths use forward slashes, are relative to the output root, and are exposed
   alongside the descriptor only after output planning has resolved the document location.
   In-memory rendering that has no output location may expose no path.
4. `TemplateRenderContext` exposes the descriptor and optional logical output path while retaining
   its existing three-argument construction and existing `Content`, `Title`, and `Kind` members
   where practical. Core populates the same metadata for templates and programmatic front-matter
   providers.
5. Xml2Doc exposes only metadata supported by its authoritative inputs. The 2.4.0 model identifies a
   type document and its XML documentation symbol, but it does not label that symbol as a class,
   interface, record, struct, or enum. A future accepted enrichment model may add those values.
6. Caller-supplied metadata is represented in Core as an immutable, ordinally keyed collection and
   is copied at renderer construction so later caller mutation cannot change output. CLI, JSON
   configuration, and MSBuild project the same Core representation.
7. Caller metadata supports only the deterministic scalar and list shapes accepted by the existing
   YAML front-matter serializer. Keys are emitted in ordinal order. Unsupported shapes fail before
   output is written.
8. Document-derived metadata is authoritative when combined with caller metadata. Collision and
   precedence rules are implemented once in Core and are identical across hosts. Existing literal
   `FrontMatterPath` behavior remains unchanged and does not participate in metadata merging.
9. Existing templates and front-matter providers that do not opt into caller metadata retain their
   current output. Metadata support does not add front matter to default rendering.

## Consequences

- Templates and retrieval/indexing pipelines can identify documents without parsing rendered
  Markdown.
- Type, index, namespace, namespace-overview, and single-file contexts share one identity model.
- Host integrations remain projections of Core semantics.
- The existing flat output remains unchanged by default.
- Issue #115's request for a specific CLR type kind must be interpreted as future enrichment rather
  than inferred from compiler XML.
- The descriptor becomes the input to the document path strategy defined by ADR-015.

## Alternatives Considered

### Infer CLR declaration kind from naming or rendered content

Rejected because naming conventions and prose are not authoritative and would make metadata
incorrect for valid inputs.

### Expose only an arbitrary metadata dictionary

Rejected because document identity and path semantics are Core contracts. Strongly defined
document metadata prevents hosts and consumers from inventing incompatible keys and values.

### Let each host merge metadata independently

Rejected because precedence, validation, and serialization would drift across Core, CLI, and
MSBuild.
