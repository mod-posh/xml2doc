# OutputManifestLocation

Identifies the deterministic storage location for one invocation-scoped output manifest.

<a id="xml2doc.core.outputlifecycle.outputmanifestlocation.create(string,string)"></a>

## Method: Create(string, string)

Creates an invocation-scoped manifest location.

**Parameters**

- `outputRoot` — The root directory containing generated output.
- `identity` — The explicit, stable identity for the invocation.

**Returns**

The validated deterministic manifest location.

**Exceptions**

- [ArgumentException](System.ArgumentException.md) — The output root or manifest identity is missing or invalid.

<a id="xml2doc.core.outputlifecycle.outputmanifestlocation.identity"></a>

## Property: Identity

Gets the exact opaque identity supplied by the caller.

<a id="xml2doc.core.outputlifecycle.outputmanifestlocation.identityhash"></a>

## Property: IdentityHash

Gets the lowercase SHA-256 hexadecimal hash of the identity's exact UTF-8 bytes.

<a id="xml2doc.core.outputlifecycle.outputmanifestlocation.manifestpath"></a>

## Property: ManifestPath

Gets the canonical absolute path at which the invocation manifest is stored.

<a id="xml2doc.core.outputlifecycle.outputmanifestlocation.outputroot"></a>

## Property: OutputRoot

Gets the canonical absolute root containing the generated output.
