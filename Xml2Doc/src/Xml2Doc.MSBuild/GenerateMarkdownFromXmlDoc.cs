using Microsoft.Build.Framework;
using Xml2Doc.Core;

namespace Xml2Doc.MSBuild;

public class GenerateMarkdownFromXmlDoc : Microsoft.Build.Utilities.Task
{
    [Microsoft.Build.Framework.Required]
    public string XmlPath { get; set; } = string.Empty;

    [Microsoft.Build.Framework.Required]
    public string OutputDirectory { get; set; } = string.Empty;

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
