using System;
using System.Collections.Generic;

namespace Xml2Doc.Core.Pipeline;

/// <summary>
/// Reports the deterministic outcome of a coordinated rendering invocation.
/// </summary>
/// <param name="PlannedFiles">Absolute output paths in deterministic order.</param>
/// <param name="WrittenFiles">Absolute paths written by this invocation.</param>
/// <param name="SkippedFiles">Absolute paths intentionally left unchanged.</param>
/// <param name="PrunedFiles">Absolute stale output paths removed after generation.</param>
/// <param name="DryRun">Whether the invocation only planned output.</param>
/// <param name="Elapsed">Total planning and execution duration.</param>
public sealed record RendererRunResult(
    IReadOnlyList<string> PlannedFiles,
    IReadOnlyList<string> WrittenFiles,
    IReadOnlyList<string> SkippedFiles,
    IReadOnlyList<string> PrunedFiles,
    bool DryRun,
    TimeSpan Elapsed)
{
    /// <summary>Gets the duration spent planning deterministic outputs.</summary>
    public TimeSpan PlanningElapsed { get; init; }

    /// <summary>Gets the duration spent generating and writing Markdown.</summary>
    public TimeSpan RenderingElapsed { get; init; }

    /// <summary>
    /// Gets the duration spent evaluating or applying output lifecycle changes.
    /// Dry runs evaluate lifecycle changes without applying them.
    /// </summary>
    public TimeSpan LifecycleElapsed { get; init; }

    /// <summary>
    /// Gets stale output paths that would be pruned by a dry run. Empty for
    /// non-dry-run invocations.
    /// </summary>
    public IReadOnlyList<string> WouldPruneFiles { get; init; } =
        Array.Empty<string>();
}
