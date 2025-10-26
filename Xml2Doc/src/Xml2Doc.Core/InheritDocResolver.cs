using System;
using System.Linq;
using System.Xml.Linq;
using Xml2Doc.Core.Models;

namespace Xml2Doc.Core
{
    internal static class InheritDocResolver
    {
        public static XElement? ResolveInheritedMember(Models.Xml2Doc model, XMember member)
        {
            // Case 1: explicit cref on inheritdoc
            var inherit = member.Element.Element("inheritdoc");
            var cref = inherit?.Attribute("cref")?.Value;
            if (!string.IsNullOrWhiteSpace(cref))
            {
                var key = cref;
                if (model.Members.TryGetValue(key, out var target))
                    return target.Element;
            }

            // Case 2: find matching member on base type or interfaces
            // Member.Id is like: Namespace.Type.Method(System.String)
            var id = member.Id;
            var lastDot = id.LastIndexOf('.');
            if (lastDot < 0) return null;

            var typeId = id.Substring(0, lastDot); // Namespace.Type
            var simple = id.Substring(lastDot + 1); // Method(...)

            // Try base type of 'typeId' by trimming nested suffix segments
            // (This is heuristic; full type system resolution would require reflection against binaries.)
            // We'll attempt N-1 namespace/type prefix reductions as a cheap fallback.
            var parts = typeId.Split('.');
            for (int cut = parts.Length - 1; cut >= 1; cut--)
            {
                var candidateTypeId = string.Join('.', parts.Take(cut));
                var candidateKey = $"M:{candidateTypeId}.{simple}";
                if (model.Members.TryGetValue(candidateKey, out var target))
                    return target.Element;
            }

            return null;
        }

        public static void MergeInheritedContent(XElement into, XElement from)
        {
            // Fill empty nodes only (don't override author-provided text)
            CopyIfMissing(into, "summary", from);
            CopyIfMissing(into, "remarks", from);
            CopyIfMissing(into, "returns", from);

            // Param-wise copy
            var intoParams = into.Elements("param")
                                 .ToDictionary(p => (string?)p.Attribute("name") ?? "", StringComparer.Ordinal);
            foreach (var p in from.Elements("param"))
            {
                var name = (string?)p.Attribute("name") ?? "";
                if (!intoParams.ContainsKey(name))
                    into.Add(new XElement(p));
            }

            // Exceptions, seealso, examples – append if not present
            var fromExceptions = from.Elements("exception");
            if (!into.Elements("exception").Any() && fromExceptions.Any())
                into.Add(fromExceptions);

            var fromSeeAlsos = from.Elements("seealso");
            if (!into.Elements("seealso").Any() && fromSeeAlsos.Any())
                into.Add(fromSeeAlsos);

            var fromExamples = from.Elements("example");
            if (!into.Elements("example").Any() && fromExamples.Any())
                into.Add(fromExamples);
        }

        private static void CopyIfMissing(XElement into, string name, XElement from)
        {
            if (into.Element(name) == null && from.Element(name) != null)
                into.Add(new XElement(from.Element(name)!));
        }
    }
}
