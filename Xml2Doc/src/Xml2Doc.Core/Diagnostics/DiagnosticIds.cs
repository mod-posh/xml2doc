namespace Xml2Doc.Core.Diagnostics;

/// <summary>Stable identifiers emitted by Xml2Doc diagnostics.</summary>
public static class DiagnosticIds
{
    /// <summary>An XML documentation cref could not be resolved.</summary>
    public const string UnresolvedCref = "XML2DOC001";

    /// <summary>Multiple documented symbols generated the same anchor.</summary>
    public const string DuplicateAnchor = "XML2DOC002";

    /// <summary>An XML documentation input was malformed.</summary>
    public const string MalformedXml = "XML2DOC003";

    /// <summary>A documented symbol does not contain a summary.</summary>
    public const string MissingSummary = "XML2DOC004";

    /// <summary>An inheritdoc target could not be resolved.</summary>
    public const string UnresolvedInheritDoc = "XML2DOC005";
}
