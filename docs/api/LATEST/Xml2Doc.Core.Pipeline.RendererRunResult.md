# RendererRunResult

Reports the deterministic outcome of a coordinated rendering invocation.

<a id="xml2doc.core.pipeline.rendererrunresult.#ctor(system.collections.generic.ireadonlylist[string],system.collections.generic.ireadonlylist[string],system.collections.generic.ireadonlylist[string],system.collections.generic.ireadonlylist[string],bool,system.timespan)"></a>

## Method: #ctor(IReadOnlyList<string>, IReadOnlyList<string>, IReadOnlyList<string>, IReadOnlyList<string>, bool, TimeSpan)

Reports the deterministic outcome of a coordinated rendering invocation.

**Parameters**

- `PlannedFiles` — Absolute output paths in deterministic order.
- `WrittenFiles` — Absolute paths written by this invocation.
- `SkippedFiles` — Absolute paths intentionally left unchanged.
- `PrunedFiles` — Absolute stale output paths removed after generation.
- `DryRun` — Whether the invocation only planned output.
- `Elapsed` — Total planning and execution duration.

<a id="xml2doc.core.pipeline.rendererrunresult.dryrun"></a>

## Property: DryRun

Whether the invocation only planned output.

<a id="xml2doc.core.pipeline.rendererrunresult.elapsed"></a>

## Property: Elapsed

Total planning and execution duration.

<a id="xml2doc.core.pipeline.rendererrunresult.lifecycleelapsed"></a>

## Property: LifecycleElapsed

Gets the duration spent evaluating or applying output lifecycle changes. Dry runs evaluate lifecycle changes without applying them.

<a id="xml2doc.core.pipeline.rendererrunresult.plannedfiles"></a>

## Property: PlannedFiles

Absolute output paths in deterministic order.

<a id="xml2doc.core.pipeline.rendererrunresult.planningelapsed"></a>

## Property: PlanningElapsed

Gets the duration spent planning deterministic outputs.

<a id="xml2doc.core.pipeline.rendererrunresult.prunedfiles"></a>

## Property: PrunedFiles

Absolute stale output paths removed after generation.

<a id="xml2doc.core.pipeline.rendererrunresult.renderingelapsed"></a>

## Property: RenderingElapsed

Gets the duration spent generating and writing Markdown.

<a id="xml2doc.core.pipeline.rendererrunresult.skippedfiles"></a>

## Property: SkippedFiles

Absolute paths intentionally left unchanged.

<a id="xml2doc.core.pipeline.rendererrunresult.wouldprunefiles"></a>

## Property: WouldPruneFiles

Gets stale output paths that would be pruned by a dry run. Empty for non-dry-run invocations.

<a id="xml2doc.core.pipeline.rendererrunresult.writtenfiles"></a>

## Property: WrittenFiles

Absolute paths written by this invocation.
