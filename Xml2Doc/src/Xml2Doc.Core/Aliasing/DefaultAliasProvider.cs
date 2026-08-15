using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Xml2Doc.Core.Aliasing
{
    /// <summary>
    /// Applies the built-in C# keyword aliases for framework type tokens.
    /// </summary>
    public sealed class DefaultAliasProvider : IAliasProvider
    {
        private static readonly (string Full, string Alias)[] Aliases =
        {
            ("System.String", "string"), ("System.Int32", "int"),
            ("System.Boolean", "bool"), ("System.Object", "object"),
            ("System.Void", "void"), ("System.Int64", "long"),
            ("System.Int16", "short"), ("System.Byte", "byte"),
            ("System.SByte", "sbyte"), ("System.UInt32", "uint"),
            ("System.UInt64", "ulong"), ("System.UInt16", "ushort"),
            ("System.Char", "char"), ("System.Decimal", "decimal"),
            ("System.Double", "double"), ("System.Single", "float")
        };

        private static readonly IReadOnlyList<(Regex Pattern, string Alias)>
            FullTokenPatterns = Aliases
                .Select(alias =>
                    (new Regex(
                        $@"(?<![A-Za-z0-9_]){Regex.Escape(alias.Full)}(?![A-Za-z0-9_])"),
                     alias.Alias))
                .ToArray();

        private static readonly IReadOnlyList<(Regex Pattern, string Alias)>
            ShortTokenPatterns = Aliases
                .GroupBy(alias => alias.Full.Split('.').Last(), alias => alias.Alias)
                .Select(group =>
                    (new Regex(
                        $@"(?<![A-Za-z0-9_]){Regex.Escape(group.Key)}(?![A-Za-z0-9_])"),
                     group.First()))
                .ToArray();

        /// <summary>
        /// Gets the shared stateless default provider.
        /// </summary>
        public static DefaultAliasProvider Instance { get; } = new DefaultAliasProvider();

        private DefaultAliasProvider()
        {
        }

        /// <inheritdoc />
        public string ApplyAliases(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            foreach (var (pattern, alias) in FullTokenPatterns)
            {
                value = pattern.Replace(value, alias);
            }

            foreach (var (pattern, alias) in ShortTokenPatterns)
            {
                value = pattern.Replace(value, alias);
            }

            return value;
        }
    }
}
