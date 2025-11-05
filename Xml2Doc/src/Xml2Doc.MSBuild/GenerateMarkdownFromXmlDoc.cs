using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Text.Json;
using Xml2Doc.Core;

namespace Xml2Doc.MSBuild;

/// <summary>
/// MSBuild task that converts a compiler‑generated XML documentation file into Markdown using Xml2Doc.
/// </summary>
/// <remarks>
/// This task supports two output modes:
/// <para>
/// 1. Per‑type output: one <c>.md</c> file per documented type plus an <c>index.md</c> file (set <see cref="SingleFile"/> to <see langword="false"/>).<br/>
/// 2. Single consolidated file: one combined Markdown file containing an index and all types (set <see cref="SingleFile"/> to <see langword="true"/> and provide <see cref="OutputFile"/>).
/// </para>
/// Filename generation is controlled via <see cref="FileNameMode"/> (maps to <see cref="Core.FileNameMode.Verbatim"/> or <see cref="Core.FileNameMode.CleanGenerics"/>).<br/>
/// Additional rendering behavior (namespace trimming, code fence language) is passed to <see cref="MarkdownRenderer"/> through <see cref="RendererOptions"/>.
/// <para>
/// If the XML documentation file does not exist, the task logs a low‑importance message and returns success (treats this as "nothing to do").
/// This allows builds without XML docs (e.g., when <c>GenerateDocumentationFile</c> is disabled) to proceed without failing.
/// </para>
/// An optional JSON report can be emitted via <see cref="ReportPath"/> describing the execution (even during a dry‑run).
/// </remarks>
/// <example>
/// Minimal usage generating per‑type files:
/// <code><![CDATA[
/// <Target Name="Docs">
///   <GenerateMarkdownFromXmlDoc
///       XmlPath="$(TargetDir)MyLib.xml"
///       OutputDirectory="$(ProjectDir)docs" />
/// </Target>
/// ]]></code>
/// Single consolidated file with namespace trimming and custom code fence language:
/// <code><![CDATA[
/// <Target Name="Docs">
///   <GenerateMarkdownFromXmlDoc
///       XmlPath="$(TargetDir)MyLib.xml"
///       SingleFile="true"
///       OutputFile="$(ProjectDir)docs\\api.md"
///       RootNamespaceToTrim="MyCompany.MyProduct"
///       CodeBlockLanguage="csharp"
///       FileNameMode="clean"
///       ReportPath="$(ProjectDir)docs\\api-report.json" />
/// </Target>
/// ]]></code>
/// </example>
/// <seealso cref="MarkdownRenderer"/>
/// <seealso cref="RendererOptions"/>
/// <seealso cref="Core.FileNameMode"/>
public class GenerateMarkdownFromXmlDoc : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Path to the compiler‑generated XML documentation file to process.
    /// Must point to the file produced when the project has <c>GenerateDocumentationFile=true</c>.
    /// If the file is missing, the task skips gracefully.
    /// </summary>
    [Required] public string XmlPath { get; set; } = string.Empty;

    /// <summary>
    /// Directory where per‑type Markdown files will be written when <see cref="SingleFile"/> is <see langword="false"/>.
    /// Ignored when <see cref="SingleFile"/> is <see langword="true"/>.
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// When <see langword="true"/>, emits a single consolidated Markdown file instead of per‑type files.
    /// Requires <see cref="OutputFile"/> to be set.
    /// </summary>
    public bool SingleFile { get; set; } = false;

    /// <summary>
    /// Output Markdown file path used when <see cref="SingleFile"/> is <see langword="true"/>.
    /// Ignored when <see cref="SingleFile"/> is <see langword="false"/>.
    /// </summary>
    public string? OutputFile { get; set; }

    /// <summary>
    /// Filename mode for generated Markdown files: <c>verbatim</c> (default) or <c>clean</c> (case‑insensitive).
    /// Maps to <see cref="Core.FileNameMode.Verbatim"/> and <see cref="Core.FileNameMode.CleanGenerics"/>.
    /// </summary>
    public string FileNameMode { get; set; } = "verbatim";

    /// <summary>
    /// Optional namespace prefix to remove from displayed type names (e.g., <c>MyCompany.MyProduct</c>) in rendered output.
    /// </summary>
    public string? RootNamespaceToTrim { get; set; }

    /// <summary>
    /// Language identifier to use for fenced code blocks in Markdown. Defaults to <c>csharp</c>.
    /// </summary>
    public string CodeBlockLanguage { get; set; } = "csharp";

    /// <summary>
    /// Optional path to a JSON report describing task execution (input XML path, output mode, generated files, options).
    /// The report is written even during a dry‑run for diagnostic purposes.
    /// </summary>
    public string? ReportPath { get; set; }

    /// <summary>
    /// When <see langword="true"/>, performs a dry run: determines targets and logs intentions without writing Markdown files.
    /// The JSON report (if <see cref="ReportPath"/> is set) is still written.
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Reserved flag for future diff / change analysis of generated Markdown.
    /// Currently not implemented; setting this has no effect.
    /// </summary>
    public bool Diff { get; set; }

    // Outputs

    /// <summary>
    /// The set of generated Markdown files (or empty during dry‑run).
    /// For single‑file mode, contains exactly one item (the consolidated file).
    /// </summary>
    [Output] public ITaskItem[] GeneratedFiles { get; private set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Echoes the final resolved report path when <see cref="ReportPath"/> is supplied and the report is written.
    /// </summary>
    [Output] public string? ReportPathOut { get; private set; }

    /// <summary>
    /// Indicates whether actual file writes occurred (<see langword="true"/> unless skipped or dry‑run).
    /// </summary>
    [Output] public bool DidWork { get; private set; }

    /// <summary>
    /// Executes the task: loads the XML documentation model, configures renderer options, and generates Markdown according to the selected mode.
    /// </summary>
    /// <returns><see langword="true"/> on success; otherwise <see langword="false"/> if errors were logged.</returns>
    /// <remarks>
    /// Behavior overview:
    /// <list type="bullet">
    /// <item><description>If <see cref="XmlPath"/> does not exist, logs a message and succeeds without doing work.</description></item>
    /// <item><description>Single‑file mode requires <see cref="OutputFile"/>; per‑type mode requires <see cref="OutputDirectory"/>.</description></item>
    /// <item><description>Dry‑run suppresses Markdown writes but still evaluates file paths and logs intended actions.</description></item>
    /// <item><description>JSON report (if <see cref="ReportPath"/> set) is written after rendering (or dry‑run evaluation).</description></item>
    /// </list>
    /// Exceptions are captured and logged via MSBuild diagnostics (stack trace included).
    /// </remarks>
    public override bool Execute()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(XmlPath) || !File.Exists(XmlPath))
            {
                Log.LogMessage(MessageImportance.Low, $"Xml2Doc: no XML at '{XmlPath}', skipping.");
                return true;
            }

            // NOTE: keep the loader call consistent with current Core API in your repo
            var model = Core.Models.Xml2Doc.Load(XmlPath);

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
                // Best-effort enumeration of top-level .md files for reporting/targets
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
