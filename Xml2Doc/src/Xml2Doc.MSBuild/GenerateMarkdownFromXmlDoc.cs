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
/// MSBuild task that converts a compiler-generated XML documentation file into Markdown using Xml2Doc.
/// </summary>
/// <remarks>
/// Modes:
/// <list type="bullet">
/// <item><description>Per-type: <see cref="SingleFile"/> = <see langword="false"/> → one file per type + <c>index.md</c> written to <see cref="OutputDirectory"/>.</description></item>
/// <item><description>Single file: <see cref="SingleFile"/> = <see langword="true"/> → one consolidated file (index + all types) written to <see cref="OutputFile"/>.</description></item>
/// </list>
/// Filename style is controlled by <see cref="FileNameMode"/> (maps to <see cref="Core.FileNameMode.Verbatim"/> or <see cref="Core.FileNameMode.CleanGenerics"/>).
/// Display trimming and fenced code block language are set via <see cref="RootNamespaceToTrim"/> and <see cref="CodeBlockLanguage"/>.
/// <para>
/// Incremental hints:
/// A SHA-256 of the XML doc (<see cref="XmlSha256"/>) plus significant options are hashed into <see cref="Fingerprint"/> (auto-computed if missing).
/// This can be used by external targets to detect changes without re-rendering.
/// </para>
/// <para>
/// If the XML file is absent the task logs a low-importance message and returns success (no work performed).
/// Dry runs (<see cref="DryRun"/>) compute outputs without writing files (a report is still emitted if requested).
/// </para>
/// Reporting:
/// Set <see cref="ReportPath"/> to emit a JSON file containing inputs, options, outputs, fingerprint, and optionally a timestamp (enable via <see cref="IncludeTimestampInReport"/>).
/// Null/unused fields are omitted.
/// </remarks>
/// <example>
/// Per-type:
/// <code><![CDATA[
/// <GenerateMarkdownFromXmlDoc
///     XmlPath="$(TargetDir)MyLib.xml"
///     OutputDirectory="$(ProjectDir)docs"
///     FileNameMode="clean"
///     RootNamespaceToTrim="MyCompany.MyProduct" />
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
    /// Path to the XML documentation file (typically <c>$(TargetDir)$(AssemblyName).xml</c>).
    /// If blank or the file does not exist the task skips quietly.
    /// </summary>
    [Required] public string XmlPath { get; set; } = string.Empty;

    /// <summary>
    /// Directory where per-type Markdown files are written (ignored when <see cref="SingleFile"/> is <see langword="true"/>).
    /// Required in per-type mode.
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// When <see langword="true"/>, produce a single consolidated Markdown file instead of per-type files.
    /// Requires <see cref="OutputFile"/>.
    /// </summary>
    public bool SingleFile { get; set; }

    /// <summary>
    /// Output Markdown file path used in single-file mode. Ignored when <see cref="SingleFile"/> is <see langword="false"/>.
    /// </summary>
    public string? OutputFile { get; set; }

    /// <summary>
    /// Filename mode: <c>verbatim</c> (default) preserves generic arity tokens; <c>clean</c> strips them and normalizes braces.
    /// </summary>
    public string FileNameMode { get; set; } = "verbatim";

    /// <summary>
    /// Optional namespace prefix trimmed from displayed type names (e.g., <c>MyCompany.MyProduct</c>). Improves readability of headings and links.
    /// </summary>
    public string? RootNamespaceToTrim { get; set; }

    /// <summary>
    /// Language identifier for fenced code blocks (defaults to <c>csharp</c>).
    /// </summary>
    public string CodeBlockLanguage { get; set; } = "csharp";

    /// <summary>
    /// Optional path to a JSON report describing the execution (inputs, options, outputs, fingerprint, hashes). Written even on dry run.
    /// </summary>
    public string? ReportPath { get; set; }

    /// <summary>
    /// Dry-run mode: compute and log intended outputs without writing Markdown files. A report is still written if requested.
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Reserved for future diff or change analysis logic (currently unused; always ignored).
    /// </summary>
    public bool Diff { get; set; }

    /// <summary>
    /// Collection of generated Markdown files. In single-file mode contains one item; empty on dry run or skip.
    /// </summary>
    [Output] public ITaskItem[] GeneratedFiles { get; private set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Full path to the JSON report if successfully written; otherwise <see langword="null"/>.
    /// </summary>
    [Output] public string? ReportPathOut { get; private set; }

    /// <summary>
    /// Indicates whether any Markdown content was physically written (false if dry run or skipped).
    /// </summary>
    [Output] public bool DidWork { get; private set; }

    /// <summary>
    /// Optional externally supplied fingerprint representing significant inputs. If absent a value is auto-computed after rendering.
    /// Used for incremental detection by calling targets.
    /// </summary>
    public string? Fingerprint { get; set; }

    /// <summary>
    /// SHA-256 hash of the XML documentation file (hex lowercase). Auto-computed if not supplied.
    /// Included in the report and used when generating <see cref="Fingerprint"/>.
    /// </summary>
    public string? XmlSha256 { get; set; }

    /// <summary>
    /// When <see langword="true"/>, include a timestamp field in the JSON report; otherwise omit it for stable/deterministic output.
    /// </summary>
    public bool IncludeTimestampInReport { get; set; }

    /// <summary>
    /// Executes the task: validates required paths, loads the documentation model, renders Markdown (single or per-type),
    /// generates a fingerprint / hash as needed, and optionally emits a JSON report.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> on success (including a no-op skip); <see langword="false"/> on validation failure or an exception.
    /// </returns>
    /// <remarks>
    /// Sequence:
    /// <list type="number">
    /// <item><description>Early skip if <see cref="XmlPath"/> missing or file not found.</description></item>
    /// <item><description>Normalize paths; compute <see cref="XmlSha256"/> if absent.</description></item>
    /// <item><description>Create <see cref="RendererOptions"/> from task parameters.</description></item>
    /// <item><description>Render single-file or per-type output (unless dry run).</description></item>
    /// <item><description>Collect <see cref="GeneratedFiles"/> and set <see cref="DidWork"/> when writes occur.</description></item>
    /// <item><description>Derive <see cref="Fingerprint"/> if not supplied (hash of XML SHA, mode, output path, namespace trim, language).</description></item>
    /// <item><description>Emit JSON report (omitting nulls) when <see cref="ReportPath"/> provided.</description></item>
    /// </list>
    /// Any exception is caught, logged with stack trace, and results in a failing return value.
    /// </remarks>
    /// <exception cref="IOException">I/O failure while reading or writing output/report files.</exception>
    /// <exception cref="UnauthorizedAccessException">Insufficient permissions for file or directory operations.</exception>
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
                CodeBlockLanguage: string.IsNullOrWhiteSpace(CodeBlockLanguage) ? "csharp" : CodeBlockLanguage
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
    /// Computes a SHA-256 hash (hex lowercase) of a file's contents.
    /// </summary>
    /// <param name="path">Absolute or relative file path.</param>
    /// <returns>Hex lowercase SHA-256 string.</returns>
    /// <exception cref="IOException">File read failure.</exception>
    /// <exception cref="UnauthorizedAccessException">Access denied to the file.</exception>
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
    /// <param name="xmlSha256">SHA-256 of the XML doc file.</param>
    /// <param name="singleFile">Whether single-file mode is active.</param>
    /// <param name="outputPath">Normalized output path (file or directory).</param>
    /// <param name="fileNameMode">Effective filename mode string.</param>
    /// <param name="rootNs">Root namespace trimmed (may be empty).</param>
    /// <param name="lang">Code block language identifier.</param>
    /// <returns>Hex lowercase SHA-256 fingerprint string.</returns>
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
    /// Normalizes a path for hashing: returns full path with trailing separators trimmed; returns empty string on failure or blank input.
    /// </summary>
    /// <param name="p">Path to normalize.</param>
    /// <returns>Normalized path or empty string.</returns>
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
    /// JSON report root model. Null properties omitted based on serializer options.
    /// </summary>
    private sealed class ReportModel
    {
        /// <summary>Full path to the XML doc file.</summary>
        public string xml { get; set; } = "";
        /// <summary>True when single-file mode was used.</summary>
        public bool single { get; set; }
        /// <summary>Output file path (single-file mode only).</summary>
        public string? outputFile { get; set; }
        /// <summary>Output directory (per-type mode only).</summary>
        public string? outputDir { get; set; }
        /// <summary>Generated Markdown file paths.</summary>
        public string[] files { get; set; } = Array.Empty<string>();
        /// <summary>Rendering option snapshot.</summary>
        public ReportOptions options { get; set; } = new();
        /// <summary>Computed or supplied fingerprint.</summary>
        public string? fingerprint { get; set; }
        /// <summary>SHA-256 hash of the XML doc file.</summary>
        public string? xmlSha256 { get; set; }
        /// <summary>Timestamp included only when <see cref="IncludeTimestampInReport"/> is true.</summary>
        public DateTimeOffset? timestamp { get; set; }
    }

    /// <summary>
    /// Nested options section of the JSON report.
    /// </summary>
    private sealed class ReportOptions
    {
        /// <summary>Effective filename mode string.</summary>
        public string fileNameMode { get; set; } = "Verbatim";
        /// <summary>Root namespace trimmed (if any).</summary>
        public string? rootNs { get; set; }
        /// <summary>Code block language identifier.</summary>
        public string? lang { get; set; }
    }
}
