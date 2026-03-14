# Project Roadmap

This roadmap reconstructs the historical evolution of xml2doc using release notes, milestones, and issue history. It helps contributors understand **what stage of maturity the project is in** and how current work fits into the larger direction.

---

## Milestone 1 — Foundation (v1.0.0)

Initial repository and architecture creation.

Key outcomes:

• repository structure
• Core rendering engine
• CLI entrypoint
• MSBuild integration
• initial XML → Markdown transformation

Architectural themes:

• Core / CLI / MSBuild separation
• basic Markdown renderer
• per‑type documentation generation

Related ADRs:

• ADR‑001 Scope and non‑goals
• ADR‑002 Solution structure

---

## Milestone 2 — Renderer Options & Host Parity (v1.1.0)

This milestone introduced a **shared configuration model**.

Key outcomes:

• RendererOptions
• CLI flags mapped to renderer options
• MSBuild properties mapped to renderer options
• improved XML tag handling

Architectural themes:

• configuration surface consistency
• host parity

Related ADRs:

• ADR‑003 Output modes
• ADR‑004 Configuration model

---

## Milestone 3 — Output Quality & Regression Safety (v1.2.0)

Focus shifted toward output correctness and stability.

Key outcomes:

• snapshot tests
• sample project for testing
• grouped rendering improvements
• CLI config support

Architectural themes:

• deterministic rendering
• regression safety

Related ADRs:

• ADR‑005 Regression strategy

---

## Milestone 4 — Signature & CREF Hardening (v1.2.1)

This milestone focused on fixing signature rendering issues.

Key outcomes:

• generic rendering fixes
• cref label fixes
• additional snapshot coverage

Architectural themes:

• signature correctness
• rendering reliability

Related ADRs:

• ADR‑006 Link and anchor stability

---

## Milestone 5 — Link & Anchor Contract Stabilization (v1.3.x)

This milestone hardened the internal link model.

Key outcomes:

• explicit member anchors
• alias normalization
• internal link correctness
• additional anchor tests

Architectural themes:

• stable internal links
• anchor consistency

Related ADRs:

• ADR‑006 Link and anchor stability

---

## Milestone 6 — Build & Platform Maturity (v1.4.x)

Current milestone.

Focus areas:

• incremental MSBuild execution
• report output
• multi‑target compatibility
• improved test infrastructure

Architectural themes:

• build performance
• host compatibility

Related ADRs:

• ADR‑007 MSBuild incremental generation
• ADR‑008 Multi‑target compatibility

---

## Milestone 7 — Diagnostics & Output Strategy (Future)

Emerging architectural work.

Potential areas:

• structured diagnostics
• pluggable anchor algorithms
• improved reporting

Related ADRs:

• ADR‑009 Structured diagnostics
• ADR‑010 Pluggable anchor algorithms

---

## Future opportunities

• richer CLI reporting
• improved diagnostics output
• documentation generation reports

• richer CLI reporting
• improved diagnostics output
• documentation generation reports
