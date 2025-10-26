using System;

namespace Xml2Doc.Sample;

/// <summary>Types used to validate token-aware aliasing.</summary>
public static class AliasingPlayground
{
    /// <summary>
    /// Accepts a non-aliased BCL type that contains &quot;String&quot; as a substring.
    /// Used to ensure token-aware aliasing does not transform &quot;StringComparer&quot;.
    /// </summary>
    /// <param name="comparer">Comparer instance.</param>
    public static void UseComparer(StringComparer comparer) { }

    /// <summary>
    /// Mixes aliasable tokens with non-aliasable substrings so the renderer
    /// aliases true tokens while leaving larger identifiers intact.
    /// </summary>
    /// <param name="s">Example string.</param>
    /// <param name="x">An example Int32 value.</param>
    /// <param name="y">An example UInt32 value.</param>
    public static void Mix(string s, System.Int32 x, UInt32 y) { }
}