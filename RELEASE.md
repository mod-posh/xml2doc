# Version 1.3.0 Release

• Fixes and improvements to cross-references:
  • Add explicit anchors for members so cref links reliably resolve.
  • Single-file output now uses in-document anchors instead of per-file links.
• Safer type aliasing: token-aware replacements to avoid corrupting identifiers (e.g., “StringComparer”).
• Better formatting: preserve paragraph breaks from XML docs; keep whitespace tidy without collapsing newlines.
• More robust generic formatting: depth-aware splitting for nested generic argument labels.
• Tests expanded for anchors, link targets, generics, and formatting; snapshots updated as needed.
• Minor hygiene: remove unused usings; small cleanups.
• Docs updated to reflect link behavior and options.
• Version bump and changelog for 1.3.0.

## DOCUMENTATION

* issue-29: Update documentation for link behavior and renderer options

## ENHANCEMENT

* issue-30: Remove unused usings and minor code hygiene
* issue-28: Expand tests for anchors, links, generics, and formatting
* issue-27: Depth-aware generic argument splitting for labels
* issue-26: Preserve paragraph breaks in NormalizeXmlToMarkdown
* issue-25: Make aliasing token-aware in ApplyAliases
* issue-24: Use in-document anchors for single-file output
* issue-23: Add explicit stable anchors for members
