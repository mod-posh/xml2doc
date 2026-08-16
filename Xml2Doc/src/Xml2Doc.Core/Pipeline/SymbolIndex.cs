using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using Xml2Doc.Core.Models;

namespace Xml2Doc.Core.Pipeline;

/// <summary>
/// Provides an immutable, deterministically ordered snapshot of the symbols used
/// during a rendering invocation.
/// </summary>
public sealed class SymbolIndex
{
    private SymbolIndex(
        IReadOnlyDictionary<string, XMember> members,
        IReadOnlyDictionary<string, XMember> referenceMembers)
    {
        Members = members;
        ReferenceMembers = referenceMembers;
        Types = members.Values
            .Where(member => member.Kind == "T")
            .OrderBy(member => member.Id, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Gets renderable symbols keyed by XML documentation ID.
    /// </summary>
    public IReadOnlyDictionary<string, XMember> Members { get; }

    /// <summary>
    /// Gets reference-only symbols used for inheritance lookup.
    /// </summary>
    public IReadOnlyDictionary<string, XMember> ReferenceMembers { get; }

    /// <summary>
    /// Gets renderable type symbols in ordinal documentation-ID order.
    /// </summary>
    public IReadOnlyList<XMember> Types { get; }

    /// <summary>
    /// Builds an immutable snapshot from a parsed XML documentation model.
    /// </summary>
    public static SymbolIndex Build(Models.Xml2Doc model)
    {
        if (model is null)
            throw new ArgumentNullException(nameof(model));

        return new SymbolIndex(
            Snapshot(model.Members),
            Snapshot(model.ReferenceMembers));
    }

    /// <summary>
    /// Determines whether a renderable symbol exists in the snapshot.
    /// </summary>
    public bool ContainsMember(string documentationId) =>
        Members.ContainsKey(documentationId);

    private static IReadOnlyDictionary<string, XMember> Snapshot(
        IReadOnlyDictionary<string, XMember> source)
    {
        var snapshot = source
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(
                pair => pair.Key,
                pair => new XMember(
                    pair.Value.Name,
                    new XElement(pair.Value.Element)),
                StringComparer.Ordinal);

        return new ReadOnlyDictionary<string, XMember>(snapshot);
    }
}
