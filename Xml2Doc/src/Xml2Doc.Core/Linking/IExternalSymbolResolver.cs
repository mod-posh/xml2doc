namespace Xml2Doc.Core.Linking;

/// <summary>Resolves XML documentation identifiers to external documentation URLs.</summary>
public interface IExternalSymbolResolver
{
    /// <summary>
    /// Attempts to resolve an XML documentation identifier such as
    /// <c>T:System.String</c> to an absolute or site-relative URL.
    /// </summary>
    /// <param name="cref">The complete XML documentation identifier.</param>
    /// <param name="href">The resolved URL when successful.</param>
    /// <returns><see langword="true"/> when a non-empty URL was resolved.</returns>
    bool TryResolve(string cref, out string? href);
}
