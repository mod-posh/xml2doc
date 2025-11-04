#if NETSTANDARD2_0
using System;

namespace Xml2Doc.Core.Compat
{
    internal static class Net20Compat
    {
        // Replace s[a..b] with Slice(s, a, b)
        public static string Slice(string s, int start, int end)
            => s.Substring(start, end - start);

        // Replace s[^1] with FromEnd(s, 1)
        public static char FromEnd(string s, int indexFromEnd)
            => s[s.Length - indexFromEnd];

        // Replace an expression like s.AsSpan(i, len) with Substr(s, i, len)
        public static string Substr(string s, int start, int len)
            => s.Substring(start, len);
    }
}
#endif