using System;
#if NETSTANDARD2_0
using Xml2Doc.Core.Compat;
#endif
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xml2Doc.Core.Models;
using Xml2Doc.Core.Linking;
using System.Globalization;

namespace Xml2Doc.Core;

/// <summary>
/// Renders a parsed XML documentation model to Markdown (multi‑file or single‑file).
/// </summary>
/// <remarks>
/// Core capabilities:
/// <list type="bullet">
///   <item><description>Multi‑file output via <see cref="RenderToDirectory(string)"/> (one file per type + <c>index.md</c>).</description></item>
///   <item><description>Single consolidated file via <see cref="RenderToSingleFile(string)"/> (index followed by all types).</description></item>
///   <item><description>Overload grouping (method overloads share one heading, individual signatures listed as bullets).</description></item>
///   <item><description><c>&lt;inheritdoc&gt;</c> resolution / merge through <see cref="InheritDocResolver"/>.</description></item>
///   <item><description>Stable anchors for member sections (<see cref="IdToAnchor(string)"/>) and heading slugs (<see cref="HeadingSlug(string)"/>).</description></item>
///   <item><description>Depth‑aware generic signature formatting with alias substitution (framework types → C# keywords).</description></item>
///   <item><description>Paragraph‑preserving XML → Markdown normalization (code blocks kept verbatim; soft wraps collapsed).</description></item>
///   <item><description>Optional root namespace trimming and filename transformations (<see cref="RendererOptions"/>).</description></item>
///   <item><description>Optional per‑type member TOC (<see cref="RendererOptions.EmitToc"/>).</description></item>
///   <item><description>Optional namespace index pages (<see cref="RendererOptions.EmitNamespaceIndex"/>).</description></item>
///   <item><description>Deterministic planning of outputs without writing via <see cref="PlanOutputs(string,string?)"/> (used for dry‑run / reporting).</description></item>
///   <item><description>Selectable slug algorithm (<see cref="RendererOptions.AnchorAlgorithm"/>): Default / GitHub / Kramdown / Gfm.</description></item>
/// </list>
/// Anchor algorithm summary:
/// <list type="bullet">
///   <item><description><b>Default</b>: lowercase, whitespace → dash, strip non <c>[a-z0-9-]</c>, collapse multi‑dash runs.</description></item>
///   <item><description><b>GitHub/Gfm</b>: Unicode normalization + diacritic removal; drop punctuation; whitespace → dash; trim dashes.</description></item>
///   <item><description><b>Kramdown</b>: Similar to GitHub but retains underscores; punctuation removed; whitespace → dash.</description></item>
/// </list>
/// Public rendering methods allow I/O exceptions to surface (no catch/ swallow beyond outer <c>Main</c> typical usage).
/// </remarks>
public sealed class MarkdownRenderer
{
    private readonly Models.Xml2Doc _model;
    private readonly RendererOptions _opt;

    /// <summary>
    /// Internal link target selection mode for cref resolution (multi‑file vs single‑file).
    /// </summary>
    private enum LinkMode { PerTypeFiles, InDocumentAnchors }
    private LinkMode _linkMode = LinkMode.PerTypeFiles;

    private readonly ILinkResolver _linkResolver;
    private bool _singleFileMode;

    /// <summary>
    /// Precompiled whitespace matching regex (reserved for future slug optimizations).
    /// </summary>
    private static readonly Regex Spaces = new Regex(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Precompiled pattern for GitHub slug punctuation removal (currently unused; kept for potential micro‑optimization).
    /// </summary>
    private static readonly Regex GitHubDrop = new Regex(@"[^a-z0-9\- ]+", RegexOptions.Compiled);

    /// <summary>
    /// Precompiled pattern for Kramdown slug punctuation removal (unused placeholder).
    /// </summary>
    private static readonly Regex KramdownDrop = new Regex(@"[^a-z0-9\- _:.]+", RegexOptions.Compiled);

    /// <summary>
    /// Precompiled pattern for GFM slug punctuation removal (unused placeholder).
    /// </summary>
    private static readonly Regex GfmDrop = new Regex(@"[^a-z0-9\-_. ]+", RegexOptions.Compiled);

    /// <summary>
    /// Collapses consecutive dashes to a single dash (unused placeholder for potential manual slug pipelines).
    /// </summary>
    private static readonly Regex CollapseDash = new Regex(@"\-+", RegexOptions.Compiled);

    /// <summary>
    /// Creates a renderer for a parsed XML documentation model.
    /// </summary>
    /// <param name="model">Parsed XML documentation model (never null).</param>
    /// <param name="options">Optional rendering options; defaults applied when null.</param>
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
    /// Emits one Markdown file per documented type plus an <c>index.md</c>. Optionally emits namespace index pages.
    /// </summary>
    /// <param name="outDir">Destination directory (created if absent).</param>
    /// <remarks>
    /// Overwrites existing files. Per‑type links point to sibling files; member links point to in‑file anchors.
    /// Respects <see cref="RendererOptions.FileNameMode"/> and <see cref="RendererOptions.TrimRootNamespaceInFileNames"/>.
    /// Namespace index emission (<see cref="RendererOptions.EmitNamespaceIndex"/>) adds:
    /// <list type="bullet">
    ///   <item><description><c>namespaces.md</c> — overview of all namespaces.</description></item>
    ///   <item><description><c>namespaces/&lt;namespace&gt;.md</c> — per‑namespace type listing.</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="IOException">Error writing one or more output files.</exception>
    /// <exception cref="UnauthorizedAccessException">Insufficient permissions for the target directory.</exception>
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
                    var fileSafe = ns == "(global)" ? "_global_" : SafeNamespaceFileName(ns);
                    var nsFile = Path.Combine(nsDir, $"{fileSafe}.md");

                    var sbNs = new StringBuilder();
                    sbNs.AppendLine($"# {ns}");
                    foreach (var t in kv.Value.OrderBy(t => t.Id, StringComparer.Ordinal))
                    {
                        var shortName = ShortTypeDisplay(t.Id);
                        var perTypeFile = FileNameForPerType(t.Id);
                        sbNs.AppendLine($"- [{shortName}]({Path.Combine("..", perTypeFile).Replace('\\', '/')})");
                    }
                    File.WriteAllText(nsFile, sbNs.ToString());
                }

                var nsIndex = new StringBuilder();
                nsIndex.AppendLine("# Namespaces");
                foreach (var ns in nsMap.Keys.OrderBy(s => s, StringComparer.Ordinal))
                {
                    var fileSafe = ns == "(global)" ? "_global_" : SafeNamespaceFileName(ns);
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
    /// Emits a single Markdown file (index + all types + their members).
    /// </summary>
    /// <param name="outPath">Output file path (parent directory created if needed).</param>
    /// <remarks>Type links become heading slugs; member links use explicit anchors from <see cref="IdToAnchor(string)"/>.</remarks>
    /// <exception cref="IOException">Error writing the output file.</exception>
    /// <exception cref="UnauthorizedAccessException">Insufficient permissions for the output path.</exception>
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
    /// Returns the consolidated single‑file content (index + all types) without writing.
    /// </summary>
    public string RenderToString() => BuildSingleFileContent();

    /// <summary>
    /// Builds single‑file content, temporarily switching link mode to in‑document anchors.
    /// </summary>
    /// <returns>Markdown string containing index + all types.</returns>
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
    /// Enumerates all documented types (<c>T:</c> members only).
    /// </summary>
    private IEnumerable<XMember> GetTypes() =>
        _model.Members.Values.Where(m => m.Kind == "T");

    /// <summary>
    /// Builds a type index linking either to per‑type files or heading anchors (single‑file mode).
    /// </summary>
    /// <param name="types">Sequence of type members.</param>
    /// <param name="useAnchors">True to link to in‑document anchors; false for per‑type files.</param>
    private string RenderIndex(IEnumerable<XMember> types, bool useAnchors = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# API Reference");
        foreach (var t in types)
        {
            var shortName = ShortTypeDisplay(t.Id);
            var link = useAnchors ? $"#{HeadingSlug(shortName)}" : FileNameForPerType(t.Id);
            sb.AppendLine($"- [{shortName}]({link})");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Renders a single type (summary, remarks, examples, see‑also, optional member TOC, members grouped by overload).
    /// </summary>
    /// <param name="type">Type (<c>T:</c>) member.</param>
    /// <param name="includeHeader">Emit a top-level heading when true.</param>
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

        // Insert per‑type member TOC (multi‑file mode only).
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

            // Pretty method generic arity: ``N → <T1,…,TN>
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

    /// <summary>
    /// Computes the exact list of files this renderer would write for the current options (no disk I/O).
    /// </summary>
    /// <param name="outDir">Destination directory (may not exist).</param>
    /// <param name="singleFilePath">If non-null, plans single-file output; otherwise multi‑file.</param>
    /// <returns>Absolute paths of files that would be produced.</returns>
    /// <remarks>
    /// Multi‑file mode always includes <c>index.md</c>. Namespace index emission adds <c>namespaces.md</c> and one page per namespace.
    /// </remarks>
    public IReadOnlyList<string> PlanOutputs(string outDir, string? singleFilePath = null)
    {
        if (!string.IsNullOrWhiteSpace(singleFilePath))
        {
            var full = Path.GetFullPath(singleFilePath);
            return new[] { full };
        }

        var root = Path.GetFullPath(outDir);
        var list = new List<string>();

        var types = GetTypes().OrderBy(t => t.Id, StringComparer.Ordinal).ToList();
        foreach (var t in types)
        {
            var name = FileNameForPerType(t.Id);
            list.Add(Path.Combine(root, name));
        }

        list.Add(Path.Combine(root, "index.md"));

        if (_opt.EmitNamespaceIndex)
        {
            var nsDir = Path.Combine(root, "namespaces");
            var nsSet = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var t in types)
            {
                var id = t.Id;
                var lastDot = id.LastIndexOf('.');
                var ns = lastDot > 0 ? id.Substring(0, lastDot) : "(global)";
                nsSet.Add(ns);
            }

            foreach (var ns in nsSet)
            {
                var fileSafe = SafeNamespaceFileName(ns);
                list.Add(Path.Combine(nsDir, fileSafe + ".md"));
            }

            list.Add(Path.Combine(root, "namespaces.md"));
        }

        return list;
    }

    // === Display helpers ===

    /// <summary>
    /// Resolves a heading slug using the configured <see cref="RendererOptions.AnchorAlgorithm"/>.
    /// </summary>
    /// <param name="heading">Raw heading text.</param>
    /// <returns>Algorithm-specific slug string.</returns>
    private string HeadingSlug(string heading)
    {
        switch (_opt.AnchorAlgorithm)
        {
            case AnchorAlgorithm.Github:
                return GithubSlug(heading);
            case AnchorAlgorithm.Kramdown:
                return KramdownSlug(heading);
            case AnchorAlgorithm.Gfm:
                return GfmSlug(heading);
            case AnchorAlgorithm.Default:
            default:
                return DefaultSlug(heading);
        }
    }

    /// <summary>
    /// Default slug (lowercase, whitespace → single dash, strip non <c>[a-z0-9-]</c>, collapse multi‑dash runs, trim dashes).
    /// </summary>
    private static string DefaultSlug(string heading)
    {
        var s = heading.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"\s+", "-");
        s = Regex.Replace(s, @"[^a-z0-9\-]", "");
        s = Regex.Replace(s, @"\-{2,}", "-").Trim('-');
        return s;
    }

    /// <summary>
    /// GitHub-style slug: Unicode normalize + diacritic removal, lowercase, drop punctuation, collapse spaces to dashes, trim.
    /// </summary>
    private static string GithubSlug(string heading)
    {
        var formD = heading.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        var s = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        s = Regex.Replace(s, @"[^a-z0-9\s\-]", " ");
        s = Regex.Replace(s, @"\s+", "-");
        s = Regex.Replace(s, @"\-{2,}", "-").Trim('-');
        return s;
    }

    /// <summary>
    /// Kramdown/Jekyll slug: diacritics removed, lowercase, punctuation stripped (except underscore), whitespace → dash, trim.
    /// </summary>
    private static string KramdownSlug(string heading)
    {
        var formD = heading.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        var s = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        s = Regex.Replace(s, @"[^\w\s\-]", " ");
        s = Regex.Replace(s, @"\s+", "-");
        s = Regex.Replace(s, @"\-{2,}", "-").Trim('-');
        return s;
    }

    /// <summary>
    /// GFM slug variant: lowercase, retain underscore and dot, remove other punctuation, whitespace becomes dash, collapse dashes, trim.
    /// </summary>
    private static string GfmSlug(string heading)
    {
        var s = heading.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"[^a-z0-9\-_.\s]", "");
        s = Regex.Replace(s, @"\s+", "-");
        s = Regex.Replace(s, @"\-{2,}", "-").Trim('-');
        return s;
    }

    /// <summary>
    /// Builds a concise member header (Kind + simplified signature) for headings and overload bullets.
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
    /// Maps XML documentation kind letter to a readable word.
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
    /// Produces a short display name for a type ID (generic arity → &lt;T…&gt;, optional root namespace trimming).
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
            name = Regex.Replace(name, @"`(\d+)", "__$1");
            name = name.Replace('{', '<').Replace('}', '>');
        }

        name = name.Replace('<', '[').Replace('>', ']');
        return name + ".md";
    }

    /// <summary>
    /// Creates a stable file safe namespace page filename (replaces separators and generic brackets).
    /// </summary>
    private static string SafeNamespaceFileName(string ns)
    {
        if (string.Equals(ns, "(global)", StringComparison.Ordinal)) return "_global_";
        return ns
            .Replace('<', '[').Replace('>', ']')
            .Replace('+', '.')
            .Replace('/', '.').Replace('\\', '.');
    }

    /// <summary>
    /// Per‑type filename generator applying mode + optional root namespace trimming + optional basename stripping.
    /// </summary>
    /// <remarks>Basename stripping applied only when <see cref="RendererOptions.BasenameOnly"/> is true.</remarks>
    private string FileNameForPerType(string typeId)
    {
        var name = typeId;

        if (_opt.FileNameMode == FileNameMode.CleanGenerics)
        {
            name = Regex.Replace(name, @"`(\d+)", "__$1");
            name = name.Replace('{', '<').Replace('}', '>');
        }

        if (_opt.TrimRootNamespaceInFileNames && !string.IsNullOrWhiteSpace(_opt.RootNamespaceToTrim))
        {
            var prefix = _opt.RootNamespaceToTrim + ".";
            if (name.StartsWith(prefix, StringComparison.Ordinal))
                name = name.Substring(prefix.Length);
        }

        if (_opt.BasenameOnly)
        {
            var lastDot = name.LastIndexOf('.');
            if (lastDot >= 0) name = name.Substring(lastDot + 1);
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
    /// Converts a <c>&lt;seealso&gt;</c> element to Markdown (cref, href, or inner text).
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
    /// Produces the per‑type output filename for a cref (normalizes nested type separators then applies renderer rules).
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
    /// <param name="element">XML element or null.</param>
    /// <param name="preferCodeBlocks">True to prefer fenced blocks for multi‑line code/examples.</param>
    /// <returns>Markdown string (empty if element is null).</returns>
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
    /// Renders a member (or overload bullet) including summary, parameters, returns, exceptions, examples, see‑also links, and a stable anchor.
    /// </summary>
    /// <param name="m">Member to render.</param>
    /// <param name="sb">Destination builder.</param>
    /// <param name="asOverload">True to render as a bullet under an overload group; false for a full section.</param>
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
