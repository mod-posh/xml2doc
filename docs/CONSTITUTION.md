# xml2doc Constitution

## Purpose

xml2doc converts .NET XML documentation files into deterministic, linkable Markdown.

The project exposes this through three hosts:

• Core library
• CLI tool
• MSBuild integration

The project **does not aim to be**:

• a static site generator
• a documentation hosting platform
• a prose editing system

## Principles

1. Deterministic output is more important than clever formatting.
2. Core owns semantics and rendering.
3. CLI and MSBuild only expose Core behavior.
4. Links and anchors are part of the public output contract.
5. Rendering regressions must be caught with tests.
6. Default behavior must remain stable unless explicitly changed.

## Governance rule

If code and ADRs disagree:

1. The ADR is the source of truth
2. Code must be updated
3. Or a new ADR must supersede the old one
