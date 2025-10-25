# RendererOptions

Options that control how XML documentation is rendered to Markdown.

## Method: #ctor(FileNameMode, string, string)

Options that control how XML documentation is rendered to Markdown.

**Parameters**

- `FileNameMode` — How type IDs should be transformed when generating Markdown file names.
- `RootNamespaceToTrim` — An optional namespace prefix to remove from displayed type names (e.g., `"MyCompany.MyProduct"`).
- `CodeBlockLanguage` — The language identifier to use for fenced code blocks (e.g., `"csharp"`).

## Property: CodeBlockLanguage

The language identifier to use for fenced code blocks (e.g., `"csharp"`).

## Property: FileNameMode

How type IDs should be transformed when generating Markdown file names.

## Property: RootNamespaceToTrim

An optional namespace prefix to remove from displayed type names (e.g., `"MyCompany.MyProduct"`).
