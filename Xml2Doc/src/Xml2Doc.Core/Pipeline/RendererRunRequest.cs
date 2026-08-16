using System;

namespace Xml2Doc.Core.Pipeline;

/// <summary>
/// Describes one coordinated rendering invocation.
/// </summary>
/// <param name="OutputPath">
/// Output directory for <see cref="RendererRunMode.PerType"/> or output file
/// for <see cref="RendererRunMode.SingleFile"/>.
/// </param>
/// <param name="Mode">The requested output shape.</param>
/// <param name="DryRun">
/// When true, plan outputs without creating directories or writing files.
/// </param>
public sealed record RendererRunRequest(
    string OutputPath,
    RendererRunMode Mode = RendererRunMode.PerType,
    bool DryRun = false)
{
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            throw new ArgumentException(
                "The output path must be specified.",
                nameof(OutputPath));
        }

        if (!Enum.IsDefined(typeof(RendererRunMode), Mode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(Mode),
                Mode,
                "The rendering mode is not supported.");
        }
    }
}
