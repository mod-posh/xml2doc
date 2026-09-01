using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using Xml2Doc.Core;
using Xml2Doc.Core.Paths;

namespace Xml2Doc.MSBuild;

/// <summary>
/// MSBuild task that renders one deterministic Markdown output from multiple XML documentation files.
/// </summary>
/// <remarks>
/// All primary XML inputs participate in the rendered model. Inputs are normalized, de-duplicated,
/// and ordered canonically before <see cref="Core.Models.Xml2Doc.LoadAggregate(IEnumerable{string})"/>
/// is called. Reference XML remains lookup-only and does not generate pages.
/// </remarks>
public sealed class GenerateMarkdownFromXmlDocs : Microsoft.Build.Utilities.Task
{
    /// <summary>Primary XML documentation files that participate in the aggregate output.</summary>
    [Required]
    public ITaskItem[] XmlPaths { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>Reference-only XML documentation used for inheritance lookup.</summary>
    public ITaskItem[] ReferenceXmlPaths { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>Directory for per-type Markdown output.</summary>
    public string? OutputDirectory { get; set; }

    /// <summary>Whether to render one consolidated Markdown file.</summary>
    public bool SingleFile { get; set; }

    /// <summary>Output path used when <see cref="SingleFile"/> is true.</summary>
    public string? OutputFile { get; set; }

    /// <summary>File naming mode: <c>verbatim</c> or <c>clean</c>.</summary>
    public string FileNameMode { get; set; } = "verbatim";

    /// <summary>Optional root namespace trimmed from display names.</summary>
    public string? RootNamespaceToTrim { get; set; }

    /// <summary>Whether to trim the root namespace from generated file names.</summary>
    public bool TrimRootNamespaceInFileNames { get; set; }

    /// <summary>Language identifier used for fenced code blocks.</summary>
    public string CodeBlockLanguage { get; set; } = "csharp";

    /// <summary>Optional deterministic JSON report path.</summary>
    public string? ReportPath { get; set; }

    /// <summary>Simulates generation without writing Markdown.</summary>
    public bool DryRun { get; set; }

    /// <summary>Whether to emit a table of contents.</summary>
    public bool EmitToc { get; set; }

    /// <summary>Whether to emit a namespace index.</summary>
    public bool EmitNamespaceIndex { get; set; }

    /// <summary>Whether per-type output includes the repository-owned <c>index.md</c>.</summary>
    public bool GenerateIndex { get; set; } = true;

    /// <summary>Whether to remove stale files owned by this aggregate invocation.</summary>
    public bool PruneStaleFiles { get; set; }

    /// <summary>Stable ownership identity required when pruning is enabled.</summary>
    public string? ManifestIdentity { get; set; }

    /// <summary>Markdown line endings: <c>lf</c>, <c>crlf</c>, or <c>native</c>.</summary>
    public string LineEndings { get; set; } = "lf";

    /// <summary>Multi-document layout: <c>flat</c> or <c>namespace-folders</c>.</summary>
    public string Layout { get; set; } = "flat";

    /// <summary>Whether generated links use only base file names.</summary>
    public bool BasenameOnly { get; set; }

    /// <summary>Maximum parallelism used by per-type rendering.</summary>
    public int ParallelDegree { get; set; } = 1;

    /// <summary>Anchor algorithm: default, github, gfm, or kramdown.</summary>
    public string? AnchorAlgorithm { get; set; }

    /// <summary>Optional JSON object containing generic scalar/list caller metadata.</summary>
    public string? MetadataFile { get; set; }

    /// <summary>Optional externally-computed aggregate fingerprint included in the report.</summary>
    public string? Fingerprint { get; set; }

    /// <summary>Adds a timestamp to the report when true.</summary>
    public bool IncludeTimestampInReport { get; set; }

    /// <summary>Generated Markdown files.</summary>
    [Output]
    public ITaskItem[] GeneratedFiles { get; private set; } = Array.Empty<ITaskItem>();

    /// <summary>Full report path when a report was written.</summary>
    [Output]
    public string? ReportPathOut { get; private set; }

    /// <summary>Whether Markdown files were physically written.</summary>
    [Output]
    public bool DidWork { get; private set; }

    /// <inheritdoc />
    public override bool Execute()
    {
        try
        {
            var comparer = GetPathComparer();
            var xmlPaths = ResolvePaths(XmlPaths, comparer);

            if (xmlPaths.Length == 0)
            {
                Log.LogError("Xml2Doc: aggregate generation requires at least one XML input.");
                return false;
            }

            var missingInputs = xmlPaths.Where(path => !File.Exists(path)).ToArray();
            foreach (var missingPath in missingInputs)
            {
                Log.LogError(
                    $"Xml2Doc: aggregate XML input not found at '{missingPath}'. " +
                    "Ensure every participating project enables GenerateDocumentationFile.");
            }

            if (missingInputs.Length > 0)
                return false;

            if (PruneStaleFiles && SingleFile)
            {
                Log.LogError(
                    "Xml2Doc: PruneStaleFiles=true is only supported for per-type output.");
                return false;
            }

            if (PruneStaleFiles && string.IsNullOrWhiteSpace(ManifestIdentity))
            {
                Log.LogError(
                    "Xml2Doc: ManifestIdentity is required when PruneStaleFiles=true.");
                return false;
            }

            if (!TryResolveLineEndings(out var lineEndingStyle))
                return false;
            if (!TryResolveLayout(out var documentLayout))
                return false;

            string? metadataFull = null;
            MetadataCollection? metadata = null;
            if (!string.IsNullOrWhiteSpace(MetadataFile))
            {
                metadataFull = Path.GetFullPath(MetadataFile!);
                if (!File.Exists(metadataFull))
                {
                    Log.LogError($"Xml2Doc: metadata file not found at '{metadataFull}'.");
                    return false;
                }

                metadata = MetadataCollection.ParseJson(File.ReadAllText(metadataFull));
            }

            var primarySet = new HashSet<string>(xmlPaths, comparer);
            var referenceXmlPaths = ResolvePaths(ReferenceXmlPaths, comparer)
                .Where(path => !primarySet.Contains(path))
                .ToArray();
            var existingReferenceXmlPaths = referenceXmlPaths
                .Where(File.Exists)
                .ToArray();

            foreach (var missingPath in referenceXmlPaths.Except(
                existingReferenceXmlPaths,
                comparer))
            {
                Log.LogWarning($"Xml2Doc: reference XML not found at '{missingPath}'.");
            }

            var model = Core.Models.Xml2Doc.LoadAggregate(xmlPaths);
            model.LoadReferences(existingReferenceXmlPaths);

            var fileNameMode = FileNameMode.Equals(
                    "clean",
                    StringComparison.OrdinalIgnoreCase)
                ? Core.FileNameMode.CleanGenerics
                : Core.FileNameMode.Verbatim;
            var anchorAlgorithm = (AnchorAlgorithm ?? "default").ToLowerInvariant() switch
            {
                "github" => Core.AnchorAlgorithm.Github,
                "gfm" => Core.AnchorAlgorithm.Gfm,
                "kramdown" => Core.AnchorAlgorithm.Kramdown,
                _ => Core.AnchorAlgorithm.Default
            };

            var options = new RendererOptions(
                FileNameMode: fileNameMode,
                RootNamespaceToTrim: string.IsNullOrWhiteSpace(RootNamespaceToTrim)
                    ? null
                    : RootNamespaceToTrim,
                CodeBlockLanguage: string.IsNullOrWhiteSpace(CodeBlockLanguage)
                    ? "csharp"
                    : CodeBlockLanguage,
                TrimRootNamespaceInFileNames: TrimRootNamespaceInFileNames,
                EmitToc: EmitToc,
                EmitNamespaceIndex: EmitNamespaceIndex,
                BasenameOnly: BasenameOnly,
                ParallelDegree: ParallelDegree,
                AnchorAlgorithm: anchorAlgorithm,
                GenerateIndex: GenerateIndex,
                PruneStaleFiles: PruneStaleFiles,
                ManifestIdentity: ManifestIdentity,
                LineEndings: lineEndingStyle,
                WarningSink: warning => Log.LogWarning($"Xml2Doc: {warning}"),
                Metadata: metadata,
                Layout: documentLayout);
            var renderer = new MarkdownRenderer(model, options);

            string? outputDirectory = null;
            string? outputFile = null;

            if (SingleFile)
            {
                if (string.IsNullOrWhiteSpace(OutputFile))
                {
                    Log.LogError("Xml2Doc: SingleFile=true requires OutputFile.");
                    return false;
                }

                outputFile = Path.GetFullPath(OutputFile!);
                var outputFileDirectory = Path.GetDirectoryName(outputFile);
                if (!DryRun &&
                    !string.IsNullOrEmpty(outputFileDirectory) &&
                    !Directory.Exists(outputFileDirectory))
                {
                    Directory.CreateDirectory(outputFileDirectory);
                }

                if (!DryRun)
                {
                    renderer.RenderToSingleFile(outputFile);
                    DidWork = true;
                }

                GeneratedFiles = new ITaskItem[] { new TaskItem(outputFile) };
                Log.LogMessage(
                    MessageImportance.High,
                    $"Xml2Doc aggregate {(DryRun ? "[dry-run] would write" : "wrote")} {outputFile}");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(OutputDirectory))
                {
                    Log.LogError("Xml2Doc: SingleFile=false requires OutputDirectory.");
                    return false;
                }

                outputDirectory = Path.GetFullPath(OutputDirectory!);
                if (!DryRun && !Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);

                if (!DryRun)
                {
                    renderer.RenderToDirectory(outputDirectory);
                    DidWork = true;
                }

                GeneratedFiles = renderer.PlanOutputs(outputDirectory)
                    .Select(path => (ITaskItem)new TaskItem(path))
                    .ToArray();
                Log.LogMessage(
                    MessageImportance.High,
                    $"Xml2Doc aggregate {(DryRun ? "[dry-run] would write" : "wrote")} Markdown files to {outputDirectory}");
            }

            if (!string.IsNullOrWhiteSpace(ReportPath))
                WriteReport(
                    xmlPaths,
                    outputDirectory,
                    outputFile,
                    fileNameMode,
                    lineEndingStyle,
                    documentLayout,
                    metadataFull);

            return !Log.HasLoggedErrors;
        }
        catch (ArgumentException exception)
        {
            return LogExecutionFailure(exception);
        }
        catch (InvalidDataException exception)
        {
            return LogExecutionFailure(exception);
        }
        catch (IOException exception)
        {
            return LogExecutionFailure(exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            return LogExecutionFailure(exception);
        }
        catch (NotSupportedException exception)
        {
            return LogExecutionFailure(exception);
        }
        catch (XmlException exception)
        {
            return LogExecutionFailure(exception);
        }
        catch (JsonException exception)
        {
            return LogExecutionFailure(exception);
        }
        catch (InvalidOperationException exception)
        {
            return LogExecutionFailure(exception);
        }
    }

    private bool LogExecutionFailure(Exception exception)
    {
        Log.LogErrorFromException(exception, showStackTrace: true);
        return false;
    }

    private bool TryResolveLineEndings(out LineEndingStyle lineEndingStyle)
    {
        switch ((LineEndings ?? "lf").ToLowerInvariant())
        {
            case "lf":
                lineEndingStyle = LineEndingStyle.Lf;
                return true;
            case "crlf":
                lineEndingStyle = LineEndingStyle.CrLf;
                return true;
            case "native":
                lineEndingStyle = LineEndingStyle.Native;
                return true;
            default:
                lineEndingStyle = LineEndingStyle.Lf;
                Log.LogError(
                    "Xml2Doc: LineEndings must be one of: lf, crlf, native.");
                return false;
        }
    }

    private void WriteReport(
        string[] xmlPaths,
        string? outputDirectory,
        string? outputFile,
        Core.FileNameMode fileNameMode,
        LineEndingStyle lineEndingStyle,
        DocumentLayout layout,
        string? metadataFile)
    {
        try
        {
            var reportFull = Path.GetFullPath(ReportPath!);
            var reportDirectory = Path.GetDirectoryName(reportFull);
            if (!string.IsNullOrEmpty(reportDirectory) &&
                !Directory.Exists(reportDirectory))
            {
                Directory.CreateDirectory(reportDirectory);
            }

            var report = new ReportModel
            {
                xml = xmlPaths[0],
                xmlInputs = xmlPaths,
                single = SingleFile,
                outputFile = outputFile ?? (SingleFile ? OutputFile : null),
                outputDir = outputDirectory ?? (SingleFile ? null : OutputDirectory),
                files = GeneratedFiles.Select(item => item.ItemSpec).ToArray(),
                options = new ReportOptions
                {
                    fileNameMode = fileNameMode.ToString(),
                    rootNs = string.IsNullOrWhiteSpace(RootNamespaceToTrim)
                        ? null
                        : RootNamespaceToTrim,
                    lang = string.IsNullOrWhiteSpace(CodeBlockLanguage)
                        ? "csharp"
                        : CodeBlockLanguage,
                    parallelDegree = ParallelDegree,
                    generateIndex = GenerateIndex,
                    pruneStaleFiles = PruneStaleFiles,
                    manifestIdentity = ManifestIdentity,
                    lineEndings = lineEndingStyle.ToString(),
                    layout = layout.ToString(),
                    metadataFile = metadataFile
                },
                fingerprint = Fingerprint,
                timestamp = IncludeTimestampInReport
                    ? DateTimeOffset.Now
                    : (DateTimeOffset?)null
            };

            var json = JsonSerializer.Serialize(
                report,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
            File.WriteAllText(reportFull, json);
            ReportPathOut = reportFull;
        }
        catch (ArgumentException exception)
        {
            LogReportWarning(exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            LogReportWarning(exception);
        }
        catch (IOException exception)
        {
            LogReportWarning(exception);
        }
        catch (NotSupportedException exception)
        {
            LogReportWarning(exception);
        }
        catch (JsonException exception)
        {
            LogReportWarning(exception);
        }
    }

    private void LogReportWarning(Exception exception)
    {
        Log.LogWarning(
            $"Xml2Doc: failed to write aggregate report '{ReportPath}': {exception.Message}");
    }

    private bool TryResolveLayout(out DocumentLayout layout)
    {
        switch ((Layout ?? "flat").ToLowerInvariant())
        {
            case "flat":
                layout = DocumentLayout.Flat;
                return true;
            case "namespace-folders":
                layout = DocumentLayout.NamespaceFolders;
                return true;
            default:
                layout = DocumentLayout.Flat;
                Log.LogError(
                    "Xml2Doc: Layout must be one of: flat, namespace-folders.");
                return false;
        }
    }

    private static string[] ResolvePaths(
        IEnumerable<ITaskItem> items,
        StringComparer comparer)
        => items
            .Select(item =>
            {
                var fullPath = item.GetMetadata("FullPath");
                return string.IsNullOrWhiteSpace(fullPath)
                    ? item.ItemSpec
                    : fullPath;
            })
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .OrderBy(path => path, comparer)
            .ThenBy(path => path, StringComparer.Ordinal)
            .Distinct(comparer)
            .ToArray();

    private static StringComparer GetPathComparer()
        => Path.DirectorySeparatorChar == '\\'
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    private sealed class ReportModel
    {
        public string xml { get; set; } = string.Empty;
        public string[] xmlInputs { get; set; } = Array.Empty<string>();
        public bool single { get; set; }
        public string? outputFile { get; set; }
        public string? outputDir { get; set; }
        public string[] files { get; set; } = Array.Empty<string>();
        public ReportOptions options { get; set; } = new();
        public string? fingerprint { get; set; }
        public DateTimeOffset? timestamp { get; set; }
    }

    private sealed class ReportOptions
    {
        public string fileNameMode { get; set; } = "Verbatim";
        public string? rootNs { get; set; }
        public string? lang { get; set; }
        public int parallelDegree { get; set; }
        public bool generateIndex { get; set; }
        public bool pruneStaleFiles { get; set; }
        public string? manifestIdentity { get; set; }
        public string? lineEndings { get; set; }
        public string? layout { get; set; }
        public string? metadataFile { get; set; }
    }
}
