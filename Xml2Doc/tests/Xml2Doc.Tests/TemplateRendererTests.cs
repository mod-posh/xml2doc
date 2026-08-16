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
            var xmlPath = Path.Join(root, "input.xml");
            var templatePath = Path.Join(root, "template.md");
            var frontMatterPath = Path.Join(root, "front-matter.yml");
            var output = Path.Join(root, "docs");

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
                    FrontMatterPath: frontMatterPath,
                    EmitNamespaceIndex: true));

            renderer.RenderToDirectory(output);

            var index = await File.ReadAllTextAsync(Path.Join(output, "index.md"));
            index.ShouldStartWith("---\nlayout: api\n---\n<!-- index:API Reference -->");
            index.ShouldContain("# API Reference");
            index.ShouldEndWith("<!-- end -->");

            var type = await File.ReadAllTextAsync(Path.Join(output, "Temp.Widget.md"));
            type.ShouldStartWith("---\nlayout: api\n---\n<!-- type:Widget -->");
            type.ShouldContain("# Widget");
            type.ShouldEndWith("<!-- end -->");

            var namespaceOverview = await File.ReadAllTextAsync(
                Path.Join(output, "namespaces.md"));
            namespaceOverview.ShouldStartWith(
                "---\nlayout: api\n---\n<!-- namespaceoverview:Namespaces -->");
            namespaceOverview.ShouldContain("# Namespaces");

            var namespaceIndex = await File.ReadAllTextAsync(
                Path.Join(output, "namespaces", "Temp.md"));
            namespaceIndex.ShouldStartWith(
                "---\nlayout: api\n---\n<!-- namespaceindex:Temp -->");
            namespaceIndex.ShouldContain("# Temp");
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
            var xmlPath = Path.Join(root, "input.xml");
            var output = Path.Join(root, "api.md");
            await File.WriteAllTextAsync(xmlPath, FixtureXml, new UTF8Encoding(false));
            var model = Xml2Doc.Core.Models.Xml2Doc.Load(xmlPath);
            var renderer = new MarkdownRenderer(
                model,
                new RendererOptions(
                    TemplateRenderer: new PrefixTemplateRenderer(),
                    FrontMatter: context => new Dictionary<string, object?>
                    {
                        ["title"] = context.Title,
                        ["kind"] = context.Kind.ToString().ToLowerInvariant(),
                        ["draft"] = false
                    }));

            renderer.RenderToSingleFile(output);

            var markdown = await File.ReadAllTextAsync(output);
            markdown.ShouldStartWith(
                "---\n" +
                "draft: false\n" +
                "kind: \"singlefile\"\n" +
                "title: \"API Reference\"\n" +
                "---\n" +
                "singlefile:API Reference\n" +
                "# API Reference");
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
    public void FrontMatterDelegateAndFile_AreRejected()
    {
        var model = LoadModel();

        Should.Throw<ArgumentException>(() =>
            new MarkdownRenderer(
                model,
                new RendererOptions(
                    FrontMatterPath: "front-matter.yml",
                    FrontMatter: _ => new Dictionary<string, object?>())));
    }

    [Fact]
    public async Task TemplateWithoutContentToken_IsRejected()
    {
        var root = CreateTestRoot();

        try
        {
            var templatePath = Path.Join(root, "template.md");
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
        var root = Path.Join(
            Path.GetTempPath(),
            "Xml2Doc.Tests",
            Path.GetFileName(Path.GetRandomFileName()));
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
