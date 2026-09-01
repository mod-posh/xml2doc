using System;
#if NETSTANDARD2_0
using Xml2Doc.Core.Compat;
#endif
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections.Concurrent;
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
using Xml2Doc.Core.Paths;

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
    private const char MarkdownListItemMarker = '\uE000';
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
    private readonly MetadataCollection _callerMetadata;
    private readonly IDocumentPathResolver _documentPathResolver;
    private readonly Lazy<DocumentPlan> _documentPlan;
    private readonly HashSet<string> _reportedDiagnostics =
        new(StringComparer.Ordinal);
    private readonly object _diagnosticLock = new();
    private readonly ConcurrentDictionary<string, AutoLinkContext>
        _perDocumentAutoLinkContexts = new(StringComparer.Ordinal);
    private readonly AutoLinkContext _singleFileAutoLinkContext;

    /// <summary>
    /// Internal link target selection mode for cref resolution (multi‑file vs single‑file).
    /// </summary>
    private enum LinkMode { PerTypeFiles, InDocumentAnchors }
    private LinkMode _linkMode = LinkMode.PerTypeFiles;

    private readonly ILinkResolver _linkResolver;
    private bool _singleFileMode;

    internal bool PrunesStaleFiles => _opt.PruneStaleFiles;

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
        _callerMetadata = _opt.Metadata is null
            ? MetadataCollection.Empty
            : new MetadataCollection(_opt.Metadata);
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
        if (_callerMetadata.Count > 0 &&
            !string.IsNullOrWhiteSpace(_opt.FrontMatterPath))
        {
            throw new ArgumentException(
                "Metadata cannot be combined with FrontMatterPath.",
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
        _documentPathResolver = _opt.DocumentPathResolver ??
            new BuiltInDocumentPathResolver(
                _opt.Layout,
                _opt.TrimRootNamespaceInFileNames
                    ? _opt.RootNamespaceToTrim
                    : null);
        _documentPlan = new Lazy<DocumentPlan>(
            CreateDocumentPlanWithDiagnostics);
        var externalSymbolResolver = _opt.ExternalSymbolResolver ??
            (!string.IsNullOrWhiteSpace(_opt.ExternalDocs)
                ? new BaseUrlExternalSymbolResolver(_opt.ExternalDocs!)
                : null);
        _linkResolver = new DefaultLinkResolver(
            labelFromCref: _signatureRenderer.RenderCrefLabel,
            idToAnchor: IdToAnchor,
            typeHref: ResolveTypeHref,
            headingSlug: HeadingSlug,
            isKnownCref: IsKnownCref,
            linkPolicy: _opt.LinkPolicy,
            externalSymbolResolver: externalSymbolResolver,
            unresolvedCref: ReportUnresolvedCref);
        _singleFileAutoLinkContext = BuildAutoLinkContext(
            singleFile: true,
            currentDocumentId: null);
    }

    /// <summary>Gets the immutable authoritative multi-document path plan.</summary>
    public DocumentPlan DocumentPlan => _documentPlan.Value;

    private DocumentPlan CreateDocumentPlanWithDiagnostics()
    {
        try
        {
            return CreateDocumentPlan();
        }
        catch (DocumentPathException exception)
        {
            _opt.DiagnosticSink?.Report(new Xml2DocDiagnostic(
                exception.DiagnosticCode,
                DiagnosticSeverity.Error,
                exception.Message));
            throw;
        }
    }

    private DocumentPlan CreateDocumentPlan()
    {
        var types = GetTypes()
            .OrderBy(type => type.Id, StringComparer.Ordinal)
            .ToList();
        var documents = new List<DocumentPathContext>();

        foreach (var type in types)
        {
            var descriptor = CreateTypeDocumentDescriptor(type);
            documents.Add(new DocumentPathContext(
                descriptor,
                FileNameForPerType(type.Id),
                FileNameFor(descriptor.Symbol!, _opt.FileNameMode)));
        }

        if (_opt.GenerateIndex)
        {
            documents.Add(new DocumentPathContext(
                new DocumentDescriptor(
                    TemplateDocumentKind.Index,
                    "xml2doc:index"),
                "index.md",
                "index.md"));
        }

        if (_opt.EmitNamespaceIndex)
        {
            foreach (var ns in GetDocumentNamespaces(types))
            {
                var fileSafe = SafeNamespaceFileName(ns);
                documents.Add(new DocumentPathContext(
                    new DocumentDescriptor(
                        TemplateDocumentKind.NamespaceIndex,
                        $"N:{ns}",
                        ns == "(global)" ? null : ns),
                    $"namespaces/{fileSafe}.md",
                    fileSafe + ".md"));
            }

            documents.Add(new DocumentPathContext(
                new DocumentDescriptor(
                    TemplateDocumentKind.NamespaceOverview,
                    "xml2doc:namespaces"),
                "namespaces.md",
                "namespaces.md"));
        }

        return DocumentPlan.Create(documents, _documentPathResolver);
    }

    private static IReadOnlyList<string> GetDocumentNamespaces(
        IEnumerable<XMember> types) =>
        types
            .Select(type =>
            {
                var lastDot = type.Id.LastIndexOf('.');
                return lastDot > 0
                    ? type.Id.Substring(0, lastDot)
                    : "(global)";
            })
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

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
        _ = DocumentPlan;
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
            var typeEntries = types
                .Select(type => DocumentPlan.Get(type.Name))
                .ToArray();
            var typeFiles = typeEntries
                .Select(entry => CombineOutputPath(outDir, entry.Path))
                .ToArray();
            var typeWasWritten = new bool[types.Count];

            void RenderTypeAt(int index)
            {
                var type = types[index];
                var entry = typeEntries[index];
                try
                {
                    typeWasWritten[index] = WriteMarkdownFileIfChanged(
                        typeFiles[index],
                        NormalizeLineEndings(ApplyTemplate(
                            RenderType(
                                type,
                                entry.Document.DocumentId,
                                includeHeader: true),
                            _signatureRenderer.RenderTypeName(type.Id),
                            entry.Document,
                            entry.Path)));
                }
                finally
                {
                    _perDocumentAutoLinkContexts.TryRemove(
                        entry.Document.DocumentId,
                        out _);
                }
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
            {
                var entry = DocumentPlan.Get("xml2doc:index");
                RecordWriteResult(
                    CombineOutputPath(outDir, entry.Path),
                    NormalizeLineEndings(ApplyTemplate(
                        RenderIndex(
                            types,
                            useAnchors: false,
                            entry.Document.DocumentId),
                        "API Reference",
                        entry.Document,
                        entry.Path)),
                    writtenFiles,
                    skippedFiles);
            }

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

                foreach (var kv in nsMap.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    var ns = kv.Key;
                    var entry = DocumentPlan.Get($"N:{ns}");
                    var nsFile = CombineOutputPath(outDir, entry.Path);

                    var sbNs = new StringBuilder();
                    sbNs.AppendLine($"# {ns}");
                    foreach (var t in kv.Value.OrderBy(t => t.Id, StringComparer.Ordinal))
                    {
                        var shortName = _signatureRenderer.RenderTypeName(t.Id);
                        var link = DocumentPlan.GetRelativeLink(
                            entry.Document.DocumentId,
                            t.Name);
                        sbNs.AppendLine($"- [{shortName}]({link})");
                    }
                    RecordWriteResult(
                        nsFile,
                        NormalizeLineEndings(ApplyTemplate(
                            sbNs.ToString(),
                            ns,
                            entry.Document,
                            entry.Path)),
                        writtenFiles,
                        skippedFiles);
                }

                var overviewEntry = DocumentPlan.Get("xml2doc:namespaces");
                var nsIndex = new StringBuilder();
                nsIndex.AppendLine("# Namespaces");
                foreach (var ns in nsMap.Keys.OrderBy(s => s, StringComparer.Ordinal))
                {
                    var link = DocumentPlan.GetRelativeLink(
                        overviewEntry.Document.DocumentId,
                        $"N:{ns}");
                    nsIndex.AppendLine($"- [{ns}]({link})");
                }
                RecordWriteResult(
                    CombineOutputPath(outDir, overviewEntry.Path),
                    NormalizeLineEndings(ApplyTemplate(
                        nsIndex.ToString(),
                        "Namespaces",
                        overviewEntry.Document,
                        overviewEntry.Path)),
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

    internal IReadOnlyList<string> PlanPrunedFiles(
        string outDir,
        IReadOnlyList<string> plannedFiles)
    {
        if (!_opt.PruneStaleFiles)
            return Array.Empty<string>();

        var location = OutputManifestLocation.Create(
            outDir,
            _opt.ManifestIdentity!);
        var generatedFiles = GetOutputRootRelativePaths(
            location.OutputRoot,
            plannedFiles);
        var lifecyclePlan = OutputLifecycleExecutor
            .ExecuteAfterSuccessfulGeneration(
                location,
                generatedFiles,
                dryRun: true);
        return lifecyclePlan.FilesToDelete
            .Select(path => CombineOutputPath(location.OutputRoot, path))
            .ToArray();
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
                NormalizeLineEndings(BuildSingleFileContent(
                    Path.GetFileName(Path.GetFullPath(outPath)))),
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
        NormalizeLineEndings(BuildSingleFileContent(outputPath: null));

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

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
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
    private string BuildSingleFileContent(string? outputPath)
    {
        ValidateAnchors(singleFile: true);
        var prev = _linkMode;
        _linkMode = LinkMode.InDocumentAnchors;
        try
        {
            var types = GetTypes()
                .OrderBy(t => t.Id, StringComparer.Ordinal)
                .ToList();
            var sb = new StringBuilder();

            sb.Append(RenderIndex(
                types,
                useAnchors: true,
                currentDocumentId: null));
            sb.AppendLine();

            for (int i = 0; i < types.Count; i++)
            {
                var t = types[i];
                var typeDisplay = _signatureRenderer.RenderTypeName(t.Id);
                sb.AppendLine($"<a id=\"{HeadingSlug(typeDisplay)}\"></a>");
                sb.AppendLine($"# {typeDisplay}");
                sb.AppendLine();
                sb.Append(RenderType(
                    t,
                    currentDocumentId: null,
                    includeHeader: false));
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
                new DocumentDescriptor(
                    TemplateDocumentKind.SingleFile,
                    "xml2doc:single-file"),
                outputPath);
        }
        finally
        {
            _linkMode = prev;
        }
    }

    private string ApplyTemplate(
        string content,
        string? title,
        DocumentDescriptor document,
        string? outputPath)
    {
        var metadata = CreateDocumentMetadata(document, outputPath);
        var context = new TemplateRenderContext(
            content,
            title,
            document.Kind)
        {
            Document = document,
            OutputPath = outputPath,
            Metadata = metadata
        };
        var rendered = _templateRenderer.Render(context);
        var frontMatter = _opt.FrontMatter?.Invoke(context);

        // ADR-014 preserves provider-only output; authoritative document values participate
        // in precedence only when generic caller metadata enables the merged metadata path.
        if (_callerMetadata.Count > 0)
            frontMatter = MergeCallerMetadata(frontMatter, document, outputPath);

        if (frontMatter is null || frontMatter.Count == 0)
            return rendered;

        return YamlFrontMatter.Serialize(frontMatter) + "\n" + rendered;
    }

    private MetadataCollection CreateDocumentMetadata(
        DocumentDescriptor document,
        string? outputPath)
    {
        var values = _callerMetadata.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);
        AddDocumentMetadata(values, document, outputPath);
        return new MetadataCollection(values);
    }

    private MetadataCollection MergeCallerMetadata(
        IReadOnlyDictionary<string, object?>? frontMatter,
        DocumentDescriptor document,
        string? outputPath)
    {
        var values = _callerMetadata.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);
        if (frontMatter is not null)
        {
            foreach (var pair in frontMatter)
                values[pair.Key] = pair.Value;
        }

        AddDocumentMetadata(values, document, outputPath);
        return new MetadataCollection(values);
    }

    private static void AddDocumentMetadata(
        IDictionary<string, object?> values,
        DocumentDescriptor document,
        string? outputPath)
    {
        values["documentId"] = document.DocumentId;
        values["documentKind"] = document.Kind.ToString().ToLowerInvariant();
        values["namespace"] = document.Namespace;
        values["outputPath"] = outputPath;
        values["symbol"] = document.Symbol;
    }

    // === Core rendering ===

    /// <summary>
    /// Enumerates all documented types (<c>T:</c> members only).
    /// </summary>
    private IEnumerable<XMember> GetTypes() =>
        _symbolIndex.Types;

    private static DocumentDescriptor CreateTypeDocumentDescriptor(XMember type)
    {
        var lastDot = type.Id.LastIndexOf('.');
        var @namespace = lastDot > 0
            ? type.Id.Substring(0, lastDot)
            : null;
        var symbol = lastDot >= 0
            ? type.Id.Substring(lastDot + 1)
            : type.Id;

        return new DocumentDescriptor(
            TemplateDocumentKind.Type,
            type.Name,
            @namespace,
            symbol);
    }

    /// <summary>
    /// Builds a type index linking either to per‑type files or heading anchors (single‑file mode).
    /// </summary>
    /// <param name="types">Sequence of type members.</param>
    /// <param name="useAnchors">True to link to in‑document anchors; false for per‑type files.</param>
    /// <param name="currentDocumentId">Current planned document identity for relative links.</param>
    private string RenderIndex(
        IEnumerable<XMember> types,
        bool useAnchors,
        string? currentDocumentId)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# API Reference");
        foreach (var t in types)
        {
            var shortName = _signatureRenderer.RenderTypeName(t.Id);
            var link = useAnchors
                ? $"#{HeadingSlug(shortName)}"
                : ResolveTypeHref(t.Name, currentDocumentId);
            sb.AppendLine($"- [{shortName}]({link})");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Renders a single type (summary, remarks, examples, see‑also, optional member TOC, members grouped by overload).
    /// </summary>
    /// <param name="type">Type (<c>T:</c>) member.</param>
    /// <param name="currentDocumentId">Current planned document identity for relative links.</param>
    /// <param name="includeHeader">Emit a top-level heading when true.</param>
    private string RenderType(
        XMember type,
        string? currentDocumentId,
        bool includeHeader = true)
    {
        var sb = new StringBuilder();
        var typeDisplay = _signatureRenderer.RenderTypeName(type.Id);

        if (includeHeader)
        {
            sb.AppendLine($"# {typeDisplay}");
            sb.AppendLine();
        }

        var summary = NormalizeXmlToMarkdown(
            type.Element.Element("summary"),
            currentDocumentId: currentDocumentId);
        if (string.IsNullOrWhiteSpace(summary))
            ReportMissingSummary(type);
        if (!string.IsNullOrWhiteSpace(summary))
        {
            sb.AppendLine(summary);
            sb.AppendLine();
        }

        var remarks = NormalizeXmlToMarkdown(
            type.Element.Element("remarks"),
            currentDocumentId: currentDocumentId);
        if (!string.IsNullOrWhiteSpace(remarks))
        {
            sb.AppendLine("**Remarks**");
            sb.AppendLine();
            sb.AppendLine(remarks);
            sb.AppendLine();
        }

        foreach (var ex in type.Element.Elements("example"))
        {
            var exText = NormalizeXmlToMarkdown(
                ex,
                preferCodeBlocks: true,
                currentDocumentId: currentDocumentId);
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
                var link = SeeAlsoToMarkdown(sa, currentDocumentId);
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
                    RenderMember(
                        mem,
                        sb,
                        currentDocumentId,
                        asOverload: true);
                sb.AppendLine();
            }
            else
            {
                RenderMember(
                    g.First(),
                    sb,
                    currentDocumentId,
                    asOverload: false);
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
        return DocumentPlan
            .Select(entry => CombineOutputPath(root, entry.Path))
            .ToArray();
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

                return fullPath
                    .Substring(rootWithSeparator.Length)
                    .Replace('\\', '/');
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
    private string CrefToMarkdown(
        string? cref,
        string? currentDocumentId)
    {
        var sb = new StringBuilder();
        CrefToMarkdown(sb, cref, currentDocumentId);
        return sb.ToString();
    }

    /// <summary>
    /// Appends a Markdown link for a cref to a <see cref="StringBuilder"/> using the configured resolver.
    /// </summary>
    private void CrefToMarkdown(
        StringBuilder sb,
        string? cref,
        string? currentDocumentId)
    {
        var safeCref = string.IsNullOrWhiteSpace(cref) ? string.Empty : cref!;
        var link = _linkResolver.Resolve(
            safeCref,
            new LinkContext(
                CurrentTypeId: currentDocumentId,
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

    private AutoLinkContext BuildAutoLinkContext(
        bool singleFile,
        string? currentDocumentId)
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
                        CurrentTypeId: currentDocumentId,
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
    private string SeeAlsoToMarkdown(
        XElement sa,
        string? currentDocumentId)
    {
        var cref = (string?)sa.Attribute("cref");
        if (!string.IsNullOrWhiteSpace(cref))
            return CrefToMarkdown(cref, currentDocumentId);
        var href = (string?)sa.Attribute("href");
        if (!string.IsNullOrWhiteSpace(href))
            return $"[{sa.Value}]({href})";
        return NormalizeXmlToMarkdown(
            sa,
            currentDocumentId: currentDocumentId);
    }

    /// <summary>
    /// Produces the per‑type output filename for a cref (normalizes nested type separators then applies renderer rules).
    /// </summary>
    private string ResolveTypeHref(
        string typeCref,
        string? currentDocumentId)
    {
        var id = typeCref.StartsWith("T:") ? typeCref.Substring(2) : typeCref;
        id = id.Replace('+', '.');
        var documentId = "T:" + id;
        string targetPath;
        if (DocumentPlan.TryGet(documentId, out var target))
        {
            targetPath = target!.Path;
        }
        else
        {
            var lastDot = id.LastIndexOf('.');
            var descriptor = new DocumentDescriptor(
                TemplateDocumentKind.Type,
                documentId,
                lastDot > 0 ? id.Substring(0, lastDot) : null,
                lastDot >= 0 ? id.Substring(lastDot + 1) : id);
            var context = new DocumentPathContext(
                descriptor,
                FileNameForPerType(id),
                FileNameFor(descriptor.Symbol!, _opt.FileNameMode));
            targetPath = _opt.DocumentPathResolver is null
                ? _documentPathResolver.GetPath(context)
                : context.DefaultPath;
            DocumentPlan.ValidateLogicalPath(targetPath, documentId);
        }

        if (string.IsNullOrWhiteSpace(currentDocumentId) ||
            !DocumentPlan.TryGet(currentDocumentId!, out var source))
        {
            return targetPath;
        }

        return DocumentPlan.GetRelativePath(source!.Path, targetPath);
    }

    // === XML → Markdown normalization ===

    /// <summary>
    /// Normalizes an XML documentation element (summary, remarks, example, param, see, code) to Markdown with paragraph preservation.
    /// </summary>
    /// <param name="element">XML element or null.</param>
    /// <param name="preferCodeBlocks">True to prefer fenced blocks for multi‑line code/examples.</param>
    /// <param name="preserveListItemMarkers">Preserves internal bullet-list continuation markers.</param>
    /// <param name="currentDocumentId">Current planned document identity for relative links.</param>
    /// <returns>Markdown string (empty if element is null).</returns>
    private string NormalizeXmlToMarkdown(
        XElement? element,
        bool preferCodeBlocks = false,
        bool preserveListItemMarkers = false,
        string? currentDocumentId = null)
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
                        text.Append(CrefToMarkdown(cref, currentDocumentId));
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
                        text.AppendLine()
                        .AppendLine(NormalizeXmlToMarkdown(
                            e,
                            preserveListItemMarkers: true,
                            currentDocumentId: currentDocumentId))
                        .AppendLine();
                    break;
                case XElement e when
                    e.Name.LocalName == "list" &&
                    string.Equals(
                        (string?)e.Attribute("type"),
                        "bullet",
                        StringComparison.OrdinalIgnoreCase):
                    text.AppendLine()
                        .AppendLine(RenderBulletList(e, currentDocumentId))
                        .AppendLine();
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
        var listItems = new bool[lines.Length];
        var inFence = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Length > 0 && line[0] == MarkdownListItemMarker)
            {
                line = line.Substring(1);
                listItems[i] = true;
            }
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

            if (listItems[i])
            {
                cleaned[i] = line.TrimEnd();
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
        bool prevWasListItem = false;

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
                prevWasListItem = false;
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
                prevWasListItem = false;
            }
            else
            {
                var isListItem = listItems[i];
#if NETSTANDARD2_0
                if (!prevWasBlank && sbOut.Length > 0 && sbOut[sbOut.Length - 1] != '\n')
                    sbOut.Append(isListItem || prevWasListItem ? '\n' : ' ');
#else
                if (!prevWasBlank && sbOut.Length > 0 && sbOut[^1] != '\n')
                    sbOut.Append(isListItem || prevWasListItem ? '\n' : ' ');
#endif
                if (isListItem && preserveListItemMarkers)
                    sbOut.Append(MarkdownListItemMarker);
                sbOut.Append(line);
                prevWasBlank = false;
                prevWasListItem = isListItem;
            }
        }

        var markdown = sbOut.ToString().Trim('\n');
        if (!_opt.AutoLink || preserveListItemMarkers)
            return markdown;

        var autoLinkContext = _singleFileMode
            ? _singleFileAutoLinkContext
            : string.IsNullOrWhiteSpace(currentDocumentId)
                ? BuildAutoLinkContext(
                    singleFile: false,
                    currentDocumentId: null)
                : _perDocumentAutoLinkContexts.GetOrAdd(
                    currentDocumentId!,
                    id => BuildAutoLinkContext(
                        singleFile: false,
                        currentDocumentId: id));
        return _autoLinker.Apply(markdown, autoLinkContext);
    }

    private string RenderBulletList(
        XElement list,
        string? currentDocumentId)
    {
        var renderedItems = list.Elements("item")
            .Select(item => item.Element("description") ?? item)
            .Select(description => NormalizeXmlToMarkdown(
                description,
                currentDocumentId: currentDocumentId))
            .Where(description => !string.IsNullOrWhiteSpace(description))
            .Select(description => description
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Trim('\n')
                .Split('\n'));

        var renderedList = new StringBuilder();
        foreach (var lines in renderedItems)
        {
            var firstLineIsFence = lines[0].TrimStart().StartsWith("```", StringComparison.Ordinal);

            if (renderedList.Length > 0)
                renderedList.Append('\n');

            renderedList.Append(MarkdownListItemMarker).Append('-');
            if (!firstLineIsFence)
                renderedList.Append(' ').Append(lines[0]);

            var continuationStart = firstLineIsFence ? 0 : 1;
            for (var index = continuationStart; index < lines.Length; index++)
            {
                renderedList.Append('\n').Append(MarkdownListItemMarker);
                if (!string.IsNullOrEmpty(lines[index]))
                    renderedList.Append("  ").Append(lines[index]);
            }
        }

        return renderedList.ToString();
    }

    /// <summary>
    /// Renders a member (or overload bullet) including summary, parameters, returns, exceptions, examples, see‑also links, and a stable anchor.
    /// </summary>
    /// <param name="m">Member to render.</param>
    /// <param name="sb">Destination builder.</param>
    /// <param name="currentDocumentId">Current planned document identity for relative links.</param>
    /// <param name="asOverload">True to render as a bullet under an overload group; false for a full section.</param>
    private void RenderMember(
        XMember m,
        StringBuilder sb,
        string? currentDocumentId,
        bool asOverload)
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

        var ms = NormalizeXmlToMarkdown(
            memberElement.Element("summary"),
            currentDocumentId: currentDocumentId);
        if (string.IsNullOrWhiteSpace(ms))
            ReportMissingSummary(m);
        if (!string.IsNullOrWhiteSpace(ms))
            sb.AppendLine(ms);

        var remarks = NormalizeXmlToMarkdown(
            memberElement.Element("remarks"),
            currentDocumentId: currentDocumentId);
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
                var text = NormalizeXmlToMarkdown(
                    typeParameter,
                    currentDocumentId: currentDocumentId);
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
                var text = NormalizeXmlToMarkdown(
                    p,
                    currentDocumentId: currentDocumentId);
                sb.AppendLine($"- `{name}` — {text}");
            }
        }

        var ret = memberElement.Element("returns");
        if (ret != null)
        {
            sb.AppendLine();
            sb.AppendLine("**Returns**");
            sb.AppendLine();
            sb.AppendLine(NormalizeXmlToMarkdown(
                ret,
                currentDocumentId: currentDocumentId));
        }

        var value = memberElement.Element("value");
        if (value != null)
        {
            sb.AppendLine();
            sb.AppendLine("**Value**");
            sb.AppendLine();
            sb.AppendLine(NormalizeXmlToMarkdown(
                value,
                currentDocumentId: currentDocumentId));
        }

        var exTags = memberElement.Elements("exception").ToList();
        if (exTags.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("**Exceptions**");
            foreach (var e in exTags)
            {
                var cref = (string?)e.Attribute("cref");
                var desc = NormalizeXmlToMarkdown(
                    e,
                    currentDocumentId: currentDocumentId);
                var link = CrefToMarkdown(cref, currentDocumentId);
                sb.AppendLine($"- {link} — {desc}");
            }
        }

        var examples = memberElement.Elements("example").ToList();
        if (examples.Count > 0)
        {
            sb.AppendLine();
            foreach (var ex in examples)
            {
                var exMd = NormalizeXmlToMarkdown(
                    ex,
                    preferCodeBlocks: true,
                    currentDocumentId: currentDocumentId);
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
                var link = SeeAlsoToMarkdown(sa, currentDocumentId);
                if (!string.IsNullOrWhiteSpace(link))
                    sb.AppendLine($"- {link}");
            }
        }

        sb.AppendLine();
    }
}
