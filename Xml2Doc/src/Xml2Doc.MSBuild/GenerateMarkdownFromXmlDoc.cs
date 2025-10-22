using Microsoft.Build.Framework;
using Xml2Doc.Core;

namespace Xml2Doc.MSBuild;

/// <summary>
/// MSBuild task that converts a compiler-generated XML documentation file into Markdown files.
/// </summary>
/// <remarks>
/// The task loads the XML documentation using <see cref="Core.Models.Xml2Doc"/> and renders Markdown via <see cref="MarkdownRenderer"/>.
/// </remarks>
/// <example>
/// <code><![CDATA[
/// <Target Name="Docs">
///   <GenerateMarkdownFromXmlDoc XmlPath="$(OutputPath)MyLib.xml"
///                               OutputDirectory="$(ProjectDir)docs" />
/// </Target>
/// ]]></code>
/// </example>
public class GenerateMarkdownFromXmlDoc : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Gets or sets the path to the XML documentation file to process.
    /// </summary>
    [Microsoft.Build.Framework.Required]
    public string XmlPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the output directory where Markdown files will be written.
    /// </summary>
    [Microsoft.Build.Framework.Required]
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Executes the task.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> on success; otherwise <see langword="false"/>. Any exceptions are caught and logged via MSBuild.
    /// </returns>
    public override bool Execute()
    {
        try
        {
            var model = Core.Models.Xml2Doc.Load(XmlPath);
            var renderer = new MarkdownRenderer(model);
            renderer.RenderToDirectory(OutputDirectory);
            Log.LogMessage(MessageImportance.High, $"Xml2Doc wrote Markdown to {OutputDirectory}");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, true);
            return false;
        }
    }
}
