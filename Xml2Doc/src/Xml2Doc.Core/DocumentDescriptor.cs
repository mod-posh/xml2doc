using Xml2Doc.Core.Templates;

namespace Xml2Doc.Core;

/// <summary>
/// Identifies one logical Markdown document before template or front-matter application.
/// </summary>
/// <remarks>
/// The descriptor contains only identity supported by Xml2Doc's authoritative inputs. It does not
/// infer whether a documented type is a class, interface, record, struct, or enum.
/// </remarks>
public sealed record DocumentDescriptor
{
    /// <summary>Creates an immutable logical document descriptor.</summary>
    /// <param name="kind">The kind of generated Markdown document.</param>
    /// <param name="documentId">Stable logical identity for the generated document.</param>
    /// <param name="namespace">Applicable documented namespace, or <see langword="null"/>.</param>
    /// <param name="symbol">Applicable unqualified documented symbol, or <see langword="null"/>.</param>
    public DocumentDescriptor(
        TemplateDocumentKind kind,
        string documentId,
        string? @namespace = null,
        string? symbol = null)
    {
        if (string.IsNullOrWhiteSpace(documentId))
            throw new ArgumentException("A document identity is required.", nameof(documentId));
        if (@namespace is not null && string.IsNullOrWhiteSpace(@namespace))
            throw new ArgumentException("A document namespace cannot be empty.", nameof(@namespace));
        if (symbol is not null && string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("A document symbol cannot be empty.", nameof(symbol));

        Kind = kind;
        DocumentId = documentId;
        Namespace = @namespace;
        Symbol = symbol;
    }

    /// <summary>Gets the kind of generated Markdown document.</summary>
    public TemplateDocumentKind Kind { get; }

    /// <summary>Gets the stable logical identity for the generated document.</summary>
    public string DocumentId { get; }

    /// <summary>Gets the applicable documented namespace.</summary>
    public string? Namespace { get; }

    /// <summary>Gets the applicable unqualified documented symbol.</summary>
    public string? Symbol { get; }
}

