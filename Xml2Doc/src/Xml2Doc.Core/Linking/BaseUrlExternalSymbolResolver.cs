using System;

namespace Xml2Doc.Core.Linking;

/// <summary>
/// Resolves symbols beneath a common documentation URL by appending the escaped
/// identifier without its XML documentation kind prefix.
/// </summary>
public sealed class BaseUrlExternalSymbolResolver : IExternalSymbolResolver
{
    private readonly string _baseUrl;

    /// <summary>Creates a resolver for the supplied documentation base URL.</summary>
    /// <param name="baseUrl">The non-empty URL prefix used for resolved symbols.</param>
    public BaseUrlExternalSymbolResolver(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("A documentation base URL is required.", nameof(baseUrl));

        _baseUrl = baseUrl.Trim().TrimEnd('/');
    }

    /// <inheritdoc />
    public bool TryResolve(string cref, out string? href)
    {
        if (string.IsNullOrWhiteSpace(cref))
        {
            href = null;
            return false;
        }

        var identifier = cref.Length > 1 && cref[1] == ':'
            ? cref.Substring(2)
            : cref;
        href = _baseUrl + "/" + Uri.EscapeDataString(identifier);
        return true;
    }
}
