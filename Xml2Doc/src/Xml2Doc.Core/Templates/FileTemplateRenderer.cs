namespace Xml2Doc.Core.Templates;

/// <summary>
/// Applies an optional token-based template and optional front-matter file.
/// </summary>
public sealed class FileTemplateRenderer : ITemplateRenderer
{
    private const string ContentToken = "{{content}}";
    private readonly string? _template;
    private readonly string? _frontMatter;

    /// <summary>Loads the configured files once for deterministic rendering.</summary>
    public FileTemplateRenderer(
        string? templatePath,
        string? frontMatterPath)
    {
        if (string.IsNullOrWhiteSpace(templatePath) &&
            string.IsNullOrWhiteSpace(frontMatterPath))
        {
            throw new ArgumentException(
                "A template path or front-matter path is required.");
        }

        if (!string.IsNullOrWhiteSpace(templatePath))
        {
            _template = File.ReadAllText(templatePath!);
            if (_template.IndexOf(ContentToken, StringComparison.Ordinal) < 0)
            {
                throw new InvalidDataException(
                    $"Template '{templatePath}' must contain {ContentToken}.");
            }
        }

        if (!string.IsNullOrWhiteSpace(frontMatterPath))
            _frontMatter = File.ReadAllText(frontMatterPath!);
    }

    /// <inheritdoc />
    public string Render(TemplateRenderContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        var content = context.Content;
        if (_template is not null)
        {
            content = _template
                .Replace("{{title}}", context.Title ?? string.Empty)
                .Replace(
                    "{{kind}}",
                    context.Kind.ToString().ToLowerInvariant())
                .Replace(ContentToken, content);
        }

        if (_frontMatter is null)
            return content;

        return _frontMatter.TrimEnd('\r', '\n') + "\n" + content;
    }
}
