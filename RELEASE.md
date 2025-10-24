# Version 1.1.0 Release

- Better display names (shorten namespaces, format `List<T>` generics).
- Support for `<remarks>`, `<example>`, `<seealso>`, `<exception>`.
- Single-file output flag `--single` for merged Markdown.
- File naming modes (toggle inclusion of generic arity/backticks).

## TASK

* issue-11: Add MSBuild flags for single-file mode, filename style, namespace trimming, and code-block language, bringing full feature parity with the updated MarkdownRenderer.
* issue-10: Update CLI to use RendererOptions, support new rendering features (single-file, filename modes), and align help/output with the improved MarkdownRenderer..
* issue-9: Enhance MarkdownRenderer with cleaner names, alias support, richer XML tag handling, and options for single-file output and filename modes.

## DOCUMENTATION

* issue-12: Add README to each project

