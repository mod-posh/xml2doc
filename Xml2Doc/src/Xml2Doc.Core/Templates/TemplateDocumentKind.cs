namespace Xml2Doc.Core.Templates;

/// <summary>Identifies the kind of Markdown document being templated.</summary>
public enum TemplateDocumentKind
{
    /// <summary>A generated page for one documented type.</summary>
    Type,

    /// <summary>The primary API index.</summary>
    Index,

    /// <summary>A namespace-specific index.</summary>
    NamespaceIndex,

    /// <summary>The overview that links to every namespace index.</summary>
    NamespaceOverview,

    /// <summary>A consolidated single-file API document.</summary>
    SingleFile
}
