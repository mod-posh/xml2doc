namespace Xml2Doc.Core.AutoLinking;

/// <summary>Links known identifiers in free-form Markdown text.</summary>
public interface IAutoLinker
{
    /// <summary>Applies links while preserving protected Markdown regions.</summary>
    string Apply(string markdown, AutoLinkContext context);
}
