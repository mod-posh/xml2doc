# 2.0.3 — Documentation and Lifecycle Correctness

Deliver focused documentation and output-lifecycle correctness fixes after the current 2.0.2 release.

Included issues:

- #68 — Resolve `/// <inheritdoc />` content when generating Markdown
- #69 — Render `<see langword="..."/>` correctly in generated Markdown
- #77 — Make stale-output ownership manifests portable across checkout paths
- #79 — Regenerate missing Markdown during incremental builds

Completion criteria:

- Valid XML documentation constructs render complete Markdown.
- Regression coverage includes language keywords, inherited documentation, interfaces, and overloads.
- Output ownership works across checkout locations without unsafe deletion.
- Missing generated files are recreated during incremental builds.
- Documentation clearly defines portable and local lifecycle metadata.

## BUG, GOOD FIRST ISSUE, AREA:CORE

* issue-69: Render <see langword="..."/> correctly in generated Markdown

## BUG, AREA:MSBUILD, AREA:TESTS

* issue-79: MSBuild incremental state does not regenerate a missing generated Markdown file

## BUG, AREA:CORE

* issue-77: Make stale-output ownership manifests portable across checkout paths
* issue-68: Resolve /// <inheritdoc /> content when generating Markdown

