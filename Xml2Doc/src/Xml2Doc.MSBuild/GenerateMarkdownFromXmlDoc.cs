using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xml2Doc.Core;

namespace Xml2Doc.MSBuild;

/// <summary>
/// MSBuild task that converts a compiler‑generated XML documentation file into Markdown using Xml2Doc.
/// </summary>
/// <remarks>
/// Two output modes:
/// <list type="bullet">
/// <item><description><strong>Per‑type</strong> (<see cref="SingleFile"/> = <see langword="false"/>): one <c>.md</c> file per documented type plus an <c>index.md</c> in <see cref="OutputDirectory"/>.</description></item>
/// <item><description><strong>Single file</strong> (<see cref="SingleFile"/> = <see langword="true"/>): one consolidated Markdown file (index + all types) at <see cref="OutputFile"/>.</description></item>
/// </list>
/// File naming style is controlled by <see cref="FileNameMode"/> (maps to <see cref="Core.FileNameMode.Verbatim"/> / <see cref="Core.FileNameMode.CleanGenerics"/>).
/// Display trimming and fenced code block language via <see cref="RootNamespaceToTrim"/>, <see cref="TrimRootNamespaceInFileNames"/>, and <see cref="CodeBlockLanguage"/>.
/// <para>
/// Incremental hints: the task can auto‑compute <see cref="XmlSha256"/> (SHA‑256 of the XML doc) and a derived <see cref="Fingerprint"/> combining significant inputs (mode, output path, namespace trim, language). These values can be used by external targets for change detection.
/// </para>
/// <para>
/// If the XML file is missing the task logs a low‑importance message and returns success (no work). Dry runs (<see cref="DryRun"/>) simulate generation without writing Markdown; reports are still emitted if requested.
/// </para>
/// Reporting: set <see cref="ReportPath"/> to write a JSON report (omits nulls) including inputs, options, outputs, fingerprint, and optionally a timestamp if <see cref="IncludeTimestampInReport"/> is true.
/// </remarks>
/// <example>
/// Per‑type:
/// <code><![CDATA[
/// <GenerateMarkdownFromXmlDoc
///     XmlPath="$(TargetDir)MyLib.xml"
///     OutputDirectory="$(ProjectDir)docs"
///     FileNameMode="clean"
///     RootNamespaceToTrim="MyCompany.MyProduct"
///     TrimRootNamespaceInFileNames="true" />
/// ]]></code>
/// Single file:
/// <code><![CDATA[
/// <GenerateMarkdownFromXmlDoc
///     XmlPath="$(TargetDir)MyLib.xml"
///     SingleFile="true"
///     OutputFile="$(ProjectDir)docs\api.md"
///     CodeBlockLanguage="csharp" />
/// ]]></code>
/// Dry run:
/// <code><![CDATA[
/// <GenerateMarkdownFromXmlDoc
///     XmlPath="$(TargetDir)MyLib.xml"
///     OutputDirectory="$(ProjectDir)docs"
///     DryRun="true" />
/// ]]></code>
/// </example>
/// <seealso cref="MarkdownRenderer"/>
/// <seealso cref="RendererOptions"/>
/// <seealso cref="Core.FileNameMode"/>
public class GenerateMarkdownFromXmlDoc : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Path to the XML documentation file (usually <c>$(TargetDir)$(AssemblyName).xml</c>).
    /// Skips quietly if blank or not found.
    /// </summary>
    [Required] public string XmlPath { get; set; } = string.Empty;

    /// <summary>
    /// Directory for per‑type Markdown output (ignored in single‑file mode). Required when <see cref="SingleFile"/> is <see langword="false"/>.
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Enables single consolidated file output (index + all types). Requires <see cref="OutputFile"/>.
    /// </summary>
    public bool SingleFile { get; set; }

    /// <summary>
    /// Output path for the consolidated Markdown file (used only when <see cref="SingleFile"/> is <see langword="true"/>).
    /// </summary>
    public string? OutputFile { get; set; }

    /// <summary>
    /// File naming mode: <c>verbatim</c> preserves generic arity; <c>clean</c> strips arity tokens and normalizes generic braces.
    /// </summary>
    public string FileNameMode { get; set; } = "verbatim";

    /// <summary>
    /// Optional namespace prefix trimmed from displayed type names (e.g. <c>MyCompany.MyProduct</c>).
    /// </summary>
    public string? RootNamespaceToTrim { get; set; }

    /// <summary>
    /// When true, also trims <see cref="RootNamespaceToTrim"/> from generated file names (not just headings). Ignored if <see cref="RootNamespaceToTrim"/> is null.
    /// </summary>
    public bool TrimRootNamespaceInFileNames { get; set; }

    /// <summary>
    /// Language identifier used for fenced code blocks in output (default <c>csharp</c>).
    /// </summary>
    public string CodeBlockLanguage { get; set; } = "csharp";

    /// <summary>
    /// Optional JSON report file path. Report is written even in dry‑run mode.
    /// </summary>
    public string? ReportPath { get; set; }

    /// <summary>
    /// Simulates generation: logs intended actions without writing Markdown. Reports still produced if <see cref="ReportPath"/> set.
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Reserved for future diff/change analysis (currently no effect).
    /// </summary>
    public bool Diff { get; set; }

    /// <summary>
    /// Generated Markdown files. Single‑file mode yields one item. Empty when dry‑run or skipped.
    /// </summary>
    [Output] public ITaskItem[] GeneratedFiles { get; private set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Full path to the JSON report if written; otherwise null.
    /// </summary>
    [Output] public string? ReportPathOut { get; private set; }

    /// <summary>
    /// Indicates whether any Markdown files were physically written (false for dry‑run or skip).
    /// </summary>
    [Output] public bool DidWork { get; private set; }

    /// <summary>
    /// Optional externally supplied fingerprint of significant inputs. Auto‑computed if absent (see <see cref="Fingerprint"/> remarks).
    /// </summary>
    public string? Fingerprint { get; set; }

    /// <summary>
    /// SHA‑256 hash (hex lowercase) of the XML doc file. Auto‑computed if not provided.
    /// Used in fingerprint generation and included in the JSON report.
    /// </summary>
    public string? XmlSha256 { get; set; }

    /// <summary>
    /// Adds a timestamp to the JSON report when true; omit for deterministic reports.
    /// </summary>
    public bool IncludeTimestampInReport { get; set; }

    /// <summary>
    /// Executes the task: validates configuration, loads model, renders Markdown, computes optional hash/fingerprint, and writes a JSON report.
    /// </summary>
    /// <returns>True on success (including skip); false on validation failure or exception.</returns>
    /// <remarks>
    /// Steps:
    /// <list type="number">
    /// <item><description>Skip early if <see cref="XmlPath"/> missing or file not found.</description></item>
    /// <item><description>Normalize paths; compute <see cref="XmlSha256"/> if necessary.</description></item>
    /// <item><description>Create <see cref="RendererOptions"/> (including <see cref="TrimRootNamespaceInFileNames"/>).</description></item>
    /// <item><description>Render single or per‑type output (unless <see cref="DryRun"/>).</description></item>
    /// <item><description>Populate <see cref="GeneratedFiles"/> and set <see cref="DidWork"/>.</description></item>
    /// <item><description>Compute <see cref="Fingerprint"/> if not supplied (hash of XML SHA, mode, normalized output path, root namespace, language).</description></item>
    /// <item><description>Emit JSON report if <see cref="ReportPath"/> provided (null fields omitted; timestamp optional).</description></item>
    /// </list>
    /// Exceptions are caught, logged (stack trace), and cause a false return.
    /// </remarks>
    /// <exception cref="IOException">File I/O error while reading XML or writing outputs/report.</exception>
    /// <exception cref="UnauthorizedAccessException">Insufficient permissions for output/report paths.</exception>
    public override bool Execute()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(XmlPath) || !File.Exists(XmlPath))
            {
                Log.LogMessage(MessageImportance.Low, $"Xml2Doc: no XML at '{XmlPath}', skipping.");
                return true;
            }

            var xmlFull = Path.GetFullPath(XmlPath);

            if (string.IsNullOrWhiteSpace(XmlSha256))
            {
                try { XmlSha256 = ComputeFileSha256(xmlFull); }
                catch (Exception ex)
                {
                    Log.LogWarning($"Xml2Doc: unable to compute SHA256 for '{xmlFull}': {ex.Message}");
                }
            }

            var model = Core.Models.Xml2Doc.Load(xmlFull);

            var fnMode = FileNameMode.Equals("clean", StringComparison.OrdinalIgnoreCase)
                ? Core.FileNameMode.CleanGenerics
                : Core.FileNameMode.Verbatim;

            var options = new RendererOptions(
                FileNameMode: fnMode,
                RootNamespaceToTrim: string.IsNullOrWhiteSpace(RootNamespaceToTrim) ? null : RootNamespaceToTrim,
                CodeBlockLanguage: string.IsNullOrWhiteSpace(CodeBlockLanguage) ? "csharp" : CodeBlockLanguage,
                TrimRootNamespaceInFileNames: TrimRootNamespaceInFileNames
            );

            var renderer = new MarkdownRenderer(model, options);

            string? outDir = null;
            string? outFile = null;

            if (SingleFile)
            {
                if (string.IsNullOrWhiteSpace(OutputFile))
                {
                    Log.LogError("Xml2Doc: SingleFile=true requires OutputFile.");
                    return false;
                }

                outFile = Path.GetFullPath(OutputFile!);
                var outFileDir = Path.GetDirectoryName(outFile);
                if (!DryRun && !string.IsNullOrEmpty(outFileDir) && !Directory.Exists(outFileDir))
                    Directory.CreateDirectory(outFileDir);

                if (!DryRun)
                {
                    renderer.RenderToSingleFile(outFile);
                    DidWork = true;
                }

                GeneratedFiles = new[] { new TaskItem(outFile) };
                Log.LogMessage(MessageImportance.High, $"Xml2Doc {(DryRun ? "[dry-run] would write" : "wrote")} {outFile}");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(OutputDirectory))
                {
                    Log.LogError("Xml2Doc: SingleFile=false requires OutputDirectory.");
                    return false;
                }

                outDir = Path.GetFullPath(OutputDirectory!);
                if (!DryRun && !Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                if (!DryRun)
                {
                    renderer.RenderToDirectory(outDir);
                    DidWork = true;
                }

                if (Directory.Exists(outDir))
                {
                    GeneratedFiles = Directory.GetFiles(outDir, "*.md", SearchOption.TopDirectoryOnly)
                                              .Select(p => (ITaskItem)new TaskItem(p))
                                              .ToArray();
                }

                Log.LogMessage(MessageImportance.High, $"Xml2Doc {(DryRun ? "[dry-run] would write" : "wrote")} Markdown files to {outDir}");
            }

            if (string.IsNullOrWhiteSpace(Fingerprint))
            {
                Fingerprint = ComputeFingerprint(
                    xmlSha256: XmlSha256 ?? "",
                    singleFile: SingleFile,
                    outputPath: SingleFile ? (outFile ?? OutputFile ?? "") : (outDir ?? OutputDirectory ?? ""),
                    fileNameMode: fnMode.ToString(),
                    rootNs: options.RootNamespaceToTrim ?? "",
                    lang: options.CodeBlockLanguage ?? ""
                );
            }

            if (!string.IsNullOrWhiteSpace(ReportPath))
            {
                try
                {
                    var reportFull = Path.GetFullPath(ReportPath!);
                    var reportDir = Path.GetDirectoryName(reportFull);
                    if (!string.IsNullOrEmpty(reportDir) && !Directory.Exists(reportDir))
                        Directory.CreateDirectory(reportDir);

                    var report = new ReportModel
                    {
                        xml = xmlFull,
                        single = SingleFile,
                        outputFile = outFile ?? (SingleFile ? OutputFile : null),
                        outputDir = outDir ?? (SingleFile ? null : OutputDirectory),
                        files = GeneratedFiles.Select(i => i.ItemSpec).ToArray(),
                        options = new ReportOptions
                        {
                            fileNameMode = fnMode.ToString(),
                            rootNs = options.RootNamespaceToTrim,
                            lang = options.CodeBlockLanguage
                        },
                        fingerprint = Fingerprint,
                        xmlSha256 = XmlSha256,
                        timestamp = IncludeTimestampInReport ? DateTimeOffset.Now : (DateTimeOffset?)null
                    };

                    var json = JsonSerializer.Serialize(
                        report,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        }
                    );

                    File.WriteAllText(reportFull, json);
                    ReportPathOut = reportFull;
                }
                catch (Exception rex)
                {
                    Log.LogWarning($"Xml2Doc: failed to write report '{ReportPath}': {rex.Message}");
                }
            }

            return !Log.HasLoggedErrors;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, /*showStackTrace*/ true);
            return false;
        }
    }

    /// <summary>
    /// Computes a SHA‑256 hash (hex lowercase) of the file contents.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <returns>Hex lowercase SHA‑256 string.</returns>
    private static string ComputeFileSha256(string path)
    {
        using var sha = SHA256.Create();
        using var s = File.OpenRead(path);
        var hash = sha.ComputeHash(s);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    /// <summary>
    /// Computes a deterministic fingerprint of significant inputs (XML hash, mode, normalized output path, filename mode, root namespace, language).
    /// </summary>
    private static string ComputeFingerprint(string xmlSha256, bool singleFile, string outputPath, string fileNameMode, string rootNs, string lang)
    {
        using var sha = SHA256.Create();
        var data = string.Join("|", new[]
        {
            xmlSha256,
            singleFile ? "single" : "pertype",
            NormalizePathForHash(outputPath),
            fileNameMode,
            rootNs,
            lang
        });
        var bytes = Encoding.UTF8.GetBytes(data);
        var hash = sha.ComputeHash(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    /// <summary>
    /// Normalizes a path for hashing (full path, trimmed trailing separators). Returns empty string for blank or on failure.
    /// </summary>
    private static string NormalizePathForHash(string p)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(p)) return "";
            return Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return p ?? "";
        }
    }

    /// <summary>
    /// JSON report root model (internal). Null properties omitted during serialization.
    /// </summary>
    private sealed class ReportModel
    {
        public string xml { get; set; } = "";
        public bool single { get; set; }
        public string? outputFile { get; set; }
        public string? outputDir { get; set; }
        public string[] files { get; set; } = Array.Empty<string>();
        public ReportOptions options { get; set; } = new();
        public string? fingerprint { get; set; }
        public string? xmlSha256 { get; set; }
        public DateTimeOffset? timestamp { get; set; }
    }

    /// <summary>
    /// Snapshot of rendering options in the JSON report (internal).
    /// </summary>
    private sealed class ReportOptions
    {
        public string fileNameMode { get; set; } = "Verbatim";
        public string? rootNs { get; set; }
        public string? lang { get; set; }
    }
}
