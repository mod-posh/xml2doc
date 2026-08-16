namespace Xml2Doc.Core.Pipeline;

/// <summary>
/// Selects the output shape coordinated by <see cref="RendererRunner"/>.
/// </summary>
public enum RendererRunMode
{
    /// <summary>Generate one Markdown file per type and optional indexes.</summary>
    PerType = 0,

    /// <summary>Generate one consolidated Markdown file.</summary>
    SingleFile = 1
}
