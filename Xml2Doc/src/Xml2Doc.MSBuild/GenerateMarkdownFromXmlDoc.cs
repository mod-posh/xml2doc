using System;
using Microsoft.Build.Framework;
using Xml2Doc.Core;

namespace Xml2Doc.MSBuild;

/// <summary>
/// MSBuild task that converts a compiler-generated XML documentation file into Markdown using Xml2Doc.
/// </summary>
/// <remarks>
/// Supports per-type output into a directory or emitting a single consolidated Markdown file.
/// Use <see cref="FileNameMode"/> to control filename generation and other properties to tune rendering.
/// </remarks>
/// <example>
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
/// </example>
public class GenerateMarkdownFromXmlDoc : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Path to the compiler-generated XML documentation file to process.
    /// </summary>
    [Required] public string XmlPath { get; set; } = string.Empty;

    /// <summary>
    /// Directory where per-type Markdown files will be written when <see cref="SingleFile"/> is <see langword="false"/>.
    /// Ignored when <see cref="SingleFile"/> is <see langword="true"/>.
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// When <see langword="true"/>, emits a single Markdown file instead of per-type files.
    /// </summary>
    public bool SingleFile { get; set; } = false;

    /// <summary>
    /// Output Markdown file path used when <see cref="SingleFile"/> is <see langword="true"/>.
    /// Required if <see cref="SingleFile"/> is enabled. Ignored otherwise.
    /// </summary>
    public string? OutputFile { get; set; }

    /// <summary>
    /// Filename mode for generated Markdown files: <c>verbatim</c> (default) or <c>clean</c> (case-insensitive).
    /// Maps to <see cref="Core.FileNameMode.Verbatim"/> and <see cref="Core.FileNameMode.CleanGenerics"/>.
    /// </summary>
    public string FileNameMode { get; set; } = "verbatim";

    /// <summary>
    /// Optional namespace prefix to remove from displayed type names (e.g., <c>MyCompany.MyProduct</c>).
    /// </summary>
    public string? RootNamespaceToTrim { get; set; }

    /// <summary>
    /// Language identifier to use for fenced code blocks in Markdown. Defaults to <c>csharp</c>.
    /// </summary>
    public string CodeBlockLanguage { get; set; } = "csharp";

    /// <summary>
    /// Executes the task: loads the XML doc, configures options, and generates Markdown.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> on success; otherwise <see langword="false"/>. Errors are logged via MSBuild.
    /// </returns>
    public override bool Execute()
    {
        try
        {
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
                renderer.RenderToSingleFile(OutputFile);
                Log.LogMessage(MessageImportance.High, $"Xml2Doc wrote single-file Markdown to {OutputFile}");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(OutputDirectory))
                {
                    Log.LogError("SingleFile=false requires OutputDirectory to be set.");
                    return false;
                }
                renderer.RenderToDirectory(OutputDirectory);
                Log.LogMessage(MessageImportance.High, $"Xml2Doc wrote Markdown files to {OutputDirectory}");
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, /*showStackTrace*/ true);
            return false;
        }
    }
}
