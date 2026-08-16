using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
        var plannedFiles = Plan(request);

        if (request.DryRun)
        {
            stopwatch.Stop();
            return CreateResult(
                plannedFiles,
                writtenFiles: Array.Empty<string>(),
                dryRun: true,
                stopwatch.Elapsed);
        }

        var previousContent = plannedFiles.ToDictionary(
            path => path,
            path => File.Exists(path)
                ? File.ReadAllBytes(path)
                : null,
            StringComparer.Ordinal);

        if (request.Mode == RendererRunMode.SingleFile)
            _renderer.RenderToSingleFile(plannedFiles[0]);
        else
            _renderer.RenderToDirectory(
                System.IO.Path.GetFullPath(request.OutputPath));

        var writtenFiles = new List<string>();
        var skippedFiles = new List<string>();
        foreach (var path in plannedFiles)
        {
            var previousBytes = previousContent[path];
            if (previousBytes is not null &&
                File.Exists(path) &&
                previousBytes.SequenceEqual(File.ReadAllBytes(path)))
            {
                skippedFiles.Add(path);
            }
            else
            {
                writtenFiles.Add(path);
            }
        }

        stopwatch.Stop();
        return new RendererRunResult(
            PlannedFiles: plannedFiles,
            WrittenFiles: writtenFiles,
            SkippedFiles: skippedFiles,
            PrunedFiles: Array.Empty<string>(),
            DryRun: false,
            Elapsed: stopwatch.Elapsed);
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
