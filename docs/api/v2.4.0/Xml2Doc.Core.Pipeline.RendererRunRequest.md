# RendererRunRequest

Describes one coordinated rendering invocation.

<a id="xml2doc.core.pipeline.rendererrunrequest.#ctor(string,xml2doc.core.pipeline.rendererrunmode,bool)"></a>

## Method: #ctor(string, RendererRunMode, bool)

Describes one coordinated rendering invocation.

**Parameters**

- `OutputPath` — Output directory for [PerType](Xml2Doc.Core.Pipeline.RendererRunMode.md#xml2doc.core.pipeline.rendererrunmode.pertype) or output file for [SingleFile](Xml2Doc.Core.Pipeline.RendererRunMode.md#xml2doc.core.pipeline.rendererrunmode.singlefile).
- `Mode` — The requested output shape.
- `DryRun` — When true, plan outputs without creating directories or writing files.

<a id="xml2doc.core.pipeline.rendererrunrequest.dryrun"></a>

## Property: DryRun

When true, plan outputs without creating directories or writing files.

<a id="xml2doc.core.pipeline.rendererrunrequest.mode"></a>

## Property: Mode

The requested output shape.

<a id="xml2doc.core.pipeline.rendererrunrequest.outputpath"></a>

## Property: OutputPath

Output directory for [PerType](Xml2Doc.Core.Pipeline.RendererRunMode.md#xml2doc.core.pipeline.rendererrunmode.pertype) or output file for [SingleFile](Xml2Doc.Core.Pipeline.RendererRunMode.md#xml2doc.core.pipeline.rendererrunmode.singlefile).
