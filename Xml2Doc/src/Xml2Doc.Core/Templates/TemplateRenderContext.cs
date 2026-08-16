namespace Xml2Doc.Core.Templates;

/// <summary>Describes a rendered Markdown document before template application.</summary>
/// <param name="Content">The complete built-in Markdown content.</param>
/// <param name="Title">The document title, when available.</param>
/// <param name="Kind">The kind of generated document.</param>
public sealed record TemplateRenderContext(
    string Content,
    string? Title,
    TemplateDocumentKind Kind);
