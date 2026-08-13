# ADR-013 Deterministic Markdown Line Endings

## Status

Accepted

## Context

Markdown rendering currently relies on StringBuilder.AppendLine, which uses the host operating
system's newline sequence. Identical XML input can therefore produce LF on Linux and CRLF on
Windows. Templates and XML documentation can also introduce mixed newline sequences. This creates
large documentation-only diffs in consumer repositories such as BAT vNext and makes output bytes
depend on workstation and Git configuration.

## Decision

1. Core owns the Markdown line-ending policy through RendererOptions.
2. LF is the default on every supported host so identical input and options produce byte-identical
   Markdown independent of Environment.NewLine.
3. Callers may explicitly select CRLF or the current host's native newline for compatibility.
4. Core normalizes CRLF, LF, and lone CR sequences at the final Markdown output boundary. This
   applies to per-type pages, indexes, namespace pages, single-file output, templates, front matter,
   and in-memory rendering.
5. CLI and MSBuild expose the same Core setting. They do not perform their own newline conversion.
6. Markdown files are written as UTF-8 without a byte-order mark so encoding bytes are also stable
   across target frameworks and hosts.
7. The policy applies to generated Markdown, not JSON reports or lifecycle manifests.

## Consequences

- Default Markdown output is stable across Windows, Linux, and macOS.
- Existing consumers that require CRLF or native output can opt in explicitly.
- A first build after adopting the LF default may produce a one-time line-ending-only diff for files
  previously generated as CRLF.
- .gitattributes remains useful repository policy, but it is not required for renderer determinism.

## Alternatives Considered

### Use the platform-native newline by default

Rejected because it preserves the cross-platform instability reported in issue #67.

### Rely exclusively on .gitattributes or Git core.autocrlf

Rejected because renderer output must be deterministic before Git processes the file, and consumers
may generate documentation outside a Git worktree.

### Normalize only at file-write call sites

Rejected because RenderToString is also public output and must follow the same deterministic
contract. A shared final-output boundary keeps file and in-memory behavior aligned.
