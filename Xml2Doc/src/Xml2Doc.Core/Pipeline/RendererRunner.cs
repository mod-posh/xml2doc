using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Xml2Doc.Core.Pipeline;

/// <summary>
/// Coordinates deterministic output planning and rendering execution.
/// </summary>
/// <remarks>
/// This initial runner contract centralizes invocation behavior while preserving
/// the existing <see cref="MarkdownRenderer"/> entry points as the rendering
/// adapter. Incremental writes, pruning details, reporting, and parallel
/// scheduling can extend the result without changing the invocation boundary.
/// </remarks>
public sealed class RendererRunner
{
    private readonly MarkdownRenderer _renderer;

    /// <summary>Creates a runner for an initialized Markdown renderer.</summary>
    public RendererRunner(MarkdownRenderer renderer)
    {
        _renderer = renderer ??
            throw new ArgumentNullException(nameof(renderer));
    }

    /// <summary>Plans the absolute outputs for an invocation without writing.</summary>
    public IReadOnlyList<string> Plan(RendererRunRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        request.Validate();
        return request.Mode == RendererRunMode.SingleFile
            ? _renderer.PlanOutputs(
                outDir: string.Empty,
                singleFilePath: request.OutputPath)
            : _renderer.PlanOutputs(request.OutputPath);
    }

    /// <summary>Plans and optionally executes one rendering invocation.</summary>
    public RendererRunResult Run(RendererRunRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var planningStopwatch = Stopwatch.StartNew();
        var plannedFiles = Plan(request);
        planningStopwatch.Stop();

        if (request.DryRun)
        {
            IReadOnlyList<string> wouldPruneFiles = Array.Empty<string>();
            var lifecycleElapsed = TimeSpan.Zero;
            if (request.Mode == RendererRunMode.PerType &&
                _renderer.PrunesStaleFiles)
            {
                var lifecycleStopwatch = Stopwatch.StartNew();
                wouldPruneFiles = _renderer.PlanPrunedFiles(
                    request.OutputPath,
                    plannedFiles);
                lifecycleStopwatch.Stop();
                lifecycleElapsed = lifecycleStopwatch.Elapsed;
            }
            stopwatch.Stop();
            return CreateResult(
                plannedFiles,
                writtenFiles: Array.Empty<string>(),
                dryRun: true,
                stopwatch.Elapsed) with
            {
                PlanningElapsed = planningStopwatch.Elapsed,
                LifecycleElapsed = lifecycleElapsed,
                WouldPruneFiles = wouldPruneFiles
            };
        }

        RendererWriteResult writeResult;
        if (request.Mode == RendererRunMode.SingleFile)
            writeResult = _renderer.RenderToSingleFileWithResult(
                plannedFiles[0]);
        else
            writeResult = _renderer.RenderToDirectoryWithResult(
                System.IO.Path.GetFullPath(request.OutputPath));

        stopwatch.Stop();
        return new RendererRunResult(
            PlannedFiles: plannedFiles,
            WrittenFiles: writeResult.WrittenFiles,
            SkippedFiles: writeResult.SkippedFiles,
            PrunedFiles: writeResult.PrunedFiles,
            DryRun: false,
            Elapsed: stopwatch.Elapsed)
        {
            PlanningElapsed = planningStopwatch.Elapsed,
            RenderingElapsed = writeResult.RenderingElapsed,
            LifecycleElapsed = writeResult.LifecycleElapsed
        };
    }

    private static RendererRunResult CreateResult(
        IReadOnlyList<string> plannedFiles,
        IReadOnlyList<string> writtenFiles,
        bool dryRun,
        TimeSpan elapsed) =>
        new(
            PlannedFiles: plannedFiles,
            WrittenFiles: writtenFiles,
            SkippedFiles: Array.Empty<string>(),
            PrunedFiles: Array.Empty<string>(),
            DryRun: dryRun,
            Elapsed: elapsed);
}
