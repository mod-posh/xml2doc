using System;
#if NETSTANDARD2_0
using Xml2Doc.Core.Compat;
#endif
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xml2Doc.Core.Models;
using Xml2Doc.Core.Linking;

namespace Xml2Doc.Core;

/// <summary>
/// Renders a parsed XML documentation model to Markdown.
/// </summary>
/// <remarks>
/// Features:
/// <list type="bullet">
///   <item><description>Per‑type output (<see cref="RenderToDirectory(string)"/>) or single consolidated file (<see cref="RenderToSingleFile(string)"/>).</description></item>
///   <item><description>Overload grouping: method overloads share one heading; each overload signature rendered as a bullet.</description></item>
///   <item><description><c>&lt;inheritdoc&gt;</c> support via <see cref="InheritDocResolver"/> (inherited content merged before output).</description></item>
///   <item><description>Stable anchors for members (<see cref="IdToAnchor(string)"/>) and type headings (<see cref="HeadingSlug(string)"/> in single‑file mode).</description></item>
///   <item><description>Depth‑aware generic formatting (nested generics displayed compactly).</description></item>
///   <item><description>Token‑aware aliasing (BCL types mapped to C# keywords without corrupting longer identifiers).</description></item>
///   <item><description>Paragraph‑preserving normalization (retains blank lines and fenced code blocks, collapses soft wraps, trims stray spaces).</description></item>
///   <item><description>Optional root namespace trimming (headings and, when enabled, file names) via <see cref="RendererOptions.RootNamespaceToTrim"/> / <see cref="RendererOptions.TrimRootNamespaceInFileNames"/>.</description></item>
///   <item><description>Configurable filename style (<see cref="RendererOptions.FileNameMode"/>).</description></item>
///   <item><description>Optional per‑type member TOC (<see cref="RendererOptions.EmitToc"/>) inserted below each type heading (per‑type mode only).</description></item>
///   <item><description>Optional namespace index emission (<see cref="RendererOptions.EmitNamespaceIndex"/>) producing a <c>namespaces.md</c> overview plus individual namespace pages.</description></item>
/// </list>
/// Link resolution automatically adapts between per‑file and single‑file strategies (<see cref="LinkMode"/>).
/// </remarks>
/// <seealso cref="RendererOptions"/>
/// <seealso cref="FileNameMode"/>
/// <seealso cref="InheritDocResolver"/>
public sealed class MarkdownRenderer
{
    private readonly Models.Xml2Doc _model;
    private readonly RendererOptions _opt;

    /// <summary>
    /// Internal link target selection mode for cref resolution.
    /// </summary>
    private enum LinkMode { PerTypeFiles, InDocumentAnchors }
    private LinkMode _linkMode = LinkMode.PerTypeFiles;

    private readonly ILinkResolver _linkResolver;
    private bool _singleFileMode;

    /// <summary>
    /// Creates a renderer for a parsed XML documentation model.
    /// </summary>
    /// <param name="model">Loaded XML documentation model.</param>
    /// <param name="options">
    /// Optional rendering options (defaults applied when <see langword="null"/>).
    /// </param>
    public MarkdownRenderer(Models.Xml2Doc model, RendererOptions? options = null)
    {
        _model = model;
        _opt = options ?? new RendererOptions();
        _linkResolver = new DefaultLinkResolver(
            labelFromCref: ShortLabelFromCref,
            idToAnchor: IdToAnchor,
            typeFileName: TypeFileNameForResolver,
            headingSlug: HeadingSlug);
    }

    // === Public APIs ===

    /// <summary>
    /// Emits one Markdown file per documented type plus an <c>index.md</c> table of contents. Optionally emits namespace pages and a namespace index.
    /// </summary>
    /// <param name="outDir">Destination directory (created if absent).</param>
    /// <remarks>
    /// Per‑type links resolve to sibling files; member links resolve to in‑file anchors.
    /// File naming honors <see cref="RendererOptions.FileNameMode"/> and optional root namespace trimming in file names
    /// (<see cref="RendererOptions.TrimRootNamespaceInFileNames"/>).
    /// When <see cref="RendererOptions.EmitNamespaceIndex"/> is <see langword="true"/>, writes:
    /// <list type="bullet">
    ///   <item><description><c>namespaces.md</c> — overview.</description></item>
    ///   <item><description><c>namespaces/&lt;namespace&gt;.md</c> — per‑namespace type lists.</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="IOException">I/O failure while writing output files.</exception>
    /// <exception cref="UnauthorizedAccessException">Insufficient permissions.</exception>
    public void RenderToDirectory(string outDir)
    {
        var __prev = _singleFileMode;
        try
        {
            _singleFileMode = false;
            _linkMode = LinkMode.PerTypeFiles;

            Directory.CreateDirectory(outDir);
            var types = GetTypes().OrderBy(t => t.Id).ToList();
            foreach (var t in types)
            {
                var file = Path.Combine(outDir, FileNameForPerType(t.Id));
                File.WriteAllText(file, RenderType(t, includeHeader: true));
            }
            File.WriteAllText(Path.Combine(outDir, "index.md"), RenderIndex(types, useAnchors: false));

            if (_opt.EmitNamespaceIndex)
            {
                var nsMap = new Dictionary<string, List<XMember>>(StringComparer.Ordinal);
                foreach (var t in types)
                {
                    var id = t.Id;
                    var lastDot = id.LastIndexOf('.');
                    var ns = lastDot > 0 ? id.Substring(0, lastDot) : "(global)";
                    (nsMap.TryGetValue(ns, out var list) ? list : nsMap[ns] = new List<XMember>()).Add(t);
                }

                var nsDir = Path.Combine(outDir, "namespaces");
                Directory.CreateDirectory(nsDir);

                foreach (var kv in nsMap.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    var ns = kv.Key;
                    var fileSafe = ns == "(global)" ? "_global_" : ns.Replace('<', '[').Replace('>', ']').Replace('+', '.').Replace('/', '.').Replace('\\', '.');
                    var nsFile = Path.Combine(nsDir, $"{fileSafe}.md");

                    var sbNs = new StringBuilder();
                    sbNs.AppendLine($"# {ns}");
                    foreach (var t in kv.Value.OrderBy(t => t.Id, StringComparer.Ordinal))
                    {
                        var shortName = ShortTypeDisplay(t.Id);
                        var perTypeFile = FileNameFor(t.Id, _opt.FileNameMode);
                        sbNs.AppendLine($"- [{shortName}]({Path.Combine("..", perTypeFile).Replace('\\', '/')})");
                    }
                    File.WriteAllText(nsFile, sbNs.ToString());
                }

                var nsIndex = new StringBuilder();
                nsIndex.AppendLine("# Namespaces");
                foreach (var ns in nsMap.Keys.OrderBy(s => s, StringComparer.Ordinal))
                {
                    var fileSafe = ns == "(global)" ? "_global_" : ns.Replace('<', '[').Replace('>', ']').Replace('+', '.').Replace('/', '.').Replace('\\', '.');
                    nsIndex.AppendLine($"- [{ns}](namespaces/{fileSafe}.md)");
                }
                File.WriteAllText(Path.Combine(outDir, "namespaces.md"), nsIndex.ToString());
            }
        }
        finally
        {
            _singleFileMode = __prev;
        }
    }

    /// <summary>
    /// Emits a single Markdown file (index + all types and members).
    /// </summary>
    /// <param name="outPath">Output file path (directory created if missing).</param>
    /// <remarks>
    /// Type links target in‑document heading slugs; member links target explicit anchors generated from documentation IDs.
    /// </remarks>
    /// <exception cref="IOException">I/O failure while writing the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Insufficient permissions.</exception>
    public void RenderToSingleFile(string outPath)
    {
        var __prev = _singleFileMode;
        try
        {
            _singleFileMode = true;
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
            File.WriteAllText(outPath, BuildSingleFileContent());
        }
        finally
        {
            _singleFileMode = __prev;
        }
    }

    /// <summary>
    /// Returns the consolidated single‑file content (index + all types) without writing to disk.
    /// </summary>
    public string RenderToString() => BuildSingleFileContent();

    /// <summary>
    /// Builds single‑file content switching link mode temporarily to in‑document anchors.
    /// </summary>
    private string BuildSingleFileContent()
    {
        var prev = _linkMode;
        _linkMode = LinkMode.InDocumentAnchors;
        try
        {
            var types = GetTypes().OrderBy(t => t.Id).ToList();
            var sb = new StringBuilder();

            sb.Append(RenderIndex(types, useAnchors: true));
            sb.AppendLine();

            for (int i = 0; i < types.Count; i++)
            {
                var t = types[i];
                var typeDisplay = ShortTypeDisplay(t.Id);
                sb.AppendLine($"<a id=\"{HeadingSlug(typeDisplay)}\"></a>");
                sb.AppendLine($"# {typeDisplay}");
                sb.AppendLine();
                sb.Append(RenderType(t, includeHeader: false));
                if (i < types.Count - 1)
                {
                    sb.AppendLine();
                    sb.AppendLine("---");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
        finally
        {
            _linkMode = prev;
        }
    }

    // === Core rendering ===

    /// <summary>
    /// Enumerates all documented types (<c>T:</c> members).
    /// </summary>
    private IEnumerable<XMember> GetTypes() =>
        _model.Members.Values.Where(m => m.Kind == "T");

    /// <summary>
    /// Builds a type index pointing either to per‑type files or in‑document anchors.
    /// </summary>
    /// <param name="types">Types to include.</param>
    /// <param name="useAnchors"><c>true</c> for single‑file mode, otherwise per‑file links.</param>
    private string RenderIndex(IEnumerable<XMember> types, bool useAnchors = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# API Reference");
        foreach (var t in types)
        {
            var shortName = ShortTypeDisplay(t.Id);
            var link = useAnchors
                ? $"#{HeadingSlug(shortName)}"
                : FileNameForPerType(t.Id);
            sb.AppendLine($"- [{shortName}]({link})");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Renders a type page/section (summary, remarks, examples, see‑also, optional member TOC, then members grouped by overload).
    /// </summary>
    private string RenderType(XMember type, bool includeHeader = true)
    {
        var sb = new StringBuilder();
        var typeDisplay = ShortTypeDisplay(type.Id);

        if (includeHeader)
        {
            sb.AppendLine($"# {typeDisplay}");
            sb.AppendLine();
        }

        var summary = NormalizeXmlToMarkdown(type.Element.Element("summary"));
        if (!string.IsNullOrWhiteSpace(summary))
        {
            sb.AppendLine(summary);
            sb.AppendLine();
        }

        var remarks = NormalizeXmlToMarkdown(type.Element.Element("remarks"));
        if (!string.IsNullOrWhiteSpace(remarks))
        {
            sb.AppendLine("**Remarks**");
            sb.AppendLine();
            sb.AppendLine(remarks);
            sb.AppendLine();
        }

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

        var members = _model.Members.Values
            .Where(m => m.Kind is "M" or "P" or "F" or "E")
            .Where(m => m.Id.StartsWith(type.Id + ".", StringComparison.Ordinal))
            .OrderBy(m => m.Id)
            .ToList();

        if (includeHeader && _opt.EmitToc && members.Count > 0 && !_singleFileMode)
        {
            sb.AppendLine(BuildMemberToc(members));
        }

        static string GroupKey(XMember mm)
        {
            var id = mm.Id;
            var parenIdx = id.IndexOf('(');
            var cut = parenIdx >= 0 ? id.LastIndexOf('.', parenIdx) : id.LastIndexOf('.');
            var nameAndParams = cut >= 0 ? id.Substring(cut + 1) : id;

            nameAndParams = Regex.Replace(nameAndParams, @"``(\d+)", m =>
            {
                var n = int.Parse(m.Groups[1].Value);
                return $"<{string.Join(",", Enumerable.Range(1, n).Select(i => $"T{i}"))}>";
            });

            nameAndParams = ApplyAliases(nameAndParams);
            if (nameAndParams.StartsWith("System.", StringComparison.Ordinal))
                nameAndParams = nameAndParams.Substring("System.".Length);

            return nameAndParams;
        }

        var groups = members.GroupBy(GroupKey).OrderBy(g => g.Key).ToList();

        foreach (var g in groups)
        {
            if (g.First().Kind == "M" && g.Count() > 1)
            {
                sb.AppendLine($"## Method: {g.Key}");
                foreach (var mem in g)
                    RenderMember(mem, sb, asOverload: true);
                sb.AppendLine();
            }
            else
            {
                RenderMember(g.First(), sb, asOverload: false);
            }
        }

        return sb.ToString();
    }

    // === Display helpers ===

    /// <summary>
    /// Creates a GitHub‑style slug: lowercase, trimmed, spaces → dashes, strip non <c>[a-z0-9-]</c>.
    /// </summary>
    private static string HeadingSlug(string heading)
    {
        var s = heading.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"\s+", "-");
        s = Regex.Replace(s, @"[^a-z0-9\-]", "");
        return s;
    }

    /// <summary>
    /// Builds a concise member header (Kind + simplified signature) for headings and overload entries.
    /// </summary>
    private string MemberHeader(XMember m)
    {
        var id = m.Id;
        var parenIdx = id.IndexOf('(');
        var cut = parenIdx >= 0 ? id.LastIndexOf('.', parenIdx) : id.LastIndexOf('.');
        var namePart = cut >= 0 ? id.Substring(cut + 1) : id;

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

        namePart = Regex.Replace(namePart, @"``(\d+)", m2 =>
        {
            var n = int.Parse(m2.Groups[1].Value);
            return $"<{string.Join(",", Enumerable.Range(1, n).Select(i => $"T{i}"))}>";
        });

        return $"{KindToWord(m.Kind)}: {namePart}";
    }

    /// <summary>
    /// Maps XML documentation kind letter (e.g. <c>M</c>) to a readable word (e.g. <c>Method</c>).
    /// </summary>
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
    /// Produces a short display name for a type ID (generic arity → placeholders, optional root namespace trimming).
    /// </summary>
    private string ShortTypeDisplay(string typeId)
    {
        if (typeId.IndexOf('{') >= 0 || typeId.IndexOf('}') >= 0 || typeId.IndexOf('<') >= 0)
        {
            var normalized = typeId.Replace('{', '<').Replace('}', '>');
            var display = ShortenSignatureType(normalized);

            if (_opt.RootNamespaceToTrim is string root && root.Length > 0 &&
                display.StartsWith(root + ".", StringComparison.Ordinal))
            {
                display = display.Substring(root.Length + 1);
            }
            return display;
        }

        var id = typeId;
        if (_opt.RootNamespaceToTrim is string root2 && root2.Length > 0 &&
            id.StartsWith(root2 + ".", StringComparison.Ordinal))
        {
            id = id.Substring(root2.Length + 1);
        }

        var lastDot = id.LastIndexOf('.');
        var simple = lastDot >= 0 ? id.Substring(lastDot + 1) : id;

        simple = Regex.Replace(simple, @"`(\d+)", m =>
        {
            var n = int.Parse(m.Groups[1].Value);
            return $"<{string.Join(",", Enumerable.Range(1, n).Select(i => $"T{i}"))}>";
        });

        return simple;
    }

    /// <summary>
    /// Built‑in mappings from fully‑qualified BCL types to C# aliases.
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

    // Token‑aware full and short patterns (avoid partial replacements inside larger identifiers).
    private static readonly (Regex Pattern, string Alias)[] AliasFullTokenPatterns =
        Aliases.Select(a => (Pattern: new Regex($@"(?<![A-Za-z0-9_]){Regex.Escape(a.Full)}(?![A-Za-z0-9_])"), a.Alias)).ToArray();

    private static readonly (Regex Pattern, string Alias)[] AliasShortTokenPatterns =
        Aliases
            .GroupBy(a => a.Full.Split('.').Last(), a => a.Alias)
            .Select(g => (Pattern: new Regex($@"(?<![A-Za-z0-9_]){Regex.Escape(g.Key)}(?![A-Za-z0-9_])"), Alias: g.First()))
            .ToArray();

    /// <summary>
    /// Applies alias substitutions to framework type tokens without touching longer identifiers.
    /// </summary>
    private static string ApplyAliases(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        foreach (var (pattern, alias) in AliasFullTokenPatterns) s = pattern.Replace(s, alias);
        foreach (var (pattern, alias) in AliasShortTokenPatterns) s = pattern.Replace(s, alias);
        return s;
    }

    /// <summary>
    /// Shortens a fully‑qualified type for signature display (aliases + recursive generic argument formatting).
    /// </summary>
    private static string ShortenSignatureType(string full)
    {
        if (string.IsNullOrWhiteSpace(full)) return string.Empty;

        var s = full.Trim().Replace('{', '<').Replace('}', '>');
        s = Regex.Replace(s, @"``(\d+)", m => $"T{int.Parse(m.Groups[1].Value) + 1}");
        s = Regex.Replace(s, @"`(\d+)", m => $"T{int.Parse(m.Groups[1].Value) + 1}");

        var lt = s.IndexOf('<');
        if (lt < 0)
        {
            s = ApplyAliases(s);
            if (s.Contains('.')) s = s.Split('.').Last();
            s = s.Replace("System.", string.Empty);
            return s;
        }

        var gt = FindMatchingAngle(s, lt);
        if (gt < 0) return s;

        var head = s.Substring(0, lt);
        var inner = s.Substring(lt + 1, gt - lt - 1);
        var tail = s.Substring(gt + 1);

        head = ApplyAliases(head);
        if (head.Contains('.')) head = head.Split('.').Last();
        head = head.Replace("System.Collections.Generic.", string.Empty)
                   .Replace("System.", string.Empty);

        var args = SplitTopLevel(inner).Select(ShortenSignatureType);
        var rebuilt = $"{head}<{string.Join(", ", args)}>";
        return rebuilt + tail;

        static int FindMatchingAngle(string str, int openIdx)
        {
            int depth = 0;
            for (int i = openIdx; i < str.Length; i++)
            {
                var ch = str[i];
                if (ch == '<') depth++;
                else if (ch == '>')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }

        static IEnumerable<string> SplitTopLevel(string s)
        {
            var depth = 0; int start = 0;
            for (int i = 0; i < s.Length; i++)
            {
                var ch = s[i];
                if (ch == '<') depth++;
                else if (ch == '>') depth--;
                else if (ch == ',' && depth == 0)
                {
                    yield return s.Substring(start, i - start).Trim();
                    start = i + 1;
                }
            }
            if (start <= s.Length) yield return s.Substring(start).Trim();
        }
    }

    /// <summary>
    /// Builds a member table of contents (overload groups collapsed to first anchor).
    /// </summary>
    private string BuildMemberToc(IEnumerable<XMember> members)
    {
        var sb = new StringBuilder();
        var groups = members
            .GroupBy(m =>
            {
                var id = m.Id;
                var parenIdx = id.IndexOf('(');
                var cut = parenIdx >= 0 ? id.LastIndexOf('.', parenIdx) : id.LastIndexOf('.');
                var nameAndParams = cut >= 0 ? id.Substring(cut + 1) : id;
                return nameAndParams;
            })
            .OrderBy(g => g.Key);

        sb.AppendLine("**Table of contents**");
        foreach (var g in groups)
        {
            var first = g.First();
            var label = MemberHeader(first);
            var anchor = IdToAnchor(first.Id);
            sb.AppendLine($"- [{label}](#{anchor})");
        }
        sb.AppendLine();
        return sb.ToString();
    }

    // === Links & filenames ===

    /// <summary>
    /// Returns a Markdown link for a cref value (type or member).
    /// </summary>
    private string CrefToMarkdown(string? cref)
    {
        var sb = new StringBuilder();
        CrefToMarkdown(sb, cref);
        return sb.ToString();
    }

    /// <summary>
    /// Appends a Markdown link for a cref to a <see cref="StringBuilder"/> using the configured resolver.
    /// </summary>
    private void CrefToMarkdown(StringBuilder sb, string? cref)
    {
        var safeCref = string.IsNullOrWhiteSpace(cref) ? string.Empty : cref!;
        var link = _linkResolver.Resolve(
            safeCref,
            new LinkContext(
                CurrentTypeId: null,
                SingleFile: _singleFileMode,
                BasePath: null));

        sb.Append('[').Append(link.Label).Append("](").Append(link.Href).Append(')');
    }

    /// <summary>
    /// Basic filename builder (mode only; no root namespace trimming).
    /// </summary>
    private static string FileNameFor(string typeId, FileNameMode mode)
    {
        var name = typeId;

        if (mode == FileNameMode.CleanGenerics)
        {
            name = Regex.Replace(name, @"`\d+", "");
            name = name.Replace('{', '<').Replace('}', '>');
        }

        name = name.Replace('<', '[').Replace('>', ']');
        return name + ".md";
    }

    /// <summary>
    /// Per‑type filename generator applying mode + optional root namespace trimming and bracket normalization.
    /// </summary>
    private string FileNameForPerType(string typeId)
    {
        var name = typeId;

        if (_opt.FileNameMode == FileNameMode.CleanGenerics)
        {
            name = Regex.Replace(name, @"`\d+", "");
            name = name.Replace('{', '<').Replace('}', '>');
        }

        if (_opt.TrimRootNamespaceInFileNames && !string.IsNullOrWhiteSpace(_opt.RootNamespaceToTrim))
        {
            var prefix = _opt.RootNamespaceToTrim + ".";
            if (name.StartsWith(prefix, StringComparison.Ordinal))
                name = name.Substring(prefix.Length);
        }

        name = name.Replace('<', '[').Replace('>', ']');
        return name + ".md";
    }

    /// <summary>
    /// Converts a documentation ID to a stable anchor (lowercase; generic braces → square brackets; aliases applied).
    /// </summary>
    private static string IdToAnchor(string id) =>
        ApplyAliases(id)
            .Replace('{', '[')
            .Replace('}', ']')
            .ToLowerInvariant();

    /// <summary>
    /// Converts a <c>&lt;seealso&gt;</c> element to Markdown (cref/href/text fallback).
    /// </summary>
    private string SeeAlsoToMarkdown(XElement sa)
    {
        var cref = (string?)sa.Attribute("cref");
        if (!string.IsNullOrWhiteSpace(cref))
            return CrefToMarkdown(cref);
        var href = (string?)sa.Attribute("href");
        if (!string.IsNullOrWhiteSpace(href))
            return $"[{sa.Value}]({href})";
        return NormalizeXmlToMarkdown(sa);
    }

    /// <summary>
    /// Produces the per‑type output filename for a cref (normalizes nested type separators and applies renderer rules).
    /// </summary>
    private string TypeFileNameForResolver(string typeCref)
    {
        var id = typeCref.StartsWith("T:") ? typeCref.Substring(2) : typeCref;
        id = id.Replace('+', '.');
        return FileNameForPerType(id);
    }

    /// <summary>
    /// Shortens a type cref for display (arity → placeholders, braces normalized, aliases applied).
    /// </summary>
    private string ShortenTypeName(string cref)
    {
#if NETSTANDARD2_0
        var id = (cref.IndexOf(':') >= 0) ? cref.Split(new[] { ':' }, 2)[1] : cref;
#else
        var id = cref.Contains(':') ? cref.Split(':', 2)[1] : cref;
#endif
        var last = id.Split('.').LastOrDefault() ?? id;
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
    /// Generates a short label from a cref (type name or method name + simplified parameter list).
    /// </summary>
    private string ShortLabelFromCref(string cref)
    {
        if (string.IsNullOrWhiteSpace(cref))
            return string.Empty;

#if NETSTANDARD2_0
        var parts = cref.Split(new[] { ':' }, 2);
#else
        var parts = cref.Split(':', 2);
#endif
        var kind = parts.Length == 2 ? parts[0] : "";
        var id = parts.Length == 2 ? parts[1] : cref;

        if (kind == "T")
            return ShortTypeDisplay(id);

        if (kind == "M")
        {
            var parenIdx = id.IndexOf('(');
            var cut = parenIdx >= 0 ? id.LastIndexOf('.', parenIdx) : id.LastIndexOf('.');
            var nameAndParams = cut >= 0 ? id.Substring(cut + 1) : id;
            var paren = nameAndParams.IndexOf('(');
#if NETSTANDARD2_0
            var methodName = paren >= 0 ? nameAndParams.Substring(0, paren) : nameAndParams;
#else
            var methodName = paren >= 0 ? nameAndParams[..paren] : nameAndParams;
#endif
            methodName = Regex.Replace(methodName, @"``(\d+)", m2 =>
            {
                var n = int.Parse(m2.Groups[1].Value);
                return $"<{string.Join(",", Enumerable.Range(1, n).Select(i => $"T{i}"))}>";
            });

            var paramList = (paren >= 0 && nameAndParams.EndsWith(")"))
                ? nameAndParams.Substring(paren + 1, nameAndParams.Length - paren - 2)
                : string.Empty;

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
                        yield return s.Substring(start, i - start).Trim();
                        start = i + 1;
                    }
                }
                if (s.Length > 0) yield return s.Substring(start).Trim();
            }

            static IEnumerable<string> SplitTopLevelGenericArgs(string s)
            {
                var depth = 0; var start = 0;
                for (int i = 0; i < s.Length; i++)
                {
                    var ch = s[i];
                    if (ch == '<') depth++;
                    else if (ch == '>') depth--;
                    else if (ch == ',' && depth == 0)
                    {
                        yield return s.Substring(start, i - start).Trim();
                        start = i + 1;
                    }
                }
                if (start <= s.Length) yield return s.Substring(start).Trim();
            }

            string FormatParam(string p)
            {
                p = p.Trim();
                p = p.Replace('{', '<').Replace('}', '>');
                p = ApplyAliases(p);

                if (p.Contains('<') && p.Contains('>'))
                {
                    var lt = p.IndexOf('<');
                    var gt = p.LastIndexOf('>');
                    if (lt >= 0 && gt > lt)
                    {
#if NETSTANDARD2_0
                        var head = p.Substring(0, lt + 1);
                        var inner = p.Substring(lt + 1, gt - lt - 1);
                        var tail = p.Substring(gt);
#else
                        var head = p[..(lt + 1)];
                        var inner = p.Substring(lt + 1, gt - lt - 1);
                        var tail = p[gt..];
#endif
                        var trimmedArgs = SplitTopLevelGenericArgs(inner)
                            .Select(x => x.Contains('<')
                                ? Regex.Replace(x, @"(?<![A-Za-z0-9_])([A-ZaZ0-9_.]+)(?=\s*<)", m => m.Groups[1].Value.Split('.').Last())
                                : x.Split('.').Last()
                            );
                        var newInner = string.Join(", ", trimmedArgs);
                        p = head + newInner + tail;
                    }
                }

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

#if NETSTANDARD2_0
        if (id.Contains("."))
            return id.Substring(id.LastIndexOf('.') + 1);
#else
        if (id.Contains('.'))
            return id[(id.LastIndexOf('.') + 1)..];
#endif
        return id;
    }

    // === XML → Markdown normalization ===

    /// <summary>
    /// Normalizes an XML documentation element (summary, remarks, example, param, see, code) to Markdown with paragraph preservation.
    /// </summary>
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
                        text.Append(CrefToMarkdown(cref));
                    else
                    {
                        var href = (string?)e.Attribute("href");
                        text.Append(!string.IsNullOrWhiteSpace(href)
                            ? $"[{e.Value}]({href})"
                            : e.Value);
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

        var raw = text.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = raw.Split('\n');
        var cleaned = new string[lines.Length];
        var inFence = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var ls = line.TrimStart();
            if (ls.StartsWith("```"))
            {
                cleaned[i] = line;
                inFence = !inFence;
                continue;
            }
            if (inFence)
            {
                cleaned[i] = line;
                continue;
            }

            var collapsed = Regex.Replace(line.Trim(), "[ \t]+", " ");
            collapsed = collapsed.Replace(" .", ".")
                                 .Replace(" ,", ",")
                                 .Replace(" ;", ";")
                                 .Replace(" :", ":")
                                 .Replace(" )", ")")
                                 .Replace(" ]", "]");
            cleaned[i] = collapsed;
        }

        var sbOut = new StringBuilder();
        inFence = false;
        bool prevWasBlank = true;

        for (int i = 0; i < cleaned.Length; i++)
        {
            var line = cleaned[i];
            var ls = line.TrimStart();

            if (ls.StartsWith("```"))
            {
#if NETSTANDARD2_0
                if (sbOut.Length > 0 && sbOut[sbOut.Length - 1] != '\n')
                    sbOut.Append('\n');
#else
                if (sbOut.Length > 0 && sbOut[^1] != '\n')
                    sbOut.Append('\n');
#endif
                sbOut.Append(line).Append('\n');
                inFence = !inFence;
                prevWasBlank = true;
                continue;
            }

            if (inFence)
            {
                sbOut.Append(line).Append('\n');
                continue;
            }

            var isBlank = string.IsNullOrEmpty(line);
            if (isBlank)
            {
                if (!prevWasBlank)
                    sbOut.Append('\n').Append('\n');
                prevWasBlank = true;
            }
            else
            {
#if NETSTANDARD2_0
                if (!prevWasBlank && sbOut.Length > 0 && sbOut[sbOut.Length - 1] != '\n')
                    sbOut.Append(' ');
#else
                if (!prevWasBlank && sbOut.Length > 0 && sbOut[^1] != '\n')
                    sbOut.Append(' ');
#endif
                sbOut.Append(line);
                prevWasBlank = false;
            }
        }

        return sbOut.ToString().Trim('\n');
    }

    /// <summary>
    /// Renders a member (or overload bullet) including summary, parameters, returns, exceptions, examples, see‑also links and its stable anchor.
    /// </summary>
    private void RenderMember(XMember m, StringBuilder sb, bool asOverload)
    {
        var inherit = m.Element.Element("inheritdoc");
        if (inherit != null)
        {
            var target = InheritDocResolver.ResolveInheritedMember(_model, m);
            if (target != null)
                InheritDocResolver.MergeInheritedContent(m.Element, target);
        }

        sb.AppendLine($"<a id=\"{IdToAnchor(m.Id)}\"></a>");

        if (asOverload)
            sb.AppendLine($"- `{MemberHeader(m)}`");
        else
            sb.AppendLine($"## {MemberHeader(m)}");

        var ms = NormalizeXmlToMarkdown(m.Element.Element("summary"));
        if (!string.IsNullOrWhiteSpace(ms))
            sb.AppendLine(ms);

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

        var exTags = m.Element.Elements("exception").ToList();
        if (exTags.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("**Exceptions**");
            foreach (var e in exTags)
            {
                var cref = (string?)e.Attribute("cref");
                var desc = NormalizeXmlToMarkdown(e);
                var link = CrefToMarkdown(cref);
                sb.AppendLine($"- {link} — {desc}");
            }
        }

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
