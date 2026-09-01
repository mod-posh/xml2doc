using Xml2Doc.Core;

namespace Xml2Doc.Core.Templates;

/// <summary>Describes a rendered Markdown document before template application.</summary>
/// <param name="Content">The complete built-in Markdown content.</param>
/// <param name="Title">The document title, when available.</param>
/// <param name="Kind">The kind of generated document.</param>
public sealed record TemplateRenderContext(
    string Content,
    string? Title,
    TemplateDocumentKind Kind)
{
    /// <summary>
    /// Gets the logical identity metadata supplied by an Xml2Doc rendering operation.
    /// </summary>
    /// <remarks>
    /// This remains <see langword="null"/> when a context is constructed directly through the
    /// backward-compatible three-argument constructor.
    /// </remarks>
    public DocumentDescriptor? Document { get; init; }

    /// <summary>
    /// Gets the resolved output-root-relative logical path using forward slashes.
    /// </summary>
    /// <remarks>
    /// In-memory rendering that has no resolved output location exposes <see langword="null"/>.
    /// </remarks>
    public string? OutputPath { get; init; }

    /// <summary>
    /// Gets the immutable document-derived and caller-supplied metadata for this render.
    /// </summary>
    /// <remarks>
    /// Document-derived keys are authoritative when they collide with caller-supplied values.
    /// Directly constructed contexts expose an empty collection.
    /// </remarks>
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } =
        MetadataCollection.Empty;
}
