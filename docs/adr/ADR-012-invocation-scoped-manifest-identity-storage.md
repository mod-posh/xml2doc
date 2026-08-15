# ADR-012 Invocation-Scoped Manifest Identity and Storage

## Status

Accepted

## Context

ADR-011 requires stale-output pruning to use an explicit, invocation-scoped manifest. A manifest
may authorize deletion only for paths previously recorded by the same invocation. Issue #64 also
requires multiple independent projects to share an output directory without deleting one another's
files.

The manifest identity and storage contract must therefore be stable across machines and builds,
safe to map to a filesystem path, and shared by Core, CLI, and MSBuild. Deriving identity from an
absolute XML, project, or output path would make ownership depend on checkout location, target
framework, or host-specific behavior. Using a caller-provided identity directly as a filename would
also introduce invalid-character, traversal, length, and collision risks.

## Decision

1. Stale-output pruning remains opt-in and requires an explicit, non-empty manifest identity. Core
   must not prune when no identity is supplied.
2. The manifest identity is an opaque, stable string chosen by the caller. Identity comparison is
   ordinal. Hosts must not silently derive it from absolute input, project, or output paths.
3. Core maps the UTF-8 bytes of the exact identity to a lowercase SHA-256 hexadecimal filename. The
   manifest is stored at:

   ```text
   <output-root>/.xml2doc/manifests/<identity-sha256>.json
   ```

   Hashing makes the storage path deterministic and prevents identity text from becoming path
   syntax. The `.xml2doc` directory is reserved for Xml2Doc lifecycle metadata and is not part of
   the generated Markdown output set.
4. The manifest records its original identity, schema version, a portable current-root marker, and
   normalized output-root-relative owned paths. Loading verifies that the stored identity ordinally
   matches the requested identity. The caller's current output root is always the deletion boundary;
   a manifest never supplies an absolute deletion root. Schema 1 manifests containing an absolute
   root migrate on their next successful save after their relative entries pass current safety
   validation.
5. CLI and MSBuild expose the same explicit Core identity concept. A shared output directory uses a
   distinct stable identity for each independent invocation. Reusing an identity intentionally
   transfers ownership of that manifest's recorded paths to the current invocation.
6. Identity resolution, manifest-location calculation, serialization, loading, atomic replacement,
   ownership-aware deletion, and transactional cleanup are Core behavior. CLI and MSBuild expose
   the same opt-in pruning and identity settings without implementing lifecycle behavior themselves.

## Consequences

- Independent projects can share an output root while maintaining disjoint ownership histories.
- Manifest locations are stable across checkout directories, machines, configurations, and target
  frameworks when callers retain the same identity.
- The `manifests` directory may be versioned when generated Markdown is versioned so clean checkouts
  retain stale-file ownership. The `transactions` directory is local staging state and must be
  ignored.
- Identity values cannot escape the metadata directory or create platform-specific filenames.
- A hash collision is cryptographically improbable, and the identity stored in the manifest allows
  a mismatch to fail safely instead of authorizing deletion.
- Callers must deliberately select and preserve an identity; pruning does not infer ownership from
  files already present in the output directory.
- The reserved metadata directory becomes part of the output lifecycle contract, but generated
  Markdown filenames and document structure remain unchanged.

## Alternatives Considered

### Derive identity from an absolute input or project path

Rejected because absolute paths vary across machines, agents, configurations, and repository
locations. That would create different ownership histories for the same logical invocation.

### Sanitize the identity into a readable filename

Rejected because sanitization can be platform-dependent and can map distinct identities to the same
filename. It also requires arbitrary length and character policies unrelated to ownership.

### Use one manifest per output directory

Rejected because independent projects sharing an output directory could claim and delete one
another's generated files.

### Derive identity from the current output set

Rejected because the identity would change when a type is added, removed, or renamed—the exact
changes for which a stable ownership history is required.
