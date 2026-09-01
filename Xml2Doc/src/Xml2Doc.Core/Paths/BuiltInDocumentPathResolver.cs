namespace Xml2Doc.Core.Paths;

/// <summary>Provides Xml2Doc's supported built-in document layouts.</summary>
public sealed class BuiltInDocumentPathResolver : IDocumentPathResolver
{
    private readonly DocumentLayout _layout;
    private readonly string? _rootNamespaceToTrim;

    /// <summary>Creates a resolver for a built-in layout.</summary>
    /// <param name="layout">Layout selected by the caller.</param>
    /// <param name="rootNamespaceToTrim">
    /// Optional namespace prefix removed from namespace directories.
    /// </param>
    public BuiltInDocumentPathResolver(
        DocumentLayout layout,
        string? rootNamespaceToTrim = null)
    {
        if (!Enum.IsDefined(typeof(DocumentLayout), layout))
            throw new ArgumentOutOfRangeException(nameof(layout));

        _layout = layout;
        _rootNamespaceToTrim = string.IsNullOrWhiteSpace(rootNamespaceToTrim)
            ? null
            : rootNamespaceToTrim;
    }

    /// <inheritdoc />
    public string GetPath(DocumentPathContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        if (_layout == DocumentLayout.Flat)
            return context.DefaultPath;

        return context.Document.Kind switch
        {
            Templates.TemplateDocumentKind.Type =>
                NamespaceDirectory(context.Document.Namespace) + "/" + context.FileName,
            Templates.TemplateDocumentKind.NamespaceIndex =>
                NamespaceDirectory(context.Document.Namespace) + "/index.md",
            _ => context.DefaultPath
        };
    }

    private string NamespaceDirectory(string? documentNamespace)
    {
        var value = documentNamespace;
        if (!string.IsNullOrWhiteSpace(value) &&
            !string.IsNullOrWhiteSpace(_rootNamespaceToTrim))
        {
            if (string.Equals(
                    value,
                    _rootNamespaceToTrim,
                    StringComparison.Ordinal))
            {
                value = null;
            }
            else
            {
                var prefix = _rootNamespaceToTrim + ".";
                if (value.StartsWith(prefix, StringComparison.Ordinal))
                    value = value.Substring(prefix.Length);
            }
        }

        if (string.IsNullOrWhiteSpace(value))
            return "namespaces/_global_";

        return "namespaces/" + string.Join(
            "/",
            value.Split('.')
                .Select(SafeSegment));
    }

    private static string SafeSegment(string segment) =>
        segment
            .Replace('<', '[')
            .Replace('>', ']')
            .Replace('+', '_');
}
