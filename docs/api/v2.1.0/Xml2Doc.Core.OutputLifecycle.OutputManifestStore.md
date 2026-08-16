# OutputManifestStore

Loads and atomically persists invocation-scoped output manifests.

<a id="xml2doc.core.outputlifecycle.outputmanifeststore.load(xml2doc.core.outputlifecycle.outputmanifestlocation)"></a>

## Method: Load(OutputManifestLocation)

Loads the requested manifest when it exists.

**Parameters**

- `location` — The invocation-scoped manifest location.

**Returns**

The validated manifest, or `null` when no manifest exists.

<a id="xml2doc.core.outputlifecycle.outputmanifeststore.save(xml2doc.core.outputlifecycle.outputmanifestlocation,system.collections.generic.ireadonlylist[string])"></a>

## Method: Save(OutputManifestLocation, IReadOnlyList<string>)

Validates and atomically persists the current invocation's ownership manifest.

**Parameters**

- `location` — The invocation-scoped manifest location.
- `ownedFiles` — The output-root-relative files owned by the invocation.
