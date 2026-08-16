using System;
#if NETSTANDARD2_0
using Xml2Doc.Core.Compat;
#endif
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xml2Doc.Core.Models;
using Xml2Doc.Core.Linking;
using Xml2Doc.Core.OutputLifecycle;
using Xml2Doc.Core.Aliasing;
using Xml2Doc.Core.Anchoring;
using Xml2Doc.Core.Templates;
using Xml2Doc.Core.AutoLinking;
using Xml2Doc.Core.Signatures;
using Xml2Doc.Core.Diagnostics;
using Xml2Doc.Core.Pipeline;

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
    private static readonly Encoding MarkdownEncoding =
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private readonly Models.Xml2Doc _model;
    private readonly SymbolIndex _symbolIndex;
    private readonly RendererOptions _opt;
    private readonly IAliasProvider _aliasProvider;
    private readonly IAnchorGenerator _anchorGenerator;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly IAutoLinker _autoLinker;
    private readonly ISignatureRenderer _signatureRenderer;
    private readonly SignatureStyle _signatureStyle;
    private readonly HashSet<string> _reportedDiagnostics =
        new(StringComparer.Ordinal);
    private readonly object _diagnosticLock = new();
    private readonly AutoLinkContext _perTypeAutoLinkContext;
    private readonly AutoLinkContext _singleFileAutoLinkContext;

    /// <summary>
    /// Internal link target selection mode for cref resolution (multi‑file vs single‑file).
    /// </summary>
    private enum LinkMode { PerTypeFiles, InDocumentAnchors }
    private LinkMode _linkMode = LinkMode.PerTypeFiles;

    private readonly ILinkResolver _linkResolver;
    private bool _singleFileMode;

    /// <summary>
    /// Creates a renderer for a parsed XML documentation model.
    /// </summary>
    /// <param name="model">Parsed XML documentation model (never null).</param>
    /// <param name="options">Optional rendering options; defaults applied when null.</param>
    public MarkdownRenderer(Models.Xml2Doc model, RendererOptions? options = null)
    {
        _model = model;
        _symbolIndex = SymbolIndex.Build(model);
        _opt = options ?? new RendererOptions();
        _aliasProvider = _opt.AliasProvider ?? DefaultAliasProvider.Instance;
        _signatureStyle = _opt.SignatureStyle ?? SignatureStyle.Default;
        _signatureRenderer = _opt.SignatureRenderer ??
            new DefaultSignatureRenderer(
                _aliasProvider,
                _opt.RootNamespaceToTrim);
        _anchorGenerator = _opt.AnchorGenerator ??
            new DefaultAnchorGenerator(_opt.AnchorAlgorithm, _aliasProvider);
        if (_opt.TemplateRenderer is not null &&
            (!string.IsNullOrWhiteSpace(_opt.TemplatePath) ||
             !string.IsNullOrWhiteSpace(_opt.FrontMatterPath)))
        {
            throw new ArgumentException(
                "TemplateRenderer cannot be combined with TemplatePath or FrontMatterPath.",
                nameof(options));
        }
        if (_opt.FrontMatter is not null &&
            !string.IsNullOrWhiteSpace(_opt.FrontMatterPath))
        {
            throw new ArgumentException(
                "FrontMatter cannot be combined with FrontMatterPath.",
                nameof(options));
        }

        _templateRenderer = _opt.TemplateRenderer ??
            (!string.IsNullOrWhiteSpace(_opt.TemplatePath) ||
             !string.IsNullOrWhiteSpace(_opt.FrontMatterPath)
                ? new FileTemplateRenderer(
                    _opt.TemplatePath,
                    _opt.FrontMatterPath)
                : DefaultTemplateRenderer.Instance);
        _autoLinker = _opt.AutoLinker ?? SimpleAutoLinker.Instance;
        var externalSymbolResolver = _opt.ExternalSymbolResolver ??
            (!string.IsNullOrWhiteSpace(_opt.ExternalDocs)
                ? new BaseUrlExternalSymbolResolver(_opt.ExternalDocs!)
                : null);
        _linkResolver = new DefaultLinkResolver(
            labelFromCref: _signatureRenderer.RenderCrefLabel,
            idToAnchor: IdToAnchor,
            typeFileName: TypeFileNameForResolver,
            headingSlug: HeadingSlug,
            isKnownCref: IsKnownCref,
            linkPolicy: _opt.LinkPolicy,
            externalSymbolResolver: externalSymbolResolver,
            unresolvedCref: ReportUnresolvedCref);
        _perTypeAutoLinkContext = BuildAutoLinkContext(singleFile: false);
        _singleFileAutoLinkContext = BuildAutoLinkContext(singleFile: true);
    }

    // === Public APIs ===

    /// <summary>
    /// Emits one Markdown file per documented type and, by default, an <c>index.md</c>. Optionally emits namespace index pages.
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
    public void RenderToDirectory(string outDir) =>
        _ = RenderToDirectoryWithResult(outDir);

    internal RendererWriteResult RenderToDirectoryWithResult(string outDir)
    {
        var renderingStopwatch = Stopwatch.StartNew();
        ValidateAnchors(singleFile: false);
        var __prev = _singleFileMode;
        var writtenFiles = new List<string>();
        var skippedFiles = new List<string>();
        IReadOnlyList<string> prunedFiles = Array.Empty<string>();
        var lifecycleElapsed = TimeSpan.Zero;
        OutputManifestLocation? manifestLocation = null;
        IReadOnlyList<string>? generatedFiles = null;

        if (_opt.PruneStaleFiles)
        {
            manifestLocation = OutputManifestLocation.Create(
                outDir,
                _opt.ManifestIdentity!);
            generatedFiles = GetOutputRootRelativePaths(
                manifestLocation.OutputRoot,
                PlanOutputs(outDir));
            OutputManifestPlanner.CreatePlan(
                manifestLocation.OutputRoot,
                generatedFiles,
                previousManifest: null);
        }

        try
        {
            _singleFileMode = false;
            _linkMode = LinkMode.PerTypeFiles;

            Directory.CreateDirectory(outDir);
            var types = GetTypes()
                .OrderBy(t => t.Id, StringComparer.Ordinal)
                .ToList();
            var typeFiles = types
                .Select(type =>
                    Path.Combine(outDir, FileNameForPerType(type.Id)))
                .ToArray();
            var typeWasWritten = new bool[types.Count];

            void RenderTypeAt(int index)
            {
                var type = types[index];
                typeWasWritten[index] = WriteMarkdownFileIfChanged(
                    typeFiles[index],
                    NormalizeLineEndings(ApplyTemplate(
                        RenderType(type, includeHeader: true),
                        _signatureRenderer.RenderTypeName(type.Id),
                        TemplateDocumentKind.Type)));
            }

            var parallelDegree = _opt.ParallelDegree.GetValueOrDefault(1);
            if (parallelDegree > 1 && types.Count > 1)
            {
                Parallel.For(
                    0,
                    types.Count,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = parallelDegree
                    },
                    RenderTypeAt);
            }
            else
            {
                for (var index = 0; index < types.Count; index++)
                    RenderTypeAt(index);
            }

            for (var index = 0; index < typeFiles.Length; index++)
            {
                (typeWasWritten[index]
                    ? writtenFiles
                    : skippedFiles).Add(typeFiles[index]);
            }
            if (_opt.GenerateIndex)
                RecordWriteResult(
                    CombineOutputPath(outDir, "index.md"),
                    NormalizeLineEndings(ApplyTemplate(
                        RenderIndex(types, useAnchors: false),
                        "API Reference",
                        TemplateDocumentKind.Index)),
                    writtenFiles,
                    skippedFiles);

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
                        var shortName = _signatureRenderer.RenderTypeName(t.Id);
                        var perTypeFile = FileNameForPerType(t.Id);
                        sbNs.AppendLine($"- [{shortName}]({Path.Combine("..", perTypeFile).Replace('\\', '/')})");
                    }
                    RecordWriteResult(
                        nsFile,
                        NormalizeLineEndings(ApplyTemplate(
                            sbNs.ToString(),
                            ns,
                            TemplateDocumentKind.NamespaceIndex)),
                        writtenFiles,
                        skippedFiles);
                }

                var nsIndex = new StringBuilder();
                nsIndex.AppendLine("# Namespaces");
                foreach (var ns in nsMap.Keys.OrderBy(s => s, StringComparer.Ordinal))
                {
                    var fileSafe = ns == "(global)" ? "_global_" : SafeNamespaceFileName(ns);
                    nsIndex.AppendLine($"- [{ns}](namespaces/{fileSafe}.md)");
                }
                RecordWriteResult(
                    CombineOutputPath(outDir, "namespaces.md"),
                    NormalizeLineEndings(ApplyTemplate(
                        nsIndex.ToString(),
                        "Namespaces",
                        TemplateDocumentKind.NamespaceOverview)),
                    writtenFiles,
                    skippedFiles);
            }

            if (manifestLocation is not null &&
                generatedFiles is not null)
            {
                renderingStopwatch.Stop();
                var lifecycleStopwatch = Stopwatch.StartNew();
                var lifecycleResult = OutputLifecycleExecutor
                    .ExecuteAfterSuccessfulGenerationWithResult(
                        manifestLocation,
                        generatedFiles);
                lifecycleStopwatch.Stop();
                lifecycleElapsed = lifecycleStopwatch.Elapsed;
                prunedFiles = lifecycleResult.DeletedFiles;
            }
        }
        finally
        {
            _singleFileMode = __prev;
        }

        renderingStopwatch.Stop();
        return new RendererWriteResult(
            writtenFiles,
            skippedFiles,
            prunedFiles,
            renderingStopwatch.Elapsed,
            lifecycleElapsed);
    }

    /// <summary>
    /// Emits a single Markdown file (index + all types + their members).
    /// </summary>
    /// <param name="outPath">Output file path (parent directory created if needed).</param>
    /// <remarks>Type links become heading slugs; member links use explicit anchors from <see cref="IdToAnchor(string)"/>.</remarks>
    /// <exception cref="IOException">Error writing the output file.</exception>
    /// <exception cref="UnauthorizedAccessException">Insufficient permissions for the output path.</exception>
    public void RenderToSingleFile(string outPath) =>
        _ = RenderToSingleFileWithResult(outPath);

    internal RendererWriteResult RenderToSingleFileWithResult(string outPath)
    {
        var __prev = _singleFileMode;
        var writtenFiles = new List<string>();
        var skippedFiles = new List<string>();
        var renderingStopwatch = Stopwatch.StartNew();
        try
        {
            _singleFileMode = true;
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
            RecordWriteResult(
                outPath,
                NormalizeLineEndings(BuildSingleFileContent()),
                writtenFiles,
                skippedFiles);
        }
        finally
        {
            _singleFileMode = __prev;
        }

        renderingStopwatch.Stop();
        return new RendererWriteResult(
            writtenFiles,
            skippedFiles,
            PrunedFiles: Array.Empty<string>(),
            RenderingElapsed: renderingStopwatch.Elapsed,
            LifecycleElapsed: TimeSpan.Zero);
    }

    /// <summary>
    /// Returns the consolidated single‑file content (index + all types) without writing.
    /// </summary>
    public string RenderToString() =>
        NormalizeLineEndings(BuildSingleFileContent());

    private static bool WriteMarkdownFileIfChanged(
        string path,
        string content)
    {
        var bytes = MarkdownEncoding.GetBytes(content);

        if (File.Exists(path) &&
            File.ReadAllBytes(path).SequenceEqual(bytes))
        {
            return false;
        }

        File.WriteAllBytes(path, bytes);
        return true;
    }

    private static void RecordWriteResult(
        string path,
        string content,
        ICollection<string> writtenFiles,
        ICollection<string> skippedFiles)
    {
        if (WriteMarkdownFileIfChanged(path, content))
            writtenFiles.Add(path);
        else
            skippedFiles.Add(path);
    }

    private string NormalizeLineEndings(string content)
    {
        var normalized = content
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');

        var lineEnding = _opt.LineEndings switch
        {
            LineEndingStyle.CrLf => "\r\n",
            LineEndingStyle.Native => Environment.NewLine,
            _ => "\n"
        };

        return lineEnding == "\n"
            ? normalized
            : normalized.Replace("\n", lineEnding);
    }

    private static string CombineOutputPath(string outputDirectory, string fileName)
    {
        if (Path.IsPathRooted(fileName))
            throw new ArgumentException("The output file name must be relative.", nameof(fileName));

        var outputRoot = Path.GetFullPath(outputDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(outputRoot + fileName);
        var comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!candidate.StartsWith(outputRoot, comparison))
            throw new ArgumentException("The output file name must remain within the output directory.", nameof(fileName));

        return candidate;
    }

    /// <summary>
    /// Builds single‑file content, temporarily switching link mode to in‑document anchors.
    /// </summary>
    /// <returns>Markdown string containing index + all types.</returns>
    private string BuildSingleFileContent()
    {
        ValidateAnchors(singleFile: true);
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
                var typeDisplay = _signatureRenderer.RenderTypeName(t.Id);
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

            return ApplyTemplate(
                sb.ToString(),
                "API Reference",
                TemplateDocumentKind.SingleFile);
        }
        finally
        {
            _linkMode = prev;
        }
    }

    private string ApplyTemplate(
        string content,
        string? title,
        TemplateDocumentKind kind)
    {
        var context = new TemplateRenderContext(content, title, kind);
        var rendered = _templateRenderer.Render(context);
        var frontMatter = _opt.FrontMatter?.Invoke(context);

        if (frontMatter is null || frontMatter.Count == 0)
            return rendered;

        return YamlFrontMatter.Serialize(frontMatter) + "\n" + rendered;
    }

    // === Core rendering ===

    /// <summary>
    /// Enumerates all documented types (<c>T:</c> members only).
    /// </summary>
    private IEnumerable<XMember> GetTypes() =>
        _symbolIndex.Types;

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
            var shortName = _signatureRenderer.RenderTypeName(t.Id);
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
        var typeDisplay = _signatureRenderer.RenderTypeName(type.Id);

        if (includeHeader)
        {
            sb.AppendLine($"# {typeDisplay}");
            sb.AppendLine();
        }

        var summary = NormalizeXmlToMarkdown(type.Element.Element("summary"));
        if (string.IsNullOrWhiteSpace(summary))
            ReportMissingSummary(type);
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

        var members = _symbolIndex.Members.Values
            .Where(m => m.Kind is "M" or "P" or "F" or "E")
            .Where(m => m.Id.StartsWith(type.Id + ".", StringComparison.Ordinal))
            .OrderBy(m => m.Id, StringComparer.Ordinal)
            .ToList();

        // Insert per‑type member TOC (multi‑file mode only).
        if (includeHeader && _opt.EmitToc && members.Count > 0 && !_singleFileMode)
        {
            sb.AppendLine(BuildMemberToc(members));
        }

        string GroupKey(XMember mm)
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

            nameAndParams = _aliasProvider.ApplyAliases(nameAndParams);
            if (nameAndParams.StartsWith("System.", StringComparison.Ordinal))
                nameAndParams = nameAndParams.Substring("System.".Length);

            return nameAndParams;
        }

        var groups = members
            .GroupBy(GroupKey, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToList();

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
    /// Multi‑file mode includes <c>index.md</c> when <see cref="RendererOptions.GenerateIndex"/> is true.
    /// Namespace index emission adds <c>namespaces.md</c> and one page per namespace.
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

        if (_opt.GenerateIndex)
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

    private static IReadOnlyList<string> GetOutputRootRelativePaths(
        string outputRoot,
        IReadOnlyList<string> absolutePaths)
    {
        var rootWithSeparator =
            outputRoot.EndsWith(
                Path.DirectorySeparatorChar.ToString(),
                StringComparison.Ordinal) ||
            outputRoot.EndsWith(
                Path.AltDirectorySeparatorChar.ToString(),
                StringComparison.Ordinal)
                ? outputRoot
                : outputRoot + Path.DirectorySeparatorChar;

        return absolutePaths
            .Select(path =>
            {
                var fullPath = Path.GetFullPath(path);

                if (!fullPath.StartsWith(
                        rootWithSeparator,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "A planned renderer output is outside the output root.");
                }

                return fullPath.Substring(rootWithSeparator.Length);
            })
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    // === Display helpers ===

    /// <summary>
    /// Resolves a heading slug using the configured <see cref="RendererOptions.AnchorAlgorithm"/>.
    /// </summary>
    /// <param name="heading">Raw heading text.</param>
    /// <returns>Algorithm-specific slug string.</returns>
    private string HeadingSlug(string heading) =>
        _anchorGenerator.GenerateHeadingAnchor(heading);

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
            var label = _signatureRenderer.RenderMemberHeader(first, _signatureStyle);
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

    private bool IsKnownCref(string cref) =>
        _symbolIndex.ContainsMember(cref);

    private void ReportUnresolvedCref(string cref)
    {
        if (string.IsNullOrWhiteSpace(cref))
            return;

        ReportDiagnostic(new Xml2DocDiagnostic(
            DiagnosticIds.UnresolvedCref,
            DiagnosticSeverity.Warning,
            $"Unable to resolve cref '{cref}'.",
            MemberId: cref));
    }

    private void ReportMissingSummary(XMember member) =>
        ReportDiagnostic(new Xml2DocDiagnostic(
            DiagnosticIds.MissingSummary,
            DiagnosticSeverity.Warning,
            $"Documentation member '{member.Name}' does not contain a summary.",
            MemberId: member.Name));

    private void ReportDiagnostic(Xml2DocDiagnostic diagnostic)
    {
        var key = string.Join(
            "|",
            diagnostic.Code,
            diagnostic.MemberId ?? string.Empty,
            diagnostic.Message);
        lock (_diagnosticLock)
        {
            if (_reportedDiagnostics.Add(key))
                _opt.DiagnosticSink?.Report(diagnostic);
        }
    }

    private void ReportWarning(string message)
    {
        lock (_diagnosticLock)
            _opt.WarningSink?.Invoke(message);
    }

    private void ValidateAnchors(bool singleFile)
    {
        if (_opt.DiagnosticSink is null)
            return;

        var anchors = _symbolIndex.Members.Values
            .Where(member => member.Kind is "M" or "P" or "F" or "E")
            .Select(member => new
            {
                Scope = singleFile
                    ? string.Empty
                    : ContainingTypeId(member.Id),
                Anchor = IdToAnchor(member.Id),
                member.Name
            });

        if (singleFile)
        {
            anchors = anchors.Concat(
                GetTypes().Select(member => new
                {
                    Scope = string.Empty,
                    Anchor = HeadingSlug(
                        _signatureRenderer.RenderTypeName(member.Id)),
                    member.Name
                }));
        }

        foreach (var group in anchors
            .GroupBy(item => new { item.Scope, item.Anchor })
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key.Scope, StringComparer.Ordinal)
            .ThenBy(group => group.Key.Anchor, StringComparer.Ordinal))
        {
            var members = group
                .Select(item => item.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            ReportDiagnostic(new Xml2DocDiagnostic(
                DiagnosticIds.DuplicateAnchor,
                DiagnosticSeverity.Warning,
                $"Anchor '{group.Key.Anchor}' is generated by multiple members: " +
                string.Join(", ", members) + ".",
                MemberId: members[0]));
        }
    }

    private static string ContainingTypeId(string memberId)
    {
        var parameterList = memberId.IndexOf('(');
        var head = parameterList >= 0
            ? memberId.Substring(0, parameterList)
            : memberId;
        var separator = head.LastIndexOf('.');
        return separator >= 0 ? head.Substring(0, separator) : head;
    }

    private AutoLinkContext BuildAutoLinkContext(bool singleFile)
    {
        if (!_opt.AutoLink)
            return new AutoLinkContext(Array.Empty<AutoLinkTarget>());

        var targets = _symbolIndex.Members.Values
            .Where(member => member.Kind is "T" or "M" or "P" or "F" or "E")
            .Select(member =>
            {
                var cref = member.Kind + ":" + member.Id;
                var link = _linkResolver.Resolve(
                    cref,
                    new LinkContext(
                        CurrentTypeId: null,
                        SingleFile: singleFile,
                        BasePath: null));
                return new AutoLinkTarget(link.Label, link.Href);
            })
            .OrderByDescending(target => target.Label.Length)
            .ThenBy(target => target.Label, StringComparer.Ordinal)
            .ToArray();

        return new AutoLinkContext(targets);
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
    private string IdToAnchor(string id) =>
        _anchorGenerator.GenerateMemberAnchor(id);

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
                        if (!string.IsNullOrWhiteSpace(href))
                            text.Append($"[{e.Value}]({href})");
                        else
                        {
                            var langword = (string?)e.Attribute("langword");
                            text.Append(!string.IsNullOrWhiteSpace(langword)
                                ? $"`{langword}`"
                                : e.Value);
                        }
                    }
                    break;
                case XElement e when e.Name.LocalName is "paramref" or "typeparamref":
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

        var markdown = sbOut.ToString().Trim('\n');
        if (!_opt.AutoLink)
            return markdown;

        return _autoLinker.Apply(
            markdown,
            _singleFileMode
                ? _singleFileAutoLinkContext
                : _perTypeAutoLinkContext);
    }

    /// <summary>
    /// Renders a member (or overload bullet) including summary, parameters, returns, exceptions, examples, see‑also links, and a stable anchor.
    /// </summary>
    /// <param name="m">Member to render.</param>
    /// <param name="sb">Destination builder.</param>
    /// <param name="asOverload">True to render as a bullet under an overload group; false for a full section.</param>
    private void RenderMember(XMember m, StringBuilder sb, bool asOverload)
    {
        // Resolve inheritance into a per-render copy. Mutating the source model would
        // make an implementation rendered earlier eligible as a later inheritance
        // candidate, causing output to depend on member ordering.
        var memberElement = new XElement(m.Element);
        var inherit = memberElement.Element("inheritdoc");
        if (inherit != null)
        {
            var target = InheritDocResolver.ResolveInheritedMember(_model, m);
            if (target != null)
                InheritDocResolver.MergeInheritedContent(memberElement, target);
            else
            {
                var message = $"Unable to resolve <inheritdoc /> for '{m.Name}'.";
                ReportDiagnostic(new Xml2DocDiagnostic(
                    DiagnosticIds.UnresolvedInheritDoc,
                    DiagnosticSeverity.Warning,
                    message,
                    MemberId: m.Name));
                ReportWarning(message);
            }
        }

        sb.AppendLine($"<a id=\"{IdToAnchor(m.Id)}\"></a>");

        if (asOverload)
            sb.AppendLine($"- `{_signatureRenderer.RenderMemberHeader(m, _signatureStyle)}`");
        else
            sb.AppendLine($"## {_signatureRenderer.RenderMemberHeader(m, _signatureStyle)}");

        var ms = NormalizeXmlToMarkdown(memberElement.Element("summary"));
        if (string.IsNullOrWhiteSpace(ms))
            ReportMissingSummary(m);
        if (!string.IsNullOrWhiteSpace(ms))
            sb.AppendLine(ms);

        var remarks = NormalizeXmlToMarkdown(memberElement.Element("remarks"));
        if (!string.IsNullOrWhiteSpace(remarks))
        {
            sb.AppendLine();
            sb.AppendLine("**Remarks**");
            sb.AppendLine();
            sb.AppendLine(remarks);
        }

        var typeParameters = memberElement.Elements("typeparam").ToList();
        if (typeParameters.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("**Type parameters**");
            foreach (var typeParameter in typeParameters)
            {
                var name = (string?)typeParameter.Attribute("name") ?? "";
                var text = NormalizeXmlToMarkdown(typeParameter);
                sb.AppendLine($"- `{name}` — {text}");
            }
        }

        var ps = memberElement.Elements("param").ToList();
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

        var ret = memberElement.Element("returns");
        if (ret != null)
        {
            sb.AppendLine();
            sb.AppendLine("**Returns**");
            sb.AppendLine();
            sb.AppendLine(NormalizeXmlToMarkdown(ret));
        }

        var value = memberElement.Element("value");
        if (value != null)
        {
            sb.AppendLine();
            sb.AppendLine("**Value**");
            sb.AppendLine();
            sb.AppendLine(NormalizeXmlToMarkdown(value));
        }

        var exTags = memberElement.Elements("exception").ToList();
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

        var examples = memberElement.Elements("example").ToList();
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

        var memberSeeAlsos = memberElement.Elements("seealso").ToList();
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
