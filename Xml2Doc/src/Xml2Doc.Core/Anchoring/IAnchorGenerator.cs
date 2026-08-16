namespace Xml2Doc.Core.Anchoring;

/// <summary>
/// Generates stable anchors for headings and documented members.
/// </summary>
public interface IAnchorGenerator
{
    /// <summary>Generates an anchor for a rendered heading.</summary>
    string GenerateHeadingAnchor(string heading);

    /// <summary>Generates an anchor for an XML documentation member identifier.</summary>
    string GenerateMemberAnchor(string memberId);
}
