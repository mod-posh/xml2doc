namespace Xml2Doc.Core.Templates;

/// <summary>Preserves Xml2Doc's built-in Markdown without modification.</summary>
public sealed class DefaultTemplateRenderer : ITemplateRenderer
{
    /// <summary>The shared stateless instance.</summary>
    public static DefaultTemplateRenderer Instance { get; } = new();

    private DefaultTemplateRenderer()
    {
    }

    /// <inheritdoc />
    public string Render(TemplateRenderContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        return context.Content;
    }
}
