using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
// ADDED: these are needed on net472 where implicit usings aren’t on by default
using System.IO;
using System.Linq;
// (kept) using for JSON
using System.Text.Json;
using Xml2Doc.Core;

namespace Xml2Doc.MSBuild;

/// <summary>
/// MSBuild task that converts a compiler‑generated XML documentation file into Markdown using Xml2Doc.
/// </summary>
/// <remarks>
/// Core behavior:
/// <list type="bullet">
/// <item><description>Loads an XML documentation file produced when <c>GenerateDocumentationFile=true</c> is set on a project.</description></item>
/// <item><description>Renders either one consolidated Markdown file (set <see cref="SingleFile"/> and provide <see cref="OutputFile"/>) or per‑type files plus an index (provide <see cref="OutputDirectory"/>).</description></item>
/// <item><description>Filename style is controlled by <see cref="FileNameMode"/> mapping to <see cref="Core.FileNameMode.Verbatim"/> or <see cref="Core.FileNameMode.CleanGenerics"/>.</description></item>
/// <item><description>Display trimming and fenced code block language are passed through to <see cref="RendererOptions"/> via properties <see cref="RootNamespaceToTrim"/> and <see cref="CodeBlockLanguage"/>.</description></item>
/// <item><description>Optional JSON execution report written to <see cref="ReportPath"/> (even during a dry run if the path is set).</description></item>
/// </list>
/// Alias support:
/// <para>
/// Older targets may pass <c>Xml</c>, <c>OutputDir</c>, <c>RootNamespace</c>, or <c>ProjectDir</c>. These alias properties forward to the primary ones for backwards compatibility before processing begins.
/// </para>
/// Incremental behavior:
/// <para>
/// The companion targets file (shipped under <c>build/</c>) can establish input/output stamps to skip regeneration when neither task options nor the XML file change.
/// </para>
/// Failure / skip semantics:
/// <list type="bullet">
/// <item><description>If <see cref="XmlPath"/> is missing or the file does not exist, the task logs a low‑importance message and returns success (treated as "nothing to do").</description></item>
/// <item><description>Validation errors (e.g., missing <see cref="OutputFile"/> in single‑file mode) log an MSBuild error and return <see langword="false"/>.</description></item>
/// <item><description>Exceptions are caught, logged with stack trace, and produce a failing result.</description></item>
/// </list>
/// </remarks>
/// <example>
/// Per‑type output:
/// <code><![CDATA[
/// <Target Name="Docs">
///   <GenerateMarkdownFromXmlDoc
///       XmlPath="$(TargetDir)MyLib.xml"
///       OutputDirectory="$(ProjectDir)docs"
///       FileNameMode="clean"
///       RootNamespaceToTrim="MyCompany.MyProduct"
///       CodeBlockLanguage="csharp" />
/// </Target>
/// ]]></code>
/// Single consolidated file:
/// <code><![CDATA[
/// <Target Name="Docs">
///   <GenerateMarkdownFromXmlDoc
///       XmlPath="$(TargetDir)MyLib.xml"
///       SingleFile="true"
///       OutputFile="$(ProjectDir)docs\\api.md"
///       FileNameMode="verbatim"
///       CodeBlockLanguage="csharp"
///       ReportPath="$(ProjectDir)docs\\api-report.json" />
/// </Target>
/// ]]></code>
/// Dry run (preview only):
/// <code><![CDATA[
/// <Target Name="DocsPreview">
///   <GenerateMarkdownFromXmlDoc
///       XmlPath="$(TargetDir)MyLib.xml"
///       OutputDirectory="$(ProjectDir)docs"
///       DryRun="true" />
/// </Target>
/// ]]></code>
/// </example>
/// <seealso cref="MarkdownRenderer"/>
/// <seealso cref="RendererOptions"/>
/// <seealso cref="Core.FileNameMode"/>
public class GenerateMarkdownFromXmlDoc : Microsoft.Build.Utilities.Task
{
    // ------------------------------
    // INPUTS
    // ------------------------------

    /// <summary>
    /// Path to the XML documentation file (normally <c>$(TargetDir)$(AssemblyName).xml</c>).
    /// Required when execution proceeds; if absent or the file does not exist the task skips without error.
    /// </summary>
    [Required] public string XmlPath { get; set; } = string.Empty;

    /// <summary>
    /// Directory where per‑type Markdown files are written (ignored when <see cref="SingleFile"/> is <see langword="true"/>).
    /// Must be set in multi‑file mode.
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// When <see langword="true"/>, emits a single consolidated Markdown file (index + all types). Requires <see cref="OutputFile"/>.
    /// </summary>
    public bool SingleFile { get; set; } = false;

    /// <summary>
    /// Output Markdown file path used when <see cref="SingleFile"/> is <see langword="true"/>.
    /// Ignored otherwise; must be non‑blank in single‑file mode.
    /// </summary>
    public string? OutputFile { get; set; }

    /// <summary>
    /// Filename mode for generated Markdown: <c>verbatim</c> (default) or <c>clean</c>.
    /// Maps to <see cref="Core.FileNameMode.Verbatim"/> or <see cref="Core.FileNameMode.CleanGenerics"/>.
    /// </summary>
    public string FileNameMode { get; set; } = "verbatim";

    /// <summary>
    /// Namespace prefix trimmed from displayed type names (e.g., <c>MyCompany.MyProduct</c>) to shorten headings and links.
    /// </summary>
    public string? RootNamespaceToTrim { get; set; }

    /// <summary>
    /// Language identifier used for fenced code blocks in generated Markdown (defaults to <c>csharp</c>).
    /// </summary>
    public string CodeBlockLanguage { get; set; } = "csharp";

    /// <summary>
    /// Optional path to a JSON execution report. When provided, a file describing inputs, outputs, and options is written (also during dry runs).
    /// </summary>
    public string? ReportPath { get; set; }

    /// <summary>
    /// When <see langword="true"/>, performs a dry run: computes intended outputs and logs actions without writing Markdown files.
    /// Still produces a report if <see cref="ReportPath"/> is set.
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Reserved for future generated‑vs‑existing diff analysis. Currently has no effect.
    /// </summary>
    public bool Diff { get; set; }

    // ---------------------------------------------------
    // BACK‑COMPAT / ALIAS PROPERTIES
    // ---------------------------------------------------

    /// <summary>
    /// Alias for <see cref="XmlPath"/> accepting an item array (common pattern: passing the XML doc as an item).
    /// The first item (if any) is mapped to <see cref="XmlPath"/> before processing.
    /// </summary>
    public ITaskItem[]? Xml { get; set; }

    /// <summary>
    /// Alias for <see cref="OutputDirectory"/> used by older target files (<c>OutputDir</c> → <see cref="OutputDirectory"/>).
    /// </summary>
    public string? OutputDir { get; set; }

    /// <summary>
    /// Optional project directory reference accepted for potential future relative path resolution. Not required by current logic.
    /// </summary>
    public string? ProjectDir { get; set; }

    /// <summary>
    /// Alias for <see cref="RootNamespaceToTrim"/> used by older targets (<c>RootNamespace</c>).
    /// Setting this updates <see cref="RootNamespaceToTrim"/>.
    /// </summary>
    public string? RootNamespace
    {
        get => RootNamespaceToTrim;
        set => RootNamespaceToTrim = value;
    }

    // --------
    // OUTPUTS
    // --------

    /// <summary>
    /// Set of generated Markdown files. Empty on dry run or when skipped (missing XML).
    /// For single‑file mode contains exactly one item.
    /// </summary>
    [Output] public ITaskItem[] GeneratedFiles { get; private set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Final resolved JSON report path if the report was written (null when not requested or failed).
    /// </summary>
    [Output] public string? ReportPathOut { get; private set; }

    /// <summary>
    /// Indicates whether any Markdown content was actually written to disk (false if dry run or skipped).
    /// </summary>
    [Output] public bool DidWork { get; private set; }

    /// <summary>
    /// Executes the task: normalizes alias properties, validates required inputs based on mode, renders Markdown and optional JSON report.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if successful (or skipped due to missing XML); <see langword="false"/> if validation fails or an exception is logged.
    /// </returns>
    /// <remarks>
    /// Processing steps:
    /// <list type="number">
    /// <item><description>Normalize alias properties (<see cref="Xml"/>, <see cref="OutputDir"/>, <see cref="RootNamespace"/>).</description></item>
    /// <item><description>Skip early (success) if <see cref="XmlPath"/> is empty or the file does not exist.</description></item>
    /// <item><description>Resolve <see cref="FileNameMode"/> to <see cref="Core.FileNameMode"/> and construct <see cref="RendererOptions"/>.</description></item>
    /// <item><description>Render either single or per‑type output via <see cref="MarkdownRenderer"/> depending on <see cref="SingleFile"/>.</description></item>
    /// <item><description>Populate <see cref="GeneratedFiles"/>, set <see cref="DidWork"/> if writes occurred.</description></item>
    /// <item><description>Write JSON execution report when <see cref="ReportPath"/> is provided.</description></item>
    /// </list>
    /// Any exception is captured, logged (stack trace included), and results in a failing return value.
    /// </remarks>
    public override bool Execute()
    {
        try
        {
            // Normalize alias properties
            if (string.IsNullOrWhiteSpace(XmlPath) && Xml != null && Xml.Length > 0)
            {
                XmlPath = Xml[0]?.ItemSpec ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(OutputDirectory) && !string.IsNullOrWhiteSpace(OutputDir))
            {
                OutputDirectory = OutputDir;
            }

            // Skip gracefully if XML docs absent
            if (string.IsNullOrWhiteSpace(XmlPath) || !File.Exists(XmlPath))
            {
                Log.LogMessage(MessageImportance.Low, $"Xml2Doc: no XML at '{XmlPath}', skipping.");
                return true;
            }

            // Load model
            var model = Core.Models.Xml2Doc.Load(XmlPath);

            // Map filename mode
            var fnMode = FileNameMode.Equals("clean", StringComparison.OrdinalIgnoreCase)
                ? Core.FileNameMode.CleanGenerics
                : Core.FileNameMode.Verbatim;

            var options = new RendererOptions(
                FileNameMode: fnMode,
                RootNamespaceToTrim: string.IsNullOrWhiteSpace(RootNamespaceToTrim) ? null : RootNamespaceToTrim,
                CodeBlockLanguage: string.IsNullOrWhiteSpace(CodeBlockLanguage) ? "csharp" : CodeBlockLanguage
            );

            var renderer = new MarkdownRenderer(model, options);

            if (SingleFile)
            {
                if (string.IsNullOrWhiteSpace(OutputFile))
                {
                    Log.LogError("SingleFile=true requires OutputFile to be set.");
                    return false;
                }

                var outFile = OutputFile!;
                Directory.CreateDirectory(Path.GetDirectoryName(outFile)!);
                if (!DryRun)
                {
                    renderer.RenderToSingleFile(outFile);
                    DidWork = true;
                }
                GeneratedFiles = new[] { new TaskItem(outFile) };
                Log.LogMessage(MessageImportance.High, $"Xml2Doc {(DryRun ? "[dry-run] would write" : "wrote")} single-file Markdown to {outFile}");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(OutputDirectory))
                {
                    Log.LogError("SingleFile=false requires OutputDirectory to be set.");
                    return false;
                }

                var outDir = OutputDirectory!;
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

            // Optional JSON report
            if (!string.IsNullOrWhiteSpace(ReportPath))
            {
                try
                {
                    var reportObj = new
                    {
                        xml = XmlPath,
                        single = SingleFile,
                        outputFile = OutputFile,
                        outputDir = OutputDirectory,
                        files = GeneratedFiles.Select(i => i.ItemSpec).ToArray(),
                        options = new
                        {
                            fileNameMode = fnMode.ToString(),
                            rootNs = options.RootNamespaceToTrim,
                            lang = options.CodeBlockLanguage
                        },
                        timestamp = DateTimeOffset.Now
                    };
                    Directory.CreateDirectory(Path.GetDirectoryName(ReportPath)!);
                    File.WriteAllText(ReportPath!, JsonSerializer.Serialize(reportObj, new JsonSerializerOptions { WriteIndented = true }));
                    ReportPathOut = ReportPath;
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
}
