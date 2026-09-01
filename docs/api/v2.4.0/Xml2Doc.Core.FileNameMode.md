# FileNameMode

Controls how output file names are generated for documented types.

<a id="xml2doc.core.filenamemode.cleangenerics"></a>

## Field: CleanGenerics

Clean: remove generic arity tokens (e.g. `MyLib.Widget`1` → `MyLib.Widget.md`) and normalize XML‑doc generic braces (`{}` → `<>`) producing shorter, more stable file names across refactors.

<a id="xml2doc.core.filenamemode.verbatim"></a>

## Field: Verbatim

Verbatim: preserve the documentation identifier exactly (e.g. `MyLib.Widget`1` → `MyLib.Widget`1.md`). Generic arity tokens (``N`) and XML‑doc generic braces remain unchanged.
