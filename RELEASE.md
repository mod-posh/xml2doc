# Version 1.4.0 Release

## Goal
Make Xml2Doc more extensible and reliable by centralizing link/anchor logic, adding template/YAML hooks, enabling safe auto-linking in text, and unlocking incremental/perf gains—without breaking current GitHub-style anchors or per-type vs single-file modes.

## Scope (themes)
- Unified cref linking behind a resolver (type/member across modes)
- Pluggable anchor algorithms (GitHub/Docsify/Kramdown)
- Templating + optional YAML front matter
- Auto-linking within free text (skip code)
- Configurable aliasing and external-docs fallback
- Structured diagnostics (CI-friendly)
- Signature rendering service (constraints, indexers, params)
- Optional TOCs and namespace indexes
- Two-phase pipeline + parallel rendering + incremental writes
- CLI/MSBuild wiring for all of the above (opt-in flags/properties)

## Acceptance checks
- All crefs resolve correctly in both per-type and single-file outputs for the sample and tests (no regressions vs 1.3.1).
- Default anchors remain GitHub-compatible; alternative algorithms selectable via options.
- Template/front matter hook emits expected structure; defaults unchanged.
- Auto-linker modifies text only outside code blocks/inline code.
- Parallel render is deterministic; incremental runs skip unchanged outputs.
- CLI/MSBuild expose features with sensible defaults and emit actionable diagnostics.

## Notes / References
- 1.3.1 stabilized internal links and anchors (baseline to preserve). 
- Design intent & constraints captured in the conversation/context seed. 
(References: Core renderer & roadmap; conversation log.)

## BUG, GITHUB-ACTIONS

* issue-56: CI build intermittently fails on Windows with file locks in `obj\Release\*\*.GeneratedMSBuildEditorConfig.editorconfig` and `CS2012`

## TASK, AREA:CORE

* issue-41: [Core] Optional TOC and per-namespace index generation
* issue-33: [Core] Centralize cref linking behind ILinkResolver

## TASK, AREA:CORE, AREA:CLI, AREA:MSBUILD

* issue-46: Multi-target packages: ship net9.0 + net6.0 (Core optionally netstandard2.0) so MSBuild/IDE can load tasks in-process

## AREA:CORE, AREA:MSBUILD, FEATURE

* issue-47: Add option to trim root namespace from generated file names (not just headings)

## TASK, AREA:MSBUILD

* issue-45: [MSBuild] New properties + incremental build

## NO LABEL

* issue-64: Per-type generation leaves stale Markdown pages when documented types are removed or renamed

