# RendererRunner

Coordinates deterministic output planning and rendering execution.

**Remarks**

This initial runner contract centralizes invocation behavior while preserving the existing [MarkdownRenderer](Xml2Doc.Core.MarkdownRenderer.md) entry points as the rendering adapter. Incremental writes, pruning details, reporting, and parallel scheduling can extend the result without changing the invocation boundary.

<a id="xml2doc.core.pipeline.rendererrunner.#ctor(xml2doc.core.markdownrenderer)"></a>

## Method: #ctor(MarkdownRenderer)

Creates a runner for an initialized Markdown renderer.

<a id="xml2doc.core.pipeline.rendererrunner.plan(xml2doc.core.pipeline.rendererrunrequest)"></a>

## Method: Plan(RendererRunRequest)

Plans the absolute outputs for an invocation without writing.

<a id="xml2doc.core.pipeline.rendererrunner.run(xml2doc.core.pipeline.rendererrunrequest)"></a>

## Method: Run(RendererRunRequest)

Plans and optionally executes one rendering invocation.
