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
                var key = cref!;
                if (model.Members.TryGetValue(key, out var target))
                    return target.Element;

                // An explicit target is authoritative. If it is not present in the
                // loaded documentation, do not guess a different member by signature.
                return null;
            }
            // Case 2: XML documentation does not record the implemented interface or base member.
            // Match the complete member suffix (including parameter types) only when one documented
            // candidate exists. Ambiguous matches deliberately remain unresolved.
            var signature = GetMemberSignature(member.Id);
            if (signature is null)
                return null;

            var candidates = model.Members.Values
                .Where(candidate =>
                    !ReferenceEquals(candidate, member) &&
                    string.Equals(candidate.Kind, member.Kind, StringComparison.Ordinal) &&
                    string.Equals(
                        GetMemberSignature(candidate.Id),
                        signature,
                        StringComparison.Ordinal) &&
                    HasInheritableContent(candidate.Element))
                .Take(2)
                .ToArray();

            if (candidates.Length == 1)
                return candidates[0].Element;

            return null;
        }

        private static string? GetMemberSignature(string id)
        {
            var parameterList = id.IndexOf('(');
            var memberHead = parameterList >= 0
                ? id.Substring(0, parameterList)
                : id;
            var separator = memberHead.LastIndexOf('.');

            return separator < 0
                ? null
                : id.Substring(separator + 1);
        }

        private static bool HasInheritableContent(XElement element) =>
            element.Elements().Any(child =>
                !string.Equals(
                    child.Name.LocalName,
                    "inheritdoc",
                    StringComparison.Ordinal));

        public static void MergeInheritedContent(XElement into, XElement from)
        {
            // Fill empty nodes only (don't override author-provided text)
            CopyIfMissing(into, "summary", from);
            CopyIfMissing(into, "remarks", from);
            CopyIfMissing(into, "returns", from);
            CopyIfMissing(into, "value", from);

            // Param-wise copy
            var intoParams = into.Elements("param")
                                 .ToDictionary(p => (string?)p.Attribute("name") ?? "", StringComparer.Ordinal);
            foreach (var p in from.Elements("param"))
            {
                var name = (string?)p.Attribute("name") ?? "";
                if (!intoParams.ContainsKey(name))
                    into.Add(new XElement(p));
            }

            var intoTypeParams = into.Elements("typeparam")
                .ToDictionary(
                    p => (string?)p.Attribute("name") ?? "",
                    StringComparer.Ordinal);
            foreach (var typeParam in from.Elements("typeparam"))
            {
                var name = (string?)typeParam.Attribute("name") ?? "";
                if (!intoTypeParams.ContainsKey(name))
                    into.Add(new XElement(typeParam));
            }

            // Exceptions, seealso, examples – append if not present
            var fromExceptions = from.Elements("exception");
            if (!into.Elements("exception").Any() && fromExceptions.Any())
                into.Add(fromExceptions.Select(exception => new XElement(exception)));

            var fromSeeAlsos = from.Elements("seealso");
            if (!into.Elements("seealso").Any() && fromSeeAlsos.Any())
                into.Add(fromSeeAlsos.Select(seeAlso => new XElement(seeAlso)));

            var fromExamples = from.Elements("example");
            if (!into.Elements("example").Any() && fromExamples.Any())
                into.Add(fromExamples.Select(example => new XElement(example)));
        }

        private static void CopyIfMissing(XElement into, string name, XElement from)
        {
            if (into.Element(name) == null && from.Element(name) != null)
                into.Add(new XElement(from.Element(name)!));
        }
    }
}
