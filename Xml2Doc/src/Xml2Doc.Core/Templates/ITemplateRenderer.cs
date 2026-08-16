namespace Xml2Doc.Core.Templates;

/// <summary>Applies an outer layout or front matter to generated Markdown.</summary>
public interface ITemplateRenderer
{
    /// <summary>Transforms one complete built-in Markdown document.</summary>
    string Render(TemplateRenderContext context);
}
