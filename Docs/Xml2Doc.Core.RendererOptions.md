# RendererOptions

Options that control how XML documentation is rendered to Markdown.

<a id="xml2doc.core.rendereroptions.#ctor(xml2doc.core.filenamemode,string,string)"></a>

## Method: #ctor(FileNameMode, string, string)

Options that control how XML documentation is rendered to Markdown.

**Parameters**

- `FileNameMode` — How type IDs should be transformed when generating Markdown file names.
- `RootNamespaceToTrim` — An optional namespace prefix to remove from displayed type names (e.g., `"MyCompany.MyProduct"`).
- `CodeBlockLanguage` — The language identifier to use for fenced code blocks (e.g., `"csharp"`).

<a id="xml2doc.core.rendereroptions.codeblocklanguage"></a>

## Property: CodeBlockLanguage

The language identifier to use for fenced code blocks (e.g., `"csharp"`).

<a id="xml2doc.core.rendereroptions.filenamemode"></a>

## Property: FileNameMode

How type IDs should be transformed when generating Markdown file names.

<a id="xml2doc.core.rendereroptions.rootnamespacetotrim"></a>

## Property: RootNamespaceToTrim

An optional namespace prefix to remove from displayed type names (e.g., `"MyCompany.MyProduct"`).
