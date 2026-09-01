# ADR-015 Authoritative Document Paths and Layout

## Status

Accepted

## Context

Xml2Doc currently calculates type, index, and namespace paths in several renderer branches. Link
generation separately assumes the default flat type layout. Moving generated files after rendering
can therefore break links, reports, manifests, and stale-output ownership.

Issue #117 requires replaceable output layouts while preserving the current flat layout and the
public link/output contract. Path selection must be resolved before rendering and shared by every
downstream stage.

## Decision

1. Core owns a document path resolver that maps the immutable document descriptor from ADR-014 to
   one output-root-relative logical path.
2. The resolver selects paths only. Relative links are derived centrally from the resolved source
   and destination paths; a custom resolver cannot provide a second, potentially inconsistent link
   algorithm.
3. Core resolves the complete document set into one immutable document plan before rendering. The
   plan is the authoritative source for rendering, indexes, namespace pages, link resolution,
   `PlanOutputs`, reports, manifests, pruning, and writes.
4. Logical paths use `/` as the canonical separator. Core rejects rooted paths, empty paths, `.` or
   `..` traversal segments, paths outside the output root, and duplicate paths. Collision detection
   is ordinal-ignore-case on every host so a plan accepted on Linux cannot fail or overwrite a
   different document on Windows.
5. The built-in `Flat` layout remains the default and preserves the existing paths and Markdown
   bytes. A built-in `NamespaceFolders` layout places type documents in deterministic namespace
   directories and is opt-in.
6. Core permits a custom programmatic resolver. CLI and MSBuild expose only named built-in layouts
   through the shared Core configuration unless a future ADR defines a safe declarative custom
   mapping format.
7. Single-file output continues to use the caller's explicit output file. Layout strategies govern
   the multi-document output set and do not reinterpret that explicit path.
8. Unsafe paths and collisions fail before any output or ownership manifest is changed. New failure
   classes receive stable structured diagnostics in accordance with ADR-009.

## Consequences

- Every generated link and lifecycle operation consumes the same resolved path plan.
- Custom layouts cannot silently diverge from link routing.
- The default layout remains backward compatible.
- Non-flat layouts intentionally change public paths and links and therefore require explicit
  selection and dedicated regression snapshots.
- Output planning becomes a first-class phase before Markdown rendering.
- ADR-011 and ADR-012 ownership boundaries remain valid because manifests continue to store
  validated output-root-relative paths.

## Alternatives Considered

### Let the resolver calculate both paths and relative links

Rejected because two independent methods can disagree. Relative links are a deterministic function
of the authoritative source and destination paths and belong in Core's shared link pipeline.

### Move or rename files after rendering

Rejected because rendered links, reports, and ownership manifests would describe the old paths.

### Use host-native separators in the logical plan

Rejected because the same input and options would produce different metadata, reports, and
manifest entries across operating systems.

### Detect collisions using the current filesystem's comparer

Rejected because a layout accepted on a case-sensitive host could overwrite documents on a
case-insensitive host.
