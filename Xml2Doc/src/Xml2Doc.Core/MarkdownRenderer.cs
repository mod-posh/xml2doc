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
    public sealed class MarkdownRenderer
    {
        private readonly Models.Xml2Doc _model;
        public MarkdownRenderer(Models.Xml2Doc model) => _model = model;

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

        private static string MemberHeader(XMember m)
        {
            var simple = m.Id[(m.Id.LastIndexOf('.') + 1)..];
            var name = Regex.Replace(simple, "\\(.*\\)", "(…)");
            return $"{m.Kind}: {name}";
        }

        private static string FileNameFor(string typeId)
            => typeId.Replace('<', '[').Replace('>', ']') + ".md";

        private static string DisplayName(string id) => id;

        private static string IdToAnchor(string id) => id.ToLowerInvariant();

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
