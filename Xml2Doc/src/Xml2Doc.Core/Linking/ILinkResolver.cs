using System;

namespace Xml2Doc.Core.Linking
{
    /// <summary>Ambient context for resolving a cref into a Markdown link.</summary>
    internal sealed record LinkContext(string? CurrentTypeId, bool SingleFile, string? BasePath);

    /// <summary>The final Markdown link parts.</summary>
    internal sealed record MarkdownLink(string Href, string Label);

    /// <summary>Converts an XML-doc cref (e.g., "T:Ns.Type", "M:Ns.Type.Member(...)") to a Markdown link.</summary>
    internal interface ILinkResolver
    {
        MarkdownLink Resolve(string cref, LinkContext context);
    }
}
