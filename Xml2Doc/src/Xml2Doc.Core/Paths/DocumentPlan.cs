using System.Collections;
using System.Collections.ObjectModel;
using Xml2Doc.Core.Diagnostics;

namespace Xml2Doc.Core.Paths;

/// <summary>Provides the immutable authoritative path plan for a multi-document render.</summary>
public sealed class DocumentPlan : IReadOnlyList<DocumentPlanEntry>
{
    private static readonly char[] InvalidSegmentCharacters =
        { ':', '*', '?', '"', '<', '>', '|' };
    private readonly IReadOnlyList<DocumentPlanEntry> _entries;
    private readonly IReadOnlyDictionary<string, DocumentPlanEntry> _byDocumentId;

    private DocumentPlan(IReadOnlyList<DocumentPlanEntry> entries)
    {
        _entries = entries;
        _byDocumentId = new ReadOnlyDictionary<string, DocumentPlanEntry>(
            entries.ToDictionary(
                entry => entry.Document.DocumentId,
                StringComparer.Ordinal));
    }

    /// <inheritdoc />
    public int Count => _entries.Count;

    /// <inheritdoc />
    public DocumentPlanEntry this[int index] => _entries[index];

    /// <summary>Gets the planned document with the supplied stable identity.</summary>
    public DocumentPlanEntry Get(string documentId) => _byDocumentId[documentId];

    /// <summary>Attempts to get the planned document with the supplied stable identity.</summary>
    public bool TryGet(string documentId, out DocumentPlanEntry? entry) =>
        _byDocumentId.TryGetValue(documentId, out entry);

    /// <summary>Returns a relative logical link between two planned documents.</summary>
    public string GetRelativeLink(string fromDocumentId, string toDocumentId) =>
        GetRelativePath(Get(fromDocumentId).Path, Get(toDocumentId).Path);

    /// <inheritdoc />
    public IEnumerator<DocumentPlanEntry> GetEnumerator() => _entries.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal static DocumentPlan Create(
        IEnumerable<DocumentPathContext> documents,
        IDocumentPathResolver resolver)
    {
        if (documents is null)
            throw new ArgumentNullException(nameof(documents));
        if (resolver is null)
            throw new ArgumentNullException(nameof(resolver));

        var entries = new List<DocumentPlanEntry>();
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var identities = new HashSet<string>(StringComparer.Ordinal);

        foreach (var context in documents)
        {
            if (!identities.Add(context.Document.DocumentId))
            {
                throw new DocumentPathException(
                    DiagnosticIds.DuplicateDocumentPath,
                    $"Document identity '{context.Document.DocumentId}' is duplicated.");
            }

            var path = resolver.GetPath(context);
            ValidateLogicalPath(path, context.Document.DocumentId);
            if (paths.TryGetValue(path, out var existingDocumentId))
            {
                throw new DocumentPathException(
                    DiagnosticIds.DuplicateDocumentPath,
                    $"Documents '{existingDocumentId}' and '{context.Document.DocumentId}' " +
                    $"resolve to the same logical path '{path}'.");
            }

            paths.Add(path, context.Document.DocumentId);
            entries.Add(new DocumentPlanEntry(context.Document, path));
        }

        return new DocumentPlan(
            new ReadOnlyCollection<DocumentPlanEntry>(entries));
    }

    internal static void ValidateLogicalPath(string? path, string documentId)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            path.StartsWith("/", StringComparison.Ordinal) ||
            path.StartsWith("\\", StringComparison.Ordinal) ||
            path.IndexOf('\\') >= 0 ||
            (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':'))
        {
            throw UnsafePath(path, documentId);
        }

        var segments = path.Split('/');
        if (segments.Any(segment =>
                segment.Length == 0 ||
                segment == "." ||
                segment == ".." ||
                segment.EndsWith(".", StringComparison.Ordinal) ||
                segment.EndsWith(" ", StringComparison.Ordinal) ||
                segment.IndexOfAny(InvalidSegmentCharacters) >= 0))
        {
            throw UnsafePath(path, documentId);
        }
    }

    internal static string GetRelativePath(string fromPath, string toPath)
    {
        var from = fromPath.Split('/');
        var to = toPath.Split('/');
        var fromDirectoryLength = Math.Max(0, from.Length - 1);
        var common = 0;
        while (common < fromDirectoryLength &&
               common < to.Length - 1 &&
               string.Equals(from[common], to[common], StringComparison.Ordinal))
        {
            common++;
        }

        var parts = new List<string>();
        for (var index = common; index < fromDirectoryLength; index++)
            parts.Add("..");
        for (var index = common; index < to.Length; index++)
            parts.Add(to[index]);
        return string.Join("/", parts);
    }

    private static DocumentPathException UnsafePath(
        string? path,
        string documentId) =>
        new(
            DiagnosticIds.UnsafeDocumentPath,
            $"Document '{documentId}' resolved to unsafe logical path " +
            $"'{path ?? "<null>"}'. Paths must be relative, canonical, and traversal-free.");
}
