using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xml2Doc.Core.Models;
using Xml2Doc.Core.Linking;

namespace Xml2Doc.Core;

/// <summary>
/// Renders a parsed XML documentation model to Markdown files.
/// </summary>
/// <remarks>
/// - Use <see cref="RenderToDirectory(string)"/> to emit one file per type plus an index.
/// - Use <see cref="RenderToSingleFile(string)"/> to generate a single consolidated Markdown file.
/// - Overloaded methods are grouped under a single header with each overload listed as a bullet.
/// - <c>&lt;inheritdoc&gt;</c> is resolved and merged via <see cref="InheritDocResolver"/> before rendering.
/// - Each member section emits a stable HTML anchor (via <see cref="IdToAnchor(string)"/>) so cref links resolve reliably.
/// - In single-file output, each type section also emits an anchor derived from the visible heading text (via <see cref="HeadingSlug(string)"/>).
/// - Token-aware aliasing prevents accidental replacements inside longer identifiers (e.g., keeps <c>StringComparer</c> intact).
/// - Depth-aware generic formatting: nested generics (e.g., <c>Dictionary&lt;string, List&lt;int&gt;&gt;</c>) are preserved and displayed compactly.
/// - Paragraph-preserving normalization: preserves paragraph breaks and fenced code blocks, collapses soft line wraps, and trims stray spaces before punctuation.
/// <para>
/// Rendering is influenced by <see cref="RendererOptions"/> (filename style, code block language, and optional root namespace trimming).
/// </para>
/// </remarks>
/// <seealso cref="RendererOptions"/>
/// <seealso cref="FileNameMode"/>
/// <seealso cref="InheritDocResolver"/>
public sealed class MarkdownRenderer
{
    private readonly Models.Xml2Doc _model;
    private readonly RendererOptions _opt;

    // Link strategy for generated CREF links
    private enum LinkMode { PerTypeFiles, InDocumentAnchors }
    private LinkMode _linkMode = LinkMode.PerTypeFiles;

    private readonly ILinkResolver _linkResolver;
    private bool _singleFileMode; // toggled by the render entry points

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
        _linkResolver = new DefaultLinkResolver(
        labelFromCref: ShortLabelFromCref,
        idToAnchor: IdToAnchor,
        typeFileName: TypeFileNameForResolver,
        headingSlug: HeadingSlug);
    }

    // === Public APIs ===

    /// <summary>
    /// Renders all types to individual Markdown files in the specified directory and writes an <c>index.md</c>.
    /// </summary>
    /// <param name="outDir">The output directory. It is created if it does not exist.</param>
    /// <remarks>
    /// Existing files with the same names are overwritten. Filenames are produced according to <see cref="RendererOptions.FileNameMode"/>.
    /// Links inside a page:
    /// - Type links point to per-type files (e.g., <c>MyApp.Foo.md</c>).
    /// - Member links point to anchors within the per-type file (e.g., <c>MyApp.Foo.md#myapp.foo.bar(string)</c>).
    /// </remarks>
    /// <exception cref="IOException">An I/O error occurs while writing files.</exception>
    /// <exception cref="UnauthorizedAccessException">Caller does not have the required permission.</exception>
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
                var file = Path.Combine(outDir, FileNameFor(t.Id, _opt.FileNameMode));
                File.WriteAllText(file, RenderType(t, includeHeader: true));
            }
            File.WriteAllText(Path.Combine(outDir, "index.md"), RenderIndex(types, useAnchors: false));
        }
        finally
        {
            _singleFileMode = __prev;
        }
    }

    /// <summary>
    /// Renders all types to a single Markdown file that includes an index followed by each type section.
    /// </summary>
    /// <param name="outPath">The output file path. The containing directory is created if necessary.</param>
    /// <remarks>
    /// Links inside the file:
    /// - Type links point to heading slugs (e.g., heading “<c>Foo&lt;T1&gt;</c>” → <c>#foot1</c>).
    /// - Member links point to explicit anchors emitted before each member section (see <see cref="IdToAnchor(string)"/>).
    /// </remarks>
    /// <exception cref="IOException">An I/O error occurs while writing the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Caller does not have the required permission.</exception>
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
    /// Returns the single-file markdown content as a string (same as <see cref="RenderToSingleFile(string)"/> but without writing to disk).
    /// </summary>
    public string RenderToString() => BuildSingleFileContent();

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
    /// Gets all documented types (<c>T:</c> members) from the model.
    /// </summary>
    /// <returns>An enumeration of members whose kind is <c>"T"</c> (types).</returns>
    private IEnumerable<XMember> GetTypes() =>
        _model.Members.Values.Where(m => m.Kind == "T");

    /// <summary>
    /// Builds the table of contents for the provided types.
    /// </summary>
    /// <param name="types">The set of types to include in the index.</param>
    /// <param name="useAnchors">
    /// When <see langword="true"/>, emits in-document anchor links (for single-file mode). When <see langword="false"/>, links to per-type files.
    /// </param>
    /// <returns>Markdown content for the index page.</returns>
    /// <remarks>
    /// Uses <see cref="ShortTypeDisplay(string)"/> for display.
    /// - When <paramref name="useAnchors"/> is <see langword="false"/>, links target per-type files produced by <see cref="FileNameFor(string, FileNameMode)"/>.
    /// - When <paramref name="useAnchors"/> is <see langword="true"/>, links target heading slugs derived from the visible type heading.
    /// </remarks>
    private string RenderIndex(IEnumerable<XMember> types, bool useAnchors = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# API Reference");
        foreach (var t in types)
        {
            var shortName = ShortTypeDisplay(t.Id);
            var link = useAnchors
                ? $"#{HeadingSlug(shortName)}"
                : FileNameFor(t.Id, _opt.FileNameMode);
            sb.AppendLine($"- [{shortName}]({link})");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Renders a single type section including summary, remarks, examples, see-also, and its members.
    /// </summary>
    /// <param name="type">The type (<c>T:</c> entry) to render.</param>
    /// <param name="includeHeader">When <see langword="true"/>, includes the type heading; otherwise only renders the body.</param>
    /// <returns>Markdown content for the specified type.</returns>
    /// <remarks>
    /// Members are grouped by simple name; method overloads are listed together under one heading.
    /// </remarks>
    private string RenderType(XMember type, bool includeHeader = true)
    {
        var sb = new StringBuilder();

        var typeDisplay = ShortTypeDisplay(type.Id);

        if (includeHeader)
        {
            sb.AppendLine($"# {typeDisplay}");
            sb.AppendLine();
        }

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

        // Group by the simple member name (dot before '('), not the last dot in the whole ID.
        static string GroupKey(XMember mm)
        {
            var id = mm.Id; // e.g., "M:Xml2Doc.Sample.Mathx.Add(System.Int32,System.Int32)"
            var parenIdx = id.IndexOf('(');
            var cut = parenIdx >= 0 ? id.LastIndexOf('.', parenIdx) : id.LastIndexOf('.');
            var nameAndParams = cut >= 0 ? id.Substring(cut + 1) : id; // "Add(System.Int32, ...)"

            // pretty generic method arity: ``2 -> <T1,T2>
            nameAndParams = Regex.Replace(nameAndParams, @"``(\d+)", m =>
            {
                var n = int.Parse(m.Groups[1].Value);
                return $"<{string.Join(",", Enumerable.Range(1, n).Select(i => $"T{i}"))}>";
            });

            // strip known framework aliases in the signature preview
            nameAndParams = ApplyAliases(nameAndParams);
            if (nameAndParams.StartsWith("System.", StringComparison.Ordinal))
                nameAndParams = nameAndParams.Substring("System.".Length);

            return nameAndParams;
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
    /// Builds a slug from a heading text compatible with common Markdown engines (e.g., GitHub).
    /// Lowercases, trims, replaces spaces with dashes, and removes non [a-z0-9-].
    /// </summary>
    private static string HeadingSlug(string heading)
    {
        var s = heading.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"\s+", "-");
        s = Regex.Replace(s, @"[^a-z0-9\-]", "");
        return s;
    }

    /// <summary>
    /// Builds a concise header for a member (e.g., <c>Method: Foo(int, string)</c>), simplifying type names and generics.
    /// </summary>
    /// <param name="m">The member to summarize.</param>
    /// <returns>A short header containing the member kind and simplified signature.</returns>
    /// <remarks>
    /// - Brace-aware parameter splitting (XML-doc generics use <c>{}</c>).
    /// - Formats method generic arity (e.g., <c>``2</c> → <c>&lt;T1,T2&gt;</c>).
    /// - Applies aliases and namespace trimming inside signatures.
    /// </remarks>
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
    /// Handles constructed generics (XML-doc <c>{}</c>) by delegating to <see cref="ShortenSignatureType(string)"/> for depth-aware formatting.
    /// </summary>
    private string ShortTypeDisplay(string typeId)
    {
        if (typeId.IndexOf('{') >= 0 || typeId.IndexOf('}') >= 0 || typeId.IndexOf('<') >= 0)
        {
            var normalized = typeId.Replace('{', '<').Replace('}', '>');
            var display = ShortenSignatureType(normalized);

            if (!string.IsNullOrEmpty(_opt.RootNamespaceToTrim) &&
                display.StartsWith(_opt.RootNamespaceToTrim + ".", StringComparison.Ordinal))
            {
                display = display.Substring(_opt.RootNamespaceToTrim.Length + 1);
            }

            return display;
        }

        var id = typeId;
        if (!string.IsNullOrEmpty(_opt.RootNamespaceToTrim) &&
            id.StartsWith(_opt.RootNamespaceToTrim + ".", StringComparison.Ordinal))
        {
            id = id.Substring(_opt.RootNamespaceToTrim.Length + 1);
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

// Token-aware patterns for fully-qualified names (e.g., System.String) and short names (e.g., String).
// We require identifier boundaries so we don't replace substrings inside larger identifiers.
private static readonly (Regex Pattern, string Alias)[] AliasFullTokenPatterns =
    Aliases
        .Select(a => (Pattern: new Regex($@"(?<![A-Za-z0-9_]){Regex.Escape(a.Full)}(?![A-Za-z0-9_])"), a.Alias))
        .ToArray();

private static readonly (Regex Pattern, string Alias)[] AliasShortTokenPatterns =
    Aliases
        .GroupBy(a => a.Full.Split('.').Last(), a => a.Alias) // short name -> alias
        .Select(g => (Pattern: new Regex($@"(?<![A-Za-z0-9_]){Regex.Escape(g.Key)}(?![A-Za-z0-9_])"), Alias: g.First()))
        .ToArray();

/// <summary>
/// Replaces fully-qualified type names and common framework type names with their C# aliases,
/// using token-aware regex so we don't corrupt longer identifiers (e.g., <c>StringComparer</c>).
/// </summary>
private static string ApplyAliases(string s)
{
    if (string.IsNullOrEmpty(s)) return s;

    foreach (var (pattern, alias) in AliasFullTokenPatterns)
        s = pattern.Replace(s, alias);

    foreach (var (pattern, alias) in AliasShortTokenPatterns)
        s = pattern.Replace(s, alias);

    return s;
}

    /// <summary>
    /// Shortens a fully-qualified type used in a signature to a compact display form,
    /// preserving the outer generic type name and formatting generic arguments recursively.
    /// Handles XML-doc generics (<c>{}</c>) → (<c>&lt;&gt;</c>), BCL aliases, and generic placeholders (<c>``0</c>/<c>`0</c> → <c>T1</c>).
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
        if (gt < 0)
        {
            return s;
        }

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

    // === Links & filenames ===

    /// <summary>
    /// Converts a cref into a Markdown link string.
    /// </summary>
    /// <param name="cref">The cref value (e.g., <c>T:Ns.Type</c>, <c>M:Ns.Type.Method(Type)</c>) or <see langword="null"/>.</param>
    /// <returns>Markdown link text (e.g., <c>[Type](Type.md)</c>).</returns>
    private string CrefToMarkdown(string? cref)
    {
        var sb = new System.Text.StringBuilder();
        CrefToMarkdown(sb, cref);
        return sb.ToString();
    }

    /// <summary>
    /// Appends a Markdown link for the provided cref to a <see cref="StringBuilder"/>.
    /// </summary>
    /// <param name="sb">Destination builder.</param>
    /// <param name="cref">The cref value or <see langword="null"/> (treated as empty).</param>
    private void CrefToMarkdown(System.Text.StringBuilder sb, string? cref)
    {
        // Normalize null/whitespace to empty string for the resolver.
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
            name = Regex.Replace(name, @"`\d+", "");
            name = name.Replace('{', '<').Replace('}', '>');
        }

        name = name.Replace('<', '[').Replace('>', ']');

        return name + ".md";
    }

    /// <summary>
    /// Converts a documentation ID into a Markdown anchor.
    /// </summary>
    /// <param name="id">The documentation ID (portion after the kind prefix).</param>
    /// <returns>Anchor text (lowercased) that can be referenced in links.</returns>
    /// <remarks>
    /// - Applies C# aliases to framework types (e.g., <c>System.Int32</c> → <c>int</c>).
    /// - Normalizes XML-doc generic braces <c>{}</c> to square brackets <c>[]</c> for HTML safety.
    /// - Preserves the full signature including the closing parenthesis.
    /// - Lowercases the entire string for stability.
    /// Example:
    /// <code>
    /// M:Temp.Nested.Transform``1(System.Collections.Generic.List{System.Collections.Generic.Dictionary{System.String,System.Int32}})
    /// → temp.nested.transform``1(system.collections.generic.list[system.collections.generic.dictionary[string,int]])
    /// </code>
    /// </remarks>
    private static string IdToAnchor(string id) =>
    ApplyAliases(id)
        .Replace('{', '[')
        .Replace('}', ']')
        .ToLowerInvariant();

    /// <summary>
    /// Converts a <c>&lt;seealso&gt;</c> element into Markdown.
    /// </summary>
    /// <param name="sa">The <c>seealso</c> element.</param>
    /// <returns>A Markdown link or normalized text.</returns>
    /// <remarks>
    /// Supports <c>cref</c> to local API links and <c>href</c> for external URLs. If neither attribute is present,
    /// falls back to the element content via <see cref="NormalizeXmlToMarkdown(XElement?, bool)"/>.
    /// </remarks>
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

    // Returns the output file name for a *type* cref (e.g., "T:Ns.Type" → "Ns.Type.md").
    // IMPORTANT: If your per-type writer already has a canonical helper, call it here
    // to ensure byte-for-byte parity with your existing outputs.
    private string TypeFileNameForResolver(string typeCref)
    {
        // Strip "T:" if present
        var id = typeCref.StartsWith("T:") ? typeCref.Substring(2) : typeCref;

        // Normalize nested types to dot form (e.g., "Outer+Inner" → "Outer.Inner")
        id = id.Replace('+', '.');

        // If you have filename modes (e.g., "clean" vs "verbatim"), mirror the exact
        // logic used by your per-type emission here. The fallback keeps the full name:
        return id + ".md";
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
    /// - Converts XML-doc generic braces <c>{}</c> to <c>&lt;&gt;</c>, applies C# aliases, and trims namespaces inside generic arguments.
   /// - Maps method generic arity tokens (e.g., <c>``1</c>) to <c>&lt;T1&gt;</c> in the method name, so labels like <c>Transform&lt;T1&gt;(...)</c> render correctly.
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

            // Pretty generics in method name: ``2 -> <T1,T2>, ``1 -> <T1>
            methodName = Regex.Replace(methodName, @"``(\d+)", m2 =>
            {
                var n = int.Parse(m2.Groups[1].Value);
                return $"<{string.Join(",", Enumerable.Range(1, n).Select(i => $"T{i}"))}>";
            });

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
                        yield return s.Substring(start, i - start).Trim();
                        start = i + 1;
                    }
                }
                if (s.Length > 0) yield return s.Substring(start).Trim();
            }

            // Depth-aware split for generic arguments within angle brackets
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

                // XML-doc generics use {} – convert first, then alias/shorten
                p = p.Replace('{', '<').Replace('}', '>');
                p = ApplyAliases(p);

                // Optionally trim namespaces inside generic args using depth-aware splitting
                if (p.Contains('<') && p.Contains('>'))
                {
                    var lt = p.IndexOf('<');
                    var gt = p.LastIndexOf('>');
                    if (lt >= 0 && gt > lt)
                    {
                        var head = p[..(lt + 1)];
                        var inner = p.Substring(lt + 1, gt - lt - 1);
                        var trimmedArgs = SplitTopLevelGenericArgs(inner)
                            .Select(x => x.Contains('<')
                                ? Regex.Replace(x, @"(?<![A-Za-z0-9_])([A-Za-z0-9_.]+)(?=\s*<)", m => m.Groups[1].Value.Split('.').Last())
                                : x.Split('.').Last()
                            );
                        var newInner = string.Join(", ", trimmedArgs);
                        var tail = p[gt..];
                        p = head + newInner + tail;
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
    /// Supported:
    /// - <c>&lt;see cref="..." /&gt;</c> and <c>&lt;see href="..."&gt;text&lt;/see&gt;</c> (converted to links via <see cref="CrefToMarkdown(string?)"/>).
    /// - <c>&lt;paramref name="..." /&gt;</c> (rendered as inline code).
    /// - <c>&lt;para&gt;</c> (emits paragraph breaks).
    /// - <c>&lt;c&gt;</c> and <c>&lt;code&gt;</c> (inline code or fenced blocks; language from <see cref="RendererOptions.CodeBlockLanguage"/>).
    /// - <c>&lt;example&gt;</c> (detects code and renders as fenced blocks when possible).
    /// Newline handling:
    /// - Preserves blank lines as paragraph breaks.
    /// - Collapses soft line breaks within a paragraph to a single space.
    /// - Trims leading whitespace on prose lines and collapses repeated spaces/tabs to a single space.
    /// - Keeps fenced code blocks verbatim.
    /// - Fixes stray spaces before punctuation (e.g., <c>"word ."</c> → <c>"word."</c>).
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
                        // Use the resolver to produce the correct markdown link/label.
                        text.Append(CrefToMarkdown(cref));
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
                if (sbOut.Length > 0 && sbOut[^1] != '\n')
                    sbOut.Append('\n');

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
                if (!prevWasBlank && sbOut.Length > 0 && sbOut[^1] != '\n')
                    sbOut.Append(' ');
                sbOut.Append(line);
                prevWasBlank = false;
            }
        }

        var result = sbOut.ToString().Trim('\n');
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
    /// - Emits a stable HTML anchor <c>&lt;a id="..."&gt;&lt;/a&gt;</c> before the heading/bullet using <see cref="IdToAnchor(string)"/>,
    ///   ensuring all member links resolve predictably.
    /// - If an <c>&lt;inheritdoc&gt;</c> tag is present, inherited content is resolved via <see cref="InheritDocResolver"/> and merged before rendering.
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

        // Emit a stable anchor for this member so cref links resolve here.
        // Use the same transformation that CrefToMarkdown uses.
        sb.AppendLine($"<a id=\"{IdToAnchor(m.Id)}\"></a>");

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
                var link = CrefToMarkdown(cref);
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
