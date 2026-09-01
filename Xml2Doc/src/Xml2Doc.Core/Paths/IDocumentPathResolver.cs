namespace Xml2Doc.Core.Paths;

/// <summary>Selects one output-root-relative logical path for a generated document.</summary>
public interface IDocumentPathResolver
{
    /// <summary>Returns the canonical logical path for <paramref name="context"/>.</summary>
    string GetPath(DocumentPathContext context);
}
