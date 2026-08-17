# OutputLifecyclePlan

Describes the deterministic output lifecycle operations planned for an invocation.

<a id="xml2doc.core.outputlifecycle.outputlifecycleplan.#ctor(system.collections.generic.ireadonlylist[string],system.collections.generic.ireadonlylist[string])"></a>

## Method: #ctor(IReadOnlyList<string>, IReadOnlyList<string>)

Describes the deterministic output lifecycle operations planned for an invocation.

**Parameters**

- `FilesToWrite` — The normalized paths of files the current invocation will write.
- `FilesToDelete` — The normalized paths of previously owned files that are now stale.

<a id="xml2doc.core.outputlifecycle.outputlifecycleplan.filestodelete"></a>

## Property: FilesToDelete

The normalized paths of previously owned files that are now stale.

<a id="xml2doc.core.outputlifecycle.outputlifecycleplan.filestowrite"></a>

## Property: FilesToWrite

The normalized paths of files the current invocation will write.
