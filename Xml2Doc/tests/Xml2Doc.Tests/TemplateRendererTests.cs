using Shouldly;
using System.Text;
using Xml2Doc.Core;
using Xml2Doc.Core.Templates;
using Xunit;

namespace Xml2Doc.Tests;

public class TemplateRendererTests
{
    [Fact]
    public async Task FileTemplateAndFrontMatter_AreAppliedToEveryDirectoryDocument()
    {
        var root = CreateTestRoot();

        try
        {
            var xmlPath = Path.Combine(root, "input.xml");
            var templatePath = Path.Combine(root, "template.md");
            var frontMatterPath = Path.Combine(root, "front-matter.yml");
            var output = Path.Combine(root, "docs");

            await File.WriteAllTextAsync(xmlPath, FixtureXml, new UTF8Encoding(false));
            await File.WriteAllTextAsync(
                templatePath,
                "<!-- {{kind}}:{{title}} -->\n{{content}}\n<!-- end -->",
                new UTF8Encoding(false));
            await File.WriteAllTextAsync(
                frontMatterPath,
                "---\nlayout: api\n---\n",
                new UTF8Encoding(false));

            var model = Xml2Doc.Core.Models.Xml2Doc.Load(xmlPath);
            var renderer = new MarkdownRenderer(
                model,
                new RendererOptions(
                    TemplatePath: templatePath,
                    FrontMatterPath: frontMatterPath));

            renderer.RenderToDirectory(output);

            var index = await File.ReadAllTextAsync(Path.Combine(output, "index.md"));
            index.ShouldStartWith("---\nlayout: api\n---\n<!-- index:API Reference -->");
            index.ShouldContain("# API Reference");
            index.ShouldEndWith("<!-- end -->");

            var type = await File.ReadAllTextAsync(Path.Combine(output, "Temp.Widget.md"));
            type.ShouldStartWith("---\nlayout: api\n---\n<!-- type:Widget -->");
            type.ShouldContain("# Widget");
            type.ShouldEndWith("<!-- end -->");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task CustomTemplateRenderer_IsAppliedToSingleFile()
    {
        var root = CreateTestRoot();

        try
        {
            var xmlPath = Path.Combine(root, "input.xml");
            var output = Path.Combine(root, "api.md");
            await File.WriteAllTextAsync(xmlPath, FixtureXml, new UTF8Encoding(false));
            var model = Xml2Doc.Core.Models.Xml2Doc.Load(xmlPath);
            var renderer = new MarkdownRenderer(
                model,
                new RendererOptions(
                    TemplateRenderer: new PrefixTemplateRenderer()));

            renderer.RenderToSingleFile(output);

            var markdown = await File.ReadAllTextAsync(output);
            markdown.ShouldStartWith("singlefile:API Reference\n# API Reference");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CustomRendererAndFileOptions_AreRejected()
    {
        var model = LoadModel();

        Should.Throw<ArgumentException>(() =>
            new MarkdownRenderer(
                model,
                new RendererOptions(
                    TemplatePath: "template.md",
                    TemplateRenderer: new PrefixTemplateRenderer())));
    }

    [Fact]
    public async Task TemplateWithoutContentToken_IsRejected()
    {
        var root = CreateTestRoot();

        try
        {
            var templatePath = Path.Combine(root, "template.md");
            await File.WriteAllTextAsync(templatePath, "missing token");

            Should.Throw<InvalidDataException>(() =>
                new FileTemplateRenderer(templatePath, frontMatterPath: null));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static Xml2Doc.Core.Models.Xml2Doc LoadModel()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, FixtureXml);

        try
        {
            return Xml2Doc.Core.Models.Xml2Doc.Load(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateTestRoot()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "Xml2Doc.Tests",
            Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        return root;
    }

    private sealed class PrefixTemplateRenderer : ITemplateRenderer
    {
        public string Render(TemplateRenderContext context) =>
            $"{context.Kind.ToString().ToLowerInvariant()}:{context.Title}\n{context.Content}";
    }

    private const string FixtureXml = """
        <doc>
          <members>
            <member name="T:Temp.Widget">
              <summary>A widget.</summary>
            </member>
          </members>
        </doc>
        """;
}
