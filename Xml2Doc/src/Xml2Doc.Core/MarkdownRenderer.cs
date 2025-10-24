using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xml2Doc.Core.Models;

namespace Xml2Doc.Core;

/// <summary>
/// Renders a parsed XML documentation model to Markdown files.
/// </summary>
/// <remarks>
/// - Use <see cref="RenderToDirectory(string)"/> to emit one file per type plus an index.
/// - Use <see cref="RenderToSingleFile(string)"/> to generate a single consolidated Markdown file.
/// Rendering is influenced by <see cref="RendererOptions"/> (filename style, code block language, and display trimming).
/// </remarks>
/// <seealso cref="RendererOptions"/>
/// <seealso cref="FileNameMode"/>
public sealed class MarkdownRenderer
{
    private readonly Models.Xml2Doc _model;
    private readonly RendererOptions _opt;

    /// <summary>
    /// Initializes a new instance of <see cref="MarkdownRenderer"/>.
    /// </summary>
    /// <param name="model">The XML documentation model to render.</param>
    /// <param name="options">
    /// Optional rendering options. If <see langword="null"/>, defaults are used
    /// (e.g., <see cref="FileNameMode.Verbatim"/>, language <c>csharp</c>).
    /// </param>
    public MarkdownRenderer(Models.Xml2Doc model, RendererOptions? options = null)
    {
        _model = model;
        _opt = options ?? new RendererOptions();
    }

    // === Public APIs ===

    /// <summary>
    /// Renders all types to individual Markdown files in the specified directory and writes an <c>index.md</c>.
    /// </summary>
    /// <param name="outDir">The output directory. It is created if it does not exist.</param>
    /// <remarks>
    /// Existing files with the same names are overwritten.
    /// </remarks>
    public void RenderToDirectory(string outDir)
    {
        Directory.CreateDirectory(outDir);

        var types = GetTypes().ToList();
        foreach (var t in types)
        {
            var file = Path.Combine(outDir, FileNameFor(t.Id, _opt.FileNameMode));
            File.WriteAllText(file, RenderType(t));
        }

        File.WriteAllText(Path.Combine(outDir, "index.md"), RenderIndex(types));
    }

    /// <summary>
    /// Renders all types to a single Markdown file that includes an index followed by each type section.
    /// </summary>
    /// <param name="outPath">The output file path. The containing directory is created if necessary.</param>
    public void RenderToSingleFile(string outPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        var sb = new StringBuilder();
        var types = GetTypes().ToList();

        sb.Append(RenderIndex(types));
        sb.AppendLine();

        foreach (var t in types.OrderBy(t => t.Id))
        {
            sb.Append(RenderType(t));
            sb.AppendLine();
        }

        File.WriteAllText(outPath, sb.ToString());
    }

    // === Core rendering ===

    /// <summary>
    /// Gets all documented types (<c>T:</c> members) from the model.
    /// </summary>
    private IEnumerable<XMember> GetTypes() =>
        _model.Members.Values.Where(m => m.Kind == "T");

    /// <summary>
    /// Builds the table of contents for the provided types.
    /// </summary>
    /// <param name="types">The set of types to include in the index.</param>
    /// <returns>Markdown content for the index page.</returns>
    private string RenderIndex(IEnumerable<XMember> types)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# API Reference");
        foreach (var t in types.OrderBy(t => t.Id))
        {
            var shortName = ShortTypeDisplay(t.Id);
            sb.AppendLine($"- [{shortName}]({FileNameFor(t.Id, _opt.FileNameMode)})");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Renders a single type section including summary, remarks, examples, see-also, and its members.
    /// </summary>
    /// <param name="type">The type (<c>T:</c> entry) to render.</param>
    /// <returns>Markdown content for the specified type.</returns>
    private string RenderType(XMember type)
    {
        var sb = new StringBuilder();

        var typeDisplay = ShortTypeDisplay(type.Id);
        sb.AppendLine($"# {typeDisplay}");
        sb.AppendLine();

        // <summary>
        var summary = NormalizeXmlToMarkdown(type.Element.Element("summary"));
        if (!string.IsNullOrWhiteSpace(summary))
        {
            sb.AppendLine(summary);
            sb.AppendLine();
        }

        // <remarks>
        var remarks = NormalizeXmlToMarkdown(type.Element.Element("remarks"));
        if (!string.IsNullOrWhiteSpace(remarks))
        {
            sb.AppendLine("**Remarks**");
            sb.AppendLine();
            sb.AppendLine(remarks);
            sb.AppendLine();
        }

        // <example>
        foreach (var ex in type.Element.Elements("example"))
        {
            var exText = NormalizeXmlToMarkdown(ex, preferCodeBlocks: true);
            if (!string.IsNullOrWhiteSpace(exText))
            {
                sb.AppendLine("**Example**");
                sb.AppendLine();
                sb.AppendLine(exText);
                sb.AppendLine();
            }
        }

        // <seealso>
        var seeAlsos = type.Element.Elements("seealso").ToList();
        if (seeAlsos.Count > 0)
        {
            sb.AppendLine("**See also**");
            foreach (var sa in seeAlsos)
            {
                var link = SeeAlsoToMarkdown(sa);
                if (!string.IsNullOrWhiteSpace(link))
                    sb.AppendLine($"- {link}");
            }
            sb.AppendLine();
        }

        // members
        var members = _model.Members.Values
            .Where(m => m.Kind is "M" or "P" or "F" or "E")
            .Where(m => m.Id.StartsWith(type.Id + ".", StringComparison.Ordinal))
            .OrderBy(m => m.Id)
            .ToList();

        foreach (var m in members)
        {
            sb.AppendLine($"## {MemberHeader(m)}");

            // summary
            var ms = NormalizeXmlToMarkdown(m.Element.Element("summary"));
            if (!string.IsNullOrWhiteSpace(ms))
            {
                sb.AppendLine(ms);
            }

            // params
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

            // returns
            var ret = m.Element.Element("returns");
            if (ret != null)
            {
                sb.AppendLine();
                sb.AppendLine("**Returns**");
                sb.AppendLine();
                sb.AppendLine(NormalizeXmlToMarkdown(ret));
            }

            // exceptions
            var exTags = m.Element.Elements("exception").ToList();
            if (exTags.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("**Exceptions**");
                foreach (var e in exTags)
                {
                    var cref = (string?)e.Attribute("cref");
                    var desc = NormalizeXmlToMarkdown(e);
                    var link = CrefToMarkdown(cref, displayFallback: ShortenTypeName(cref ?? string.Empty));
                    sb.AppendLine($"- {link} — {desc}");
                }
            }

            // examples
            var examples = m.Element.Elements("example").ToList();
            if (examples.Count > 0)
            {
                sb.AppendLine();
                foreach (var ex in examples)
                {
                    var exMd = NormalizeXmlToMarkdown(ex, preferCodeBlocks: true);
                    if (!string.IsNullOrWhiteSpace(exMd))
                    {
                        sb.AppendLine("**Example**");
                        sb.AppendLine();
                        sb.AppendLine(exMd);
                    }
                }
            }

            // seealso
            var memberSeeAlsos = m.Element.Elements("seealso").ToList();
            if (memberSeeAlsos.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("**See also**");
                foreach (var sa in memberSeeAlsos)
                {
                    var link = SeeAlsoToMarkdown(sa);
                    if (!string.IsNullOrWhiteSpace(link))
                        sb.AppendLine($"- {link}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    // === Display helpers ===

    /// <summary>
    /// Builds a concise header for a member (e.g., <c>Method: Foo(int, string)</c>), simplifying type names and generics.
    /// </summary>
    /// <param name="m">The member to summarize.</param>
    /// <returns>A short header containing the member kind and simplified signature.</returns>
    private string MemberHeader(XMember m)
    {
        // Show "Method: Foo(int, string)" with short type names
        var namePart = m.Id[(m.Id.LastIndexOf('.') + 1)..]; // e.g. MethodName(System.Int32,System.String)
        var name = namePart;

        // Replace parameter type list with simplified names
        name = Regex.Replace(name, @"\((.*)\)", match =>
        {
            var inner = match.Groups[1].Value;
            if (string.IsNullOrEmpty(inner)) return "()";
            var parts = inner.Split(',', StringSplitOptions.TrimEntries);
            var simplified = parts.Select(ShortenSignatureType).ToArray();
            return $"({string.Join(", ", simplified)})";
        });

        // Pretty generics in the simple name: ``2 -> &lt;T1,T2&gt;, ``1 -> &lt;T1&gt;
        name = Regex.Replace(name, @"``(\d+)", m =>
        {
            var n = int.Parse(m.Groups[1].Value);
            return $"<{string.Join(",", Enumerable.Range(1, n).Select(i => $"T{i}"))}>";
        });

        return $"{KindToWord(m.Kind)}: {name}";
    }

    /// <summary>
    /// Converts a documentation kind letter to a readable word.
    /// </summary>
    /// <param name="kind">The kind prefix (e.g., <c>M</c>, <c>P</c>, <c>F</c>, <c>E</c>, <c>T</c>).</param>
    private static string KindToWord(string kind) => kind switch
    {
        "M" => "Method",
        "P" => "Property",
        "F" => "Field",
        "E" => "Event",
        "T" => "Type",
        _ => kind
    };

    /// <summary>
    /// Produces a short display name for a type ID, optionally trimming a root namespace and formatting generic arity as <c>&lt;T1,T2&gt;</c>.
    /// </summary>
    /// <param name="typeId">The type documentation ID (portion after the <c>T:</c> prefix).</param>
    private string ShortTypeDisplay(string typeId)
    {
        // Optionally trim root namespace prefix for display
        var id = typeId;
        if (!string.IsNullOrEmpty(_opt.RootNamespaceToTrim) &&
            id.StartsWith(_opt.RootNamespaceToTrim + ".", StringComparison.Ordinal))
        {
            id = id.Substring(_opt.RootNamespaceToTrim.Length + 1);
        }

        var simple = id[(id.LastIndexOf('.') + 1)..];

        // Type`2 -> Type<T1,T2>
        simple = Regex.Replace(simple, @"`(\d+)", m =>
        {
            var n = int.Parse(m.Groups[1].Value);
            return $"<{string.Join(",", Enumerable.Range(1, n).Select(i => $"T{i}"))}>";
        });

        return simple;
    }

    /// <summary>
    /// Built-in mappings for fully-qualified BCL types and their C# aliases.
    /// </summary>
    private static readonly (string Full, string Alias)[] Aliases = new[]
    {
        ("System.String","string"), ("System.Int32","int"), ("System.Boolean","bool"),
        ("System.Object","object"), ("System.Void","void"), ("System.Int64","long"),
        ("System.Int16","short"), ("System.Byte","byte"), ("System.SByte","sbyte"),
        ("System.UInt32","uint"), ("System.UInt64","ulong"), ("System.UInt16","ushort"),
        ("System.Char","char"), ("System.Decimal","decimal"),
        ("System.Double","double"), ("System.Single","float")
    };

    /// <summary>
    /// Replaces fully-qualified type names and common framework type names with their C# aliases.
    /// </summary>
    /// <param name="s">The input type string.</param>
    /// <returns>The aliased form (e.g., <c>System.String</c> becomes <c>string</c>).</returns>
    private static string ApplyAliases(string s)
    {
        foreach (var (full, alias) in Aliases)
            s = s.Replace(full, alias);
        // Also replace common short names
        s = s.Replace("String", "string")
             .Replace("Int32", "int")
             .Replace("Boolean", "bool")
             .Replace("Object", "object")
             .Replace("Void", "void")
             .Replace("Int64", "long")
             .Replace("Int16", "short")
             .Replace("Byte", "byte")
             .Replace("SByte", "sbyte")
             .Replace("UInt32", "uint")
             .Replace("UInt64", "ulong")
             .Replace("UInt16", "ushort")
             .Replace("Char", "char")
             .Replace("Decimal", "decimal")
             .Replace("Double", "double")
             .Replace("Single", "float");
        return s;
    }

    /// <summary>
    /// Shortens a fully-qualified type used in a signature to a compact display form.
    /// </summary>
    /// <param name="full">The full type representation, e.g., <c>System.Collections.Generic.List{System.String}</c>.</param>
    /// <returns>A simplified representation, e.g., <c>List&lt;string&gt;</c>.</returns>
    private static string ShortenSignatureType(string full)
    {
        // e.g. System.Collections.Generic.List{System.String} -> List<string>
        var s = full.Trim();
        s = s.Replace('{', '<').Replace('}', '>');
        // alias fully qualified types first
        s = ApplyAliases(s);
        // after aliasing, keep only final segment for generic/non-generic types
        s = s.Split('.').Last();
        // ``2 -> <T1,T2>, ``1 -> <T1> (signature generic parameters shown as <T>)
        s = Regex.Replace(s, @"`\d+", m => "<T>");
        return s;
    }

    // === Links & filenames ===

    /// <summary>
    /// Converts a <c>cref</c> to a Markdown link, resolving types and members to local files/anchors.
    /// </summary>
    /// <param name="cref">The cref value (e.g., <c>T:Namespace.Type</c>, <c>M:Namespace.Type.Method</c>).</param>
    /// <param name="displayFallback">Optional display text if the cref cannot be resolved.</param>
    /// <returns>A Markdown link, or the fallback/display text if unavailable.</returns>
    private string CrefToMarkdown(string? cref, string? displayFallback = null)
    {
        if (string.IsNullOrWhiteSpace(cref)) return displayFallback ?? string.Empty;
        var kind = cref!.Split(':')[0];
        var id = cref!.Split(':')[1];

        if (kind == "T")
        {
            return $"[{ShortenTypeName(cref)}]({FileNameFor(id, _opt.FileNameMode)})";
        }
        else
        {
            var typeId = id.Split('.')[0];
            return $"[{displayFallback ?? id}]({FileNameFor(typeId, _opt.FileNameMode)}#{IdToAnchor(id)})";
        }
    }

    /// <summary>
    /// Generates a Markdown file name for a type ID based on the chosen <see cref="FileNameMode"/>.
    /// </summary>
    /// <param name="typeId">The type ID (portion after the kind prefix).</param>
    /// <param name="mode">The file name generation mode.</param>
    /// <returns>A file-system-friendly name ending with <c>.md</c>.</returns>
    private static string FileNameFor(string typeId, FileNameMode mode)
    {
        var name = typeId;

        if (mode == FileNameMode.CleanGenerics)
        {
            // Strip generic arity: Namespace.Type`1 -> Namespace.Type
            name = Regex.Replace(name, @"`\d+", "");
            // Replace signature braces for generic type params
            name = name.Replace('{', '<').Replace('}', '>');
        }

        // Make filesystem friendly
        name = name.Replace('<', '[').Replace('>', ']');

        return name + ".md";
    }

    /// <summary>
    /// Converts a documentation ID into a Markdown anchor.
    /// </summary>
    /// <param name="id">The documentation ID (portion after the kind prefix).</param>
    private static string IdToAnchor(string id) => id.ToLowerInvariant();

    /// <summary>
    /// Converts a <c>&lt;seealso&gt;</c> element into Markdown.
    /// </summary>
    /// <param name="sa">The <c>seealso</c> element.</param>
    /// <returns>A Markdown link or normalized text.</returns>
    private string SeeAlsoToMarkdown(XElement sa)
    {
        var cref = (string?)sa.Attribute("cref");
        if (!string.IsNullOrWhiteSpace(cref))
            return CrefToMarkdown(cref, displayFallback: ShortenTypeName(cref));
        var href = (string?)sa.Attribute("href");
        if (!string.IsNullOrWhiteSpace(href))
            return $"[{sa.Value}]({href})";
        return NormalizeXmlToMarkdown(sa);
    }

    /// <summary>
    /// Produces a short label from a <c>cref</c> for display purposes (e.g., replaces arity and aliases BCL types).
    /// </summary>
    /// <param name="cref">The cref value, e.g., <c>T:Namespace.Type`2</c> or <c>M:Namespace.Type.Method(System.String)</c>.</param>
    /// <returns>A simplified display name.</returns>
    private string ShortenTypeName(string cref)
    {
        var id = cref.Contains(':') ? cref.Split(':', 2)[1] : cref;
        var last = id.Split('.').LastOrDefault() ?? id;
        // generic arity -> <T1,...>
        last = Regex.Replace(last, @"`(\d+)", m =>
        {
            var n = int.Parse(m.Groups[1].Value);
            return $"<{string.Join(",", Enumerable.Range(1, n).Select(i => $"T{i}"))}>";
        });
        last = last.Replace('{', '<').Replace('}', '>');
        last = ApplyAliases(last);
        return last;
    }

    // === XML → Markdown normalization ===

    /// <summary>
    /// Normalizes XML documentation nodes to Markdown.
    /// </summary>
    /// <param name="element">The XML element to normalize (e.g., <c>summary</c>, <c>remarks</c>, <c>returns</c>, <c>param</c>, <c>example</c>).</param>
    /// <param name="preferCodeBlocks">
    /// If <see langword="true"/>, prefers fenced code blocks for code samples (e.g., within <c>example</c> or <c>code</c> elements).
    /// </param>
    /// <returns>The normalized Markdown text, or an empty string if <paramref name="element"/> is <see langword="null"/>.</returns>
    /// <remarks>
    /// Supported tags include:
    /// - <c>&lt;see cref="..." /&gt;</c> and <c>&lt;see href="..."&gt;text&lt;/see&gt;</c>
    /// - <c>&lt;paramref name="..." /&gt;</c>
    /// - <c>&lt;para&gt;</c> (emits paragraph breaks)
    /// - <c>&lt;c&gt;</c> and <c>&lt;code&gt;</c> (inline code or fenced blocks; language from <see cref="RendererOptions.CodeBlockLanguage"/>)
    /// - <c>&lt;example&gt;</c> (detects code and renders as fenced blocks when possible)
    /// Whitespace is collapsed and trimmed.
    /// </remarks>
    private string NormalizeXmlToMarkdown(XElement? element, bool preferCodeBlocks = false)
    {
        if (element is null) return string.Empty;

        if (preferCodeBlocks && element.Name.LocalName == "example")
        {
            var codeNode = element.Descendants().FirstOrDefault(x => x.Name.LocalName is "code" or "c");
            if (codeNode != null)
            {
                var code = codeNode.Value.Trim('\r', '\n');
                return $"```{_opt.CodeBlockLanguage}\n{code}\n```";
            }
        }

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
                        text.Append(CrefToMarkdown(cref, displayFallback: ShortenTypeName(cref)));
                    }
                    else
                    {
                        var href = (string?)e.Attribute("href");
                        if (!string.IsNullOrWhiteSpace(href))
                            text.Append($"[{e.Value}]({href})");
                        else
                            text.Append(e.Value);
                    }
                    break;

                case XElement e when e.Name.LocalName == "paramref":
                    var name = (string?)e.Attribute("name") ?? "";
                    text.Append($"`{name}`");
                    break;

                case XElement e when e.Name.LocalName == "para":
                    text.AppendLine().AppendLine(NormalizeXmlToMarkdown(e)).AppendLine();
                    break;

                case XElement e when e.Name.LocalName is "c" or "code":
                    var code = e.Value;
                    if (preferCodeBlocks || code.Contains('\n'))
                        text.AppendLine().AppendLine($"```{_opt.CodeBlockLanguage}").AppendLine(code.Trim('\r', '\n')).AppendLine("```");
                    else
                        text.Append($"`{code}`");
                    break;

                default:
                    if (node is XElement xe)
                        text.Append(xe.Value);
                    break;
            }
        }

        var result = Regex.Replace(text.ToString().Trim(), "\\s+", " ").Replace(" .", ".");
        return result;
    }
}
