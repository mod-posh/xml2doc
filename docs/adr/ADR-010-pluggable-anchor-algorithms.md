# ADR-010 — Pluggable Anchor Algorithms

Status: Accepted

## Context

Markdown consumers do not all derive heading anchors identically. Xml2Doc also needs stable internal links and a backward-compatible default algorithm.

## Decision

Anchor generation is provided through `IAnchorGenerator` and selected through `RendererOptions.AnchorAlgorithm` or a custom `AnchorGenerator` implementation.

Built-in modes include the backward-compatible `default` behavior plus supported alternate algorithms exposed by the CLI/Core configuration (including GitHub/GFM and Kramdown behavior).

All generated link targets and emitted anchors must use the same selected generator.

## Consequences

- Default output remains backward compatible unless the consumer selects another algorithm.
- Anchor generation is a replaceable rendering service rather than hard-coded CLI/MSBuild behavior.
- Regression coverage must validate anchor/link parity across built-in algorithms and output modes.

Implemented in the `2.1.0` rendering-extensibility release.
