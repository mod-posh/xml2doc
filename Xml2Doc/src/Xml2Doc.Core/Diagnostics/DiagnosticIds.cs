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

    /// <summary>Multiple XML documentation inputs define the same member.</summary>
    public const string DuplicateInputMember = "XML2DOC006";

    /// <summary>Multiple MSBuild projects claim ownership of the same generated index.</summary>
    public const string ConflictingIndexOwnership = "XML2DOC007";

    /// <summary>A document path resolver returned an unsafe logical path.</summary>
    public const string UnsafeDocumentPath = "XML2DOC008";

    /// <summary>Multiple documents resolved to the same logical path.</summary>
    public const string DuplicateDocumentPath = "XML2DOC009";
}
