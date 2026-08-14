# OutputManifestSerializer

Serializes and loads invocation-scoped output manifests without filesystem access.

<a id="xml2doc.core.outputlifecycle.outputmanifestserializer.deserialize(string,xml2doc.core.outputlifecycle.outputmanifestlocation)"></a>

## Method: Deserialize(string, OutputManifestLocation)

Loads and validates a manifest for the requested invocation.

**Parameters**

- `json` — The JSON manifest content.
- `location` — The requested invocation-scoped manifest location.

**Returns**

A validated manifest with normalized, ordinally ordered owned paths.

**Exceptions**

- [InvalidDataException](System.IO.InvalidDataException.md) — The manifest is malformed, unsupported, inconsistent, or unsafe.

<a id="xml2doc.core.outputlifecycle.outputmanifestserializer.serialize(xml2doc.core.outputlifecycle.outputmanifestlocation,system.collections.generic.ireadonlylist[string])"></a>

## Method: Serialize(OutputManifestLocation, IReadOnlyList<string>)

Creates a validated manifest for the current invocation and serializes it deterministically.

**Parameters**

- `location` — The invocation-scoped manifest location.
- `ownedFiles` — The output-root-relative files owned by the invocation.

**Returns**

The deterministic JSON representation of the manifest.
