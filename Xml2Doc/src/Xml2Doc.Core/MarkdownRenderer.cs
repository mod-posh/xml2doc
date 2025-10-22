using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xml2Doc.Core.Models;

namespace Xml2Doc.Core
{
    /// <summary>
    /// Renders an <see cref="Xml2Doc.Core.Models.Xml2Doc"/> model into Markdown files.
    /// </summary>
    public sealed class MarkdownRenderer
    {
        /// <summary>
        /// The loaded XML documentation model to render.
        /// </summary>
        private readonly Models.Xml2Doc _model;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownRenderer"/> class.
        /// </summary>
        /// <param name="model">The XML documentation model to render.</param>
        public MarkdownRenderer(Models.Xml2Doc model) => _model = model;

        /// <summary>
        /// Renders all types from the model into Markdown files within the specified output directory.
        /// </summary>
        /// <param name="outDir">The output directory. It is created if it does not exist.</param>
        /// <remarks>
        /// Produces one <c>.md</c> file per type and an <c>index.md</c> table of contents.
        /// Existing files with the same names will be overwritten.
        /// </remarks>
        public void RenderToDirectory(string outDir)
        {
            Directory.CreateDirectory(outDir);

            var types = _model.Members.Values.Where(m => m.Kind == "T").ToList();
            foreach (var t in types)
            {
                var file = Path.Combine(outDir, FileNameFor(t.Id));
                File.WriteAllText(file, RenderType(t));
            }

            var toc = new StringBuilder();
            toc.AppendLine("# API Reference");
            foreach (var t in types.OrderBy(t => t.Id))
                toc.AppendLine($"- [{DisplayName(t.Id)}]({FileNameFor(t.Id)})");

            File.WriteAllText(Path.Combine(outDir, "index.md"), toc.ToString());
        }

        /// <summary>
        /// Renders a single type and its members to a Markdown document.
        /// </summary>
        /// <param name="type">The type member (<c>T:</c> entry) to render.</param>
        /// <returns>The Markdown content for the specified type.</returns>
        private string RenderType(XMember type)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {DisplayName(type.Id)}");
            sb.AppendLine();

            var summary = NormalizeXmlToMarkdown(type.Element.Element("summary"));
            if (!string.IsNullOrWhiteSpace(summary))
            {
                sb.AppendLine(summary);
                sb.AppendLine();
            }

            var members = _model.Members.Values
                .Where(m => m.Kind is "M" or "P" or "F" or "E")
                .Where(m => m.Id.StartsWith(type.Id + ".", StringComparison.Ordinal))
                .OrderBy(m => m.Id)
                .ToList();

            foreach (var m in members)
            {
                sb.AppendLine($"## {MemberHeader(m)}");
                var ms = NormalizeXmlToMarkdown(m.Element.Element("summary"));
                if (!string.IsNullOrWhiteSpace(ms)) sb.AppendLine(ms);

                var ps = m.Element.Elements("param").ToList();
                if (ps.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("**Parameters**");
                    foreach (var p in ps)
                    {
                        var name = (string?)p.Attribute("name") ?? "";
                        var text = NormalizeXmlToMarkdown(p);
                        sb.AppendLine($"- `{name}` — {text}");
                    }
                }

                var ret = m.Element.Element("returns");
                if (ret != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("**Returns**");
                    sb.AppendLine();
                    sb.AppendLine(NormalizeXmlToMarkdown(ret));
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds a concise header for a member, e.g., <c>M: Method(…)</c>.
        /// </summary>
        /// <param name="m">The member to summarize.</param>
        /// <returns>A short header containing the kind prefix and simplified signature.</returns>
        private static string MemberHeader(XMember m)
        {
            var simple = m.Id[(m.Id.LastIndexOf('.') + 1)..];
            var name = Regex.Replace(simple, "\\(.*\\)", "(…)");
            return $"{m.Kind}: {name}";
        }

        /// <summary>
        /// Generates a file name for a type ID suitable for use on disk.
        /// </summary>
        /// <param name="typeId">The type documentation ID (without the <c>T:</c> prefix).</param>
        /// <returns>The markdown file name. Generic angle brackets are replaced with square brackets.</returns>
        private static string FileNameFor(string typeId)
            => typeId.Replace('<', '[').Replace('>', ']') + ".md";

        /// <summary>
        /// Computes a display name for a documentation ID.
        /// </summary>
        /// <param name="id">The documentation ID.</param>
        /// <returns>The display text. Currently returns the ID as-is.</returns>
        private static string DisplayName(string id) => id;

        /// <summary>
        /// Converts a documentation ID to a Markdown anchor.
        /// </summary>
        /// <param name="id">The documentation ID.</param>
        /// <returns>A lowercase anchor string.</returns>
        private static string IdToAnchor(string id) => id.ToLowerInvariant();

        /// <summary>
        /// Converts XML documentation nodes to Markdown text.
        /// </summary>
        /// <param name="element">The XML element to normalize (e.g., <c>summary</c>, <c>returns</c>, or <c>param</c>).</param>
        /// <returns>The normalized Markdown text, or an empty string if <paramref name="element"/> is <see langword="null"/>.</returns>
        /// <remarks>
        /// Supports <c>&lt;see cref="..." /&gt;</c> links (to types and members) and <c>&lt;paramref name="..." /&gt;</c>.
        /// Collapses whitespace and trims trailing/leading spaces.
        /// </remarks>
        private string NormalizeXmlToMarkdown(XElement? element)
        {
            if (element is null) return string.Empty;
            var text = new StringBuilder();

            foreach (var node in element.Nodes())
            {
                switch (node)
                {
                    case XText t:
                        text.Append(t.Value);
                        break;

                    case XElement e when e.Name.LocalName == "see":
                        var cref = (string?)e.Attribute("cref");
                        if (!string.IsNullOrWhiteSpace(cref))
                        {
                            var kind = cref!.Split(':')[0];
                            var id = cref!.Split(':')[1];
                            if (kind == "T")
                            {
                                text.Append($"[{id}]({FileNameFor(id)})");
                            }
                            else
                            {
                                var typeId = id.Split('.')[0];
                                text.Append($"[{id}]({FileNameFor(typeId)}#{IdToAnchor(id)})");
                            }
                        }
                        else text.Append(e.Value);
                        break;

                    case XElement e when e.Name.LocalName == "paramref":
                        var name = (string?)e.Attribute("name") ?? "";
                        text.Append($"`{name}`");
                        break;

                    case XElement e:
                        text.Append(e.Value);
                        break;

                    default:
                        text.Append(node.ToString());
                        break;
                }
            }

            return Regex.Replace(text.ToString().Trim(), "\\s+", " ").Replace(" .", ".");
        }
    }
}
