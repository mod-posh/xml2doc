using System.Collections.Generic;

namespace Xml2Doc.Core.OutputLifecycle
{
    /// <summary>
    /// Describes the files owned by a single Xml2Doc output invocation.
    /// </summary>
    /// <param name="SchemaVersion">The manifest schema version.</param>
    /// <param name="Identity">The exact opaque identity of the owning invocation.</param>
    /// <param name="OutputRoot">The canonical root directory containing the generated files.</param>
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
        public const int CurrentSchemaVersion = 1;
    }
}
