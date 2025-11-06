using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xml2Doc.Core;

namespace Xml2Doc.MSBuild;

/// <summary>
/// MSBuild task that converts a compiler‑generated XML documentation file into Markdown using Xml2Doc.
/// </summary>
/// <remarks>
/// Modes:
/// <list type="bullet">
/// <item><description>Per‑type output: set <see cref="SingleFile"/> to <see langword="false"/> and provide <see cref="OutputDirectory"/>; one <c>.md</c> per type plus an <c>index.md</c>.</description></item>
/// <item><description>Single consolidated file: set <see cref="SingleFile"/> to <see langword="true"/> and provide <see cref="OutputFile"/>; includes an index then all types.</description></item>
/// </list>
/// Filename formatting is controlled by <see cref="FileNameMode"/> (<c>verbatim</c> or <c>clean</c>). Namespace trimming and fenced code block language are set via <see cref="RootNamespaceToTrim"/> and <see cref="CodeBlockLanguage"/>.
/// If the XML file is missing the task logs a low‑importance message and succeeds (treated as no work).
/// Optional JSON execution metadata can be written to <see cref="ReportPath"/>.
/// </remarks>
/// <example>
/// Per‑type output:
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
/// <seealso cref="FileNameMode"/>
public class GenerateMarkdownFromXmlDoc : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Path to the XML documentation file (usually <c>$(TargetDir)$(AssemblyName).xml</c>).
    /// Required unless the task is skipped (missing file).
    /// </summary>
    [Required] public string XmlPath { get; set; } = string.Empty;

    /// <summary>
    /// Directory for per‑type Markdown output (ignored in single‑file mode). Required when <see cref="SingleFile"/> is <see langword="false"/>.
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// When <see langword="true"/>, generates a single consolidated Markdown file. Requires <see cref="OutputFile"/>.
    /// </summary>
    public bool SingleFile { get; set; }

    /// <summary>
    /// Output file path used in single‑file mode. Ignored in per‑type mode.
    /// </summary>
    public string? OutputFile { get; set; }

    /// <summary>
    /// Filename mode: <c>verbatim</c> (default) or <c>clean</c> (removes generic arity tokens).
    /// </summary>
    public string FileNameMode { get; set; } = "verbatim";

    /// <summary>
    /// Optional namespace prefix trimmed from displayed type names (e.g., <c>MyCompany.MyProduct</c>).
    /// </summary>
    public string? RootNamespaceToTrim { get; set; }

    /// <summary>
    /// Language identifier for fenced code blocks (defaults to <c>csharp</c>).
    /// </summary>
    public string CodeBlockLanguage { get; set; } = "csharp";

    /// <summary>
    /// Optional path for a JSON report describing the execution (inputs, outputs, options).
    /// </summary>
    public string? ReportPath { get; set; }

    /// <summary>
    /// Performs a dry run: computes/collects intended outputs and logs actions without writing Markdown.
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Reserved for future diff support (currently no effect).
    /// </summary>
    public bool Diff { get; set; }

    /// <summary>
    /// Generated Markdown files. In single‑file mode contains one item. Empty on dry run or skip.
    /// </summary>
    [Output] public ITaskItem[] GeneratedFiles { get; private set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Final report path if the JSON report was successfully written; otherwise <see langword="null"/>.
    /// </summary>
    [Output] public string? ReportPathOut { get; private set; }

    /// <summary>
    /// Indicates whether any Markdown was written (<see langword="false"/> if dry run or skipped).
    /// </summary>
    [Output] public bool DidWork { get; private set; }

    /// <summary>
    /// Executes the task: validates mode, loads XML doc model, renders Markdown (single or per‑type), gathers outputs, and optionally writes a JSON report.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> on success (including a skip due to missing XML); <see langword="false"/> on validation error or exception.
    /// </returns>
    /// <remarks>
    /// Steps:
    /// <list type="number">
    /// <item><description>Skip early if <see cref="XmlPath"/> is empty or the file does not exist (success, no work).</description></item>
    /// <item><description>Resolve <see cref="FileNameMode"/> and construct <see cref="RendererOptions"/>.</description></item>
    /// <item><description>Render: single file (writes one) or per‑type (writes many + index).</description></item>
    /// <item><description>Populate <see cref="GeneratedFiles"/>, set <see cref="DidWork"/> if not a dry run.</description></item>
    /// <item><description>Write JSON report if <see cref="ReportPath"/> is provided.</description></item>
    /// </list>
    /// Exceptions are logged with stack trace; task then returns <see langword="false"/>.
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
                    Log.LogError("Xml2Doc: SingleFile=true requires OutputFile.");
                    return false;
                }

                var outFile = OutputFile!;
                var outDir = Path.GetDirectoryName(outFile);
                if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

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

                var outDir = OutputDirectory!;
                if (!Directory.Exists(outDir))
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

                    var reportDir = Path.GetDirectoryName(ReportPath);
                    if (!string.IsNullOrEmpty(reportDir) && !Directory.Exists(reportDir))
                        Directory.CreateDirectory(reportDir);

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
