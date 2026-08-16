using Xml2Doc.Core.Models;

namespace Xml2Doc.Core.Signatures;

/// <summary>Formats documented types, members, and cref labels for Markdown output.</summary>
public interface ISignatureRenderer
{
    /// <summary>Formats a type documentation identifier for display.</summary>
    string RenderTypeName(string typeId);

    /// <summary>Formats a member heading, including its readable member kind.</summary>
    string RenderMemberHeader(XMember member, SignatureStyle style);

    /// <summary>Formats the visible label for an XML documentation cref.</summary>
    string RenderCrefLabel(string cref);
}
