# OutputManifest

Describes the files owned by a single Xml2Doc output invocation.

<a id="xml2doc.core.outputlifecycle.outputmanifest.#ctor(int,string,string,system.collections.generic.ireadonlylist[string])"></a>

## Method: #ctor(int, string, string, IReadOnlyList<string>)

Describes the files owned by a single Xml2Doc output invocation.

**Parameters**

- `SchemaVersion` — The manifest schema version.
- `Identity` — The exact opaque identity of the owning invocation.
- `OutputRoot` — The portable output-root marker. Version 1 manifests may contain a legacy absolute path.
- `Files` — The normalized, output-root-relative paths owned by the invocation.

<a id="xml2doc.core.outputlifecycle.outputmanifest.currentschemaversion"></a>

## Field: CurrentSchemaVersion

Gets the schema version supported by the current implementation.

<a id="xml2doc.core.outputlifecycle.outputmanifest.files"></a>

## Property: Files

The normalized, output-root-relative paths owned by the invocation.

<a id="xml2doc.core.outputlifecycle.outputmanifest.identity"></a>

## Property: Identity

The exact opaque identity of the owning invocation.

<a id="xml2doc.core.outputlifecycle.outputmanifest.outputroot"></a>

## Property: OutputRoot

The portable output-root marker. Version 1 manifests may contain a legacy absolute path.

<a id="xml2doc.core.outputlifecycle.outputmanifest.portableoutputroot"></a>

## Field: PortableOutputRoot

Identifies the current output root without persisting a machine-specific path.

<a id="xml2doc.core.outputlifecycle.outputmanifest.schemaversion"></a>

## Property: SchemaVersion

The manifest schema version.
