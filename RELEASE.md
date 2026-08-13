# Version 2.0.0 — Deterministic Cross-Platform Output

## Goal

Make Xml2Doc output deterministic across operating systems by introducing explicit line-ending configuration and adopting LF as the default. This is a major release because changing the default from platform-native line endings may alter generated files, snapshots, hashes, and downstream automation on Windows.

## Scope (themes)

* Add configurable `LF`, `CRLF`, and `Native` line-ending styles
* Use LF as the default for deterministic cross-platform output
* Normalize final generated Markdown consistently
* Write UTF-8 output without a byte-order mark
* Expose line-ending configuration through the Core API
* Add the `--line-endings` CLI option
* Add the `Xml2Doc_LineEndings` MSBuild property
* Validate output across Windows, Linux, and macOS
* Document the breaking change and migration options
* Preserve `Native` mode for consumers requiring previous platform-specific behavior

## Acceptance checks

* Default output uses LF on all supported operating systems.
* Selecting `CRLF` produces CRLF-only output.
* Selecting `Native` preserves platform-native line-ending behavior.
* Generated files use UTF-8 without a byte-order mark.
* Core, CLI, and MSBuild configuration paths produce equivalent results.
* Invalid line-ending values produce actionable errors.
* Tests pass on Windows, Linux, and macOS.
* Existing renderer behaviour remains unchanged apart from the documented line-ending and encoding changes.
* Release documentation clearly explains how Windows consumers can restore the previous behaviour.

## Breaking changes

* The default generated line ending changes from platform-native to LF.
* Generated Markdown may differ byte-for-byte on Windows.
* Snapshot tests, checksums, source-control diffs, and downstream tools that depend on CRLF output may require updates.
* Consumers can select `Native` or `CRLF` explicitly when compatibility with previous Windows output is required.

## Issues and pull requests

* Issue #67 — deterministic line endings
* PR #71 — implementation, tests, CLI/MSBuild integration, and documentation

## Notes / References

* Version 1.4.0 is the compatibility baseline.
* ADR-013 documents the deterministic line-ending decision.
* This milestone intentionally uses a major version because the default output format changes for existing Windows consumers.

## NO LABEL

* issue-67: Preserve deterministic line endings in generated Markdown output

