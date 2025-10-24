namespace Xml2Doc.Sample;

using System.Collections.Generic;

/// <summary>Types used to exercise nested generic signatures in XML docs.</summary>
public sealed class XItem { }

/// <summary>Generic playground for exercising nested generic parameter lists.</summary>
public static class GenericPlayground
{
    /// <summary>
    /// Flattens a nested sequence.
    /// </summary>
    /// <remarks>
    /// This used to surface a stray brace in signatures like
    /// <c>IEnumerable{IEnumerable{XItem}}</c>. The renderer must output
    /// <c>IEnumerable&lt;IEnumerable&lt;XItem&gt;&gt;</c> (no extra <c>}</c>).
    /// See also <see cref="Flatten(System.Collections.Generic.IEnumerable{System.Collections.Generic.IEnumerable{Xml2Doc.Sample.XItem}})"/>.
    /// </remarks>
    public static IEnumerable<XItem> Flatten(IEnumerable<IEnumerable<XItem>> source)
    {
        foreach (var inner in source)
            foreach (var x in inner)
                yield return x;
    }

    /// <summary>
    /// Builds an index over a nested structure.
    /// </summary>
    /// <param name="map">Nested map to index.</param>
    /// <returns>Total number of leaf items.</returns>
    /// <remarks>
    /// Signature includes <c>Dictionary{string, List{XItem}}</c> which must render as
    /// <c>Dictionary&lt;string, List&lt;XItem&gt;&gt;</c> (no stray braces).
    /// </remarks>
    public static int Index(Dictionary<string, List<XItem>> map)
    {
        var count = 0;
        foreach (var kv in map)
            count += kv.Value?.Count ?? 0;
        return count;
    }

    /// <summary>
    /// Tests generic method arity formatting and nested generic parameters.
    /// </summary>
    public static T2? Transform<T1, T2>(List<Dictionary<T1, List<T2>>> input)
        where T1 : notnull
        where T2 : class
        => default;
}
