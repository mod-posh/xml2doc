using System.Collections.Generic;

namespace Xml2Doc.Core.OutputLifecycle
{
    /// <summary>
    /// Describes the deterministic output lifecycle operations planned for an invocation.
    /// </summary>
    /// <param name="FilesToWrite">
    /// The normalized paths of files the current invocation will write.
    /// </param>
    /// <param name="FilesToDelete">
    /// The normalized paths of previously owned files that are now stale.
    /// </param>
    public sealed record OutputLifecyclePlan(
        IReadOnlyList<string> FilesToWrite,
        IReadOnlyList<string> FilesToDelete);
}