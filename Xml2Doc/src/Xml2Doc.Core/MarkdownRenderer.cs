using System;
using System.Collections.Concurrent;
using System.Reflection.Emit;
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
/// - Overloaded methods are grouped under a single header with each overload listed as a bullet.
/// - <c>&lt;inheritdoc&gt;</c> is resolved and merged via <see cref="InheritDocResolver"/> before rendering.
/// <para>
/// Rendering is influenced by <see cref="RendererOptions"/> (filename style, code block language, and optional root namespace trimming).
/// </para>
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
    /// Filenames are produced according to <see cref="RendererOptions.FileNameMode"/>.
    /// </remarks>
    /// <exception cref="IOException">An I/O error occurs while writing files.</exception>
    /// <exception cref="UnauthorizedAccessException">Caller does not have the required permission.</exception>
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
    /// <exception cref="IOException">An I/O error occurs while writing the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Caller does not have the required permission.</exception>
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
    /// <returns>An enumeration of members whose kind is <c>"T"</c> (types).</returns>
    private IEnumerable<XMember> GetTypes() =>
        _model.Members.Values.Where(m => m.Kind == "T");

    /// <summary>
    /// Builds the table of contents for the provided types.
    /// </summary>
    /// <param name="types">The set of types to include in the index.</param>
    /// <returns>Markdown content for the index page.</returns>
    /// <remarks>
    /// Uses <see cref="ShortTypeDisplay(string)"/> for display and <see cref="FileNameFor(string, FileNameMode)"/> for links.
    /// </remarks>
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
    /// <remarks>
    /// Members are grouped by simple name; method overloads are listed together under one heading.
    /// </remarks>
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

        // members (grouped by simple name for overloads)
        var members = _model.Members.Values
            .Where(m => m.Kind is "M" or "P" or "F" or "E")
            .Where(m => m.Id.StartsWith(type.Id + ".", StringComparison.Ordinal))
            .OrderBy(m => m.Id)
            .ToList();

        // Group by the simple member name (dot before '('), not the last dot in the whole ID.
        static string GroupKey(XMember mm)
        {
            var id = mm.Id; // e.g., "M:Xml2Doc.Sample.Mathx.Add(System.Int32,System.Int32)"
            var parenIdx = id.IndexOf('(');
            var cut = parenIdx >= 0 ? id.LastIndexOf('.', parenIdx) : id.LastIndexOf('.');
            var nameAndParams = cut >= 0 ? id.Substring(cut + 1) : id; // "Add(System.Int32, ...)"
            var nameOnly = nameAndParams.Split('(')[0];                // "Add"
            
            // pretty generic method arity: ``2 -> <T1,T2>
            nameOnly = Regex.Replace(nameOnly, @"``(\d+)", m =>
            {
                var n = int.Parse(m.Groups[1].Value);
                return $"<{string.Join(",", Enumerable.Range(1, n).Select(i => $"T{i}"))}>";
            });
            return nameOnly;
        }
        
        var groups = members
                    .GroupBy(GroupKey)
                    .OrderBy(g => g.Key)
                    .ToList();

        foreach (var g in groups)
        {
            if (g.First().Kind == "M" && g.Count() > 1)
            {
                // one header, list each overload as a bullet
                sb.AppendLine($"## Method: {g.Key}");
                foreach (var mem in g)
                    RenderMember(mem, sb, asOverload: true);
                sb.AppendLine();
            }
            else
            {
                // single member as a full section
                RenderMember(g.First(), sb, asOverload: false);
            }
        }

        return sb.ToString();
    }

    // === Display helpers ===

    /// <summary>
    /// Builds a concise header for a member (e.g., <c>Method: Foo(int, string)</c>), simplifying type names and generics.
    /// </summary>
    /// <param name="m">The member to summarize.</param>
    /// <returns>A short header containing the member kind and simplified signature.</returns>
    /// <remarks>
    /// Handles brace-aware parameter splitting (XML-doc generics use <c>{}</c>) and formats generic arity (e.g., <c>``2</c> → <c>&lt;T1,T2&gt;</c>).
    /// </remarks>
    private string MemberHeader(XMember m)
    {
        // Example m.Id: "M:Xml2Doc.Sample.Mathx.Add(System.Int32,System.Int32)"
        var id = m.Id;

        // 1) Find the dot before the method/property name (last dot BEFORE '(')
        var parenIdx = id.IndexOf('(');
        var cut = parenIdx >= 0 ? id.LastIndexOf('.', parenIdx) : id.LastIndexOf('.');
        var namePart = cut >= 0 ? id.Substring(cut + 1) : id; // "Add(System.Int32,System.Int32)"

        // 2) Replace parameter list with simplified names (brace-aware split)
        namePart = Regex.Replace(namePart, @"\((.*)\)", match =>
        {
            var inner = match.Groups[1].Value;
            if (string.IsNullOrWhiteSpace(inner)) return "()";

            static IEnumerable<string> SplitParams(string s)
            {
                var depth = 0; var start = 0;
                for (int i = 0; i < s.Length; i++)
                {
                    var ch = s[i];
                    if (ch == '{') depth++;
                    else if (ch == '}') depth--;
                    else if (ch == ',' && depth == 0)
                    {
                        yield return s.Substring(start, i - start);
                        start = i + 1;
                    }
                }
                yield return s.Substring(start);
            }

            var parts = SplitParams(inner).Select(p => p.Trim());
            var simplified = parts.Select(ShortenSignatureType).ToArray();
            return $"({string.Join(", ", simplified)})";
        });

        // Pretty generics in the simple name: ``2 -> <T1,T2>, ``1 -> <T1>
        namePart = Regex.Replace(namePart, @"``(\d+)", m2 =>
        {
            var n = int.Parse(m2.Groups[1].Value);
            return $"<{string.Join(",", Enumerable.Range(1, n).Select(i => $"T{i}"))}>";
        });

        return $"{KindToWord(m.Kind)}: {namePart}";
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
    /// <returns>The simple type name with generic arity displayed, and the root namespace removed if configured.</returns>
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
    /// <remarks>
    /// Member anchors are generated via <see cref="IdToAnchor(string)"/> and use the lowercase documentation ID.
    /// </remarks>
    private string CrefToMarkdown(string? cref, string? displayFallback = null)
    {
        if (string.IsNullOrWhiteSpace(cref)) return displayFallback ?? string.Empty;
        var kind = cref!.Split(':')[0];
        var id = cref!.Split(':')[1];

        if (kind == "T")
        {
            // Use type display (handles arity, trimming, aliases)
            return $"[{ShortTypeDisplay(id)}]({FileNameFor(id, _opt.FileNameMode)})";
        }
        else
        {
            // Member: show a friendly label (MethodName(params)), link to the type file + anchor
            var typeId = id.Split('.')[0];
            var label = displayFallback ?? ShortLabelFromCref(cref);
            return $"[{label}]({FileNameFor(typeId, _opt.FileNameMode)}#{IdToAnchor(id)})";
        }
    }

    /// <summary>
    /// Generates a Markdown file name for a type ID based on the chosen <see cref="FileNameMode"/>.
    /// </summary>
    /// <param name="typeId">The type ID (portion after the kind prefix).</param>
    /// <param name="mode">The file name generation mode.</param>
    /// <returns>A file-system-friendly name ending with <c>.md</c>.</returns>
    /// <remarks>
    /// In <see cref="FileNameMode.CleanGenerics"/> the generic arity (e.g., <c>`1</c>) is removed and generic braces are normalized.
    /// Angle brackets are replaced with square brackets to avoid filesystem issues.
    /// </remarks>
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
    /// <returns>Lowercase anchor text that can be referenced in links.</returns>
    private static string IdToAnchor(string id) => id.ToLowerInvariant();

    /// <summary>
    /// Converts a <c>&lt;seealso&gt;</c> element into Markdown.
    /// </summary>
    /// <param name="sa">The <c>seealso</c> element.</param>
    /// <returns>A Markdown link or normalized text.</returns>
    /// <remarks>
    /// Supports <c>cref</c> to local API links and <c>href</c> for external URLs.
    /// </remarks>
    private string SeeAlsoToMarkdown(XElement sa)
    {
        var cref = (string?)sa.Attribute("cref");
        if (!string.IsNullOrWhiteSpace(cref))
            return CrefToMarkdown(cref, displayFallback: ShortLabelFromCref(cref));
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

    /// <summary>
    /// Creates a short, human-friendly label from a cref string.
    /// </summary>
    /// <param name="cref">
    /// A cref such as <c>T:Namespace.Type</c> or <c>M:Namespace.Type.Method(Type,Type)</c>.
    /// </param>
    /// <returns>
    /// For types, the short type display (with generic arity formatted). For methods, the method name with
    /// simplified parameter types. For other kinds, the simple member identifier.
    /// </returns>
    /// <remarks>
    /// Handles XML-doc generic braces by converting <c>{}</c> to <c>&lt;&gt;</c>, applies C# aliases, and trims namespaces
    /// from generic arguments to keep labels compact.
    /// </remarks>
    private string ShortLabelFromCref(string cref)
    {
        if (string.IsNullOrWhiteSpace(cref))
            return string.Empty;

        // Split "K:Namespace.Type.Member(...)" into (K, rest)
        var parts = cref.Split(':', 2);
        var kind = parts.Length == 2 ? parts[0] : "";
        var id = parts.Length == 2 ? parts[1] : cref;

        if (kind == "T")
        {
            // Type
            return ShortTypeDisplay(id);
        }

        if (kind == "M")
        {
            // Method: Namespace.Type.Method(Type,Type)
            var parenIdx = id.IndexOf('(');
            var cut = parenIdx >= 0 ? id.LastIndexOf('.', parenIdx) : id.LastIndexOf('.');
            var nameAndParams = cut >= 0 ? id.Substring(cut + 1) : id; // "Method(Type,Type)"
            var paren = nameAndParams.IndexOf('(');
            var methodName = paren >= 0 ? nameAndParams[..paren] : nameAndParams;
            var paramList = (paren >= 0 && nameAndParams.EndsWith(")"))
                ? nameAndParams.Substring(paren + 1, nameAndParams.Length - paren - 2) // inside (...)
                : string.Empty;

            static IEnumerable<string> SplitParams(string s)
            {
                // Handles nested generic braces in XML-doc form: IEnumerable{List{T}}
                var depth = 0; var start = 0;
                for (int i = 0; i < s.Length; i++)
                {
                    var ch = s[i];
                    if (ch == '{') depth++;
                    else if (ch == '}') depth--;
                    else if (ch == ',' && depth == 0)
                    {
                        yield return s.Substring(start, i - start);
                        start = i + 1;
                    }
                }
                if (s.Length > 0) yield return s.Substring(start);
            }

            string FormatParam(string p)
            {
                p = p.Trim();

                // XML-doc generics use {} – convert first, then alias/shorten
                p = p.Replace('{', '<').Replace('}', '>');
                p = ApplyAliases(p);

                // Optionally trim namespaces inside generic args
                if (p.Contains('<') && p.Contains('>'))
                {
                    var lt = p.IndexOf('<');
                    var gt = p.LastIndexOf('>');
                    if (lt >= 0 && gt > lt)
                    {
                        var head = p[..(lt + 1)];
                        var inner = p.Substring(lt + 1, gt - lt - 1);
                        inner = string.Join(", ", inner.Split(',').Select(x => x.Trim().Split('.').Last()));
                        var tail = p[gt..];
                        p = head + inner + tail;
                    }
                }

                // For non-generic names, just drop namespace noise
                if (!p.Contains('<'))
                    p = p.Split('.').Last();

                return p;
            }

            var formattedParams = string.IsNullOrWhiteSpace(paramList)
                ? string.Empty
                : string.Join(", ", SplitParams(paramList).Select(FormatParam));

            return string.IsNullOrEmpty(formattedParams)
                ? $"{methodName}()"
                : $"{methodName}({formattedParams})";
        }

        // Properties/Fields/Events: show simple member identifier (after last dot)
        if (id.Contains('.'))
            return id[(id.LastIndexOf('.') + 1)..];

        return id;
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
    /// Whitespace is collapsed and trimmed; stray space before punctuation is removed.
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
                        text.Append(CrefToMarkdown(cref, displayFallback: ShortLabelFromCref(cref)));
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

    /// <summary>
    /// Renders a single member section or overload list item, including summary, parameters, returns, exceptions, examples, and see-also.
    /// </summary>
    /// <param name="m">The member to render.</param>
    /// <param name="sb">The output builder to append Markdown to.</param>
    /// <param name="asOverload">
    /// If <see langword="true"/>, renders as a bullet item under an overload group; otherwise renders as a full section with a heading.
    /// </param>
    /// <remarks>
    /// If an <c>&lt;inheritdoc&gt;</c> tag is present, inherited content is resolved via <see cref="InheritDocResolver"/> and merged before rendering.
    /// </remarks>
    private void RenderMember(XMember m, StringBuilder sb, bool asOverload)
    {
        // inheritdoc: if present, merge content
        var inherit = m.Element.Element("inheritdoc");
        if (inherit != null)
        {
            var target = InheritDocResolver.ResolveInheritedMember(_model, m);
            if (target != null)
                InheritDocResolver.MergeInheritedContent(m.Element, target);
        }

        // Heading
        if (asOverload)
        {
            // overload list bullet with signature
            sb.AppendLine($"- `{MemberHeader(m)}`");
        }
        else
        {
            sb.AppendLine($"## {MemberHeader(m)}");
        }

        // summary
        var ms = NormalizeXmlToMarkdown(m.Element.Element("summary"));
        if (!string.IsNullOrWhiteSpace(ms))
            sb.AppendLine(ms);

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
                var link = CrefToMarkdown(cref, displayFallback: cref is null ? null : ShortLabelFromCref(cref));
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
}
