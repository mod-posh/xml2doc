using System.Collections.Generic;

namespace Xml2Doc.Core.OutputLifecycle
{
    /// <summary>
    /// Describes the files owned by a single Xml2Doc output invocation.
    /// </summary>
    /// <param name="SchemaVersion">The manifest schema version.</param>
    /// <param name="Identity">The exact opaque identity of the owning invocation.</param>
    /// <param name="OutputRoot">
    /// The portable output-root marker. Version 1 manifests may contain a legacy absolute path.
    /// </param>
    /// <param name="Files">
    /// The normalized, output-root-relative paths owned by the invocation.
    /// </param>
    public sealed record OutputManifest(
        int SchemaVersion,
        string Identity,
        string OutputRoot,
        IReadOnlyList<string> Files)
    {
        /// <summary>
        /// Gets the schema version supported by the current implementation.
        /// </summary>
        public const int CurrentSchemaVersion = 2;

        /// <summary>
        /// Identifies the current output root without persisting a machine-specific path.
        /// </summary>
        public const string PortableOutputRoot = ".";
    }
}
