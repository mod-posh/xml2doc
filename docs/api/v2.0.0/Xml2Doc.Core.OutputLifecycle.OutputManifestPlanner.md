# OutputManifestPlanner

Creates deterministic output lifecycle plans from current outputs and prior ownership.

<a id="xml2doc.core.outputlifecycle.outputmanifestplanner.createplan(string,system.collections.generic.ireadonlylist[string],xml2doc.core.outputlifecycle.outputmanifest)"></a>

## Method: CreatePlan(string, IReadOnlyList<string>, OutputManifest)

Creates an output lifecycle plan for the current invocation.

**Parameters**

- `outputRoot` — The root directory containing the generated output.
- `plannedOutputs` — The output files planned by the current invocation.
- `previousManifest` — The prior ownership manifest, or when no prior ownership exists.

**Returns**

A deterministic plan containing the files to write and stale owned files to delete.

**Exceptions**

- [ArgumentException](System.ArgumentException.md) — The current output root or planned outputs are invalid.
- [InvalidDataException](System.IO.InvalidDataException.md) — The previous manifest is unsupported, inconsistent, or unsafe.
