namespace Xml2Doc.Sample;

/// <summary>Math helpers for demos.</summary>
public static class Mathx
{
    /// <inheritdoc cref="Add(int,int)"/>
    /// <summary>Alias that calls <see cref="Add(int,int)"/>.</summary>
    public static int AddAlias(int a, int b) => Add(a, b);

    /// <summary>Add two integers.</summary>
    /// <param name="a">Left addend.</param>
    /// <param name="b">Right addend.</param>
    /// <returns>Sum of <paramref name="a"/> and <paramref name="b"/>.</returns>
    /// <example><code>var s = Mathx.Add(1,2); // 3</code></example>
    public static int Add(int a, int b) => a + b;

    /// <summary>Add three integers.</summary>
    /// <param name="a">First.</param><param name="b">Second.</param><param name="c">Third.</param>
    /// <returns>Sum of all.</returns>
    public static int Add(int a, int b, int c) => a + b + c;
}
