using Shouldly;
using System.Text;
using Xml2Doc.Core;
using Xml2Doc.Core.Templates;
using Xunit;

namespace Xml2Doc.Tests;

public class TemplateRendererTests
{
    [Fact]
    public void ExistingThreeArgumentContextConstruction_RemainsCompatible()
    {
        var context = new TemplateRenderContext(
            "content",
            "title",
            TemplateDocumentKind.Type);

        var (content, title, kind) = context;

        content.ShouldBe("content");
        title.ShouldBe("title");
        kind.ShouldBe(TemplateDocumentKind.Type);
        context.Document.ShouldBeNull();
        context.OutputPath.ShouldBeNull();
    }

    [Fact]
    public void DocumentDescriptor_RequiresStableIdentity()
    {
        var exception = Should.Throw<ArgumentException>(() =>
            new DocumentDescriptor(TemplateDocumentKind.Type, " "));

        exception.ParamName.ShouldBe("documentId");
    }

    [Fact]
    public async Task DirectoryRendering_ProvidesMetadataForEveryDocumentKind()
    {
        var root = CreateTestRoot();

        try
        {
            var xmlPath = Path.Join(root, "input.xml");
            var output = Path.Join(root, "docs");
            await File.WriteAllTextAsync(xmlPath, FixtureXml, new UTF8Encoding(false));

            var templateRenderer = new RecordingTemplateRenderer();
            var frontMatterContexts = new List<TemplateRenderContext>();
            var model = Xml2Doc.Core.Models.Xml2Doc.Load(xmlPath);
            var renderer = new MarkdownRenderer(
                model,
                new RendererOptions(
                    TemplateRenderer: templateRenderer,
                    FrontMatter: context =>
                    {
                        frontMatterContexts.Add(context);
                        return new Dictionary<string, object?>();
                    },
                    EmitNamespaceIndex: true));

            renderer.RenderToDirectory(output);

            templateRenderer.Contexts.Count.ShouldBe(4);
            frontMatterContexts.Count.ShouldBe(templateRenderer.Contexts.Count);
            for (var index = 0; index < frontMatterContexts.Count; index++)
            {
                ReferenceEquals(
                    frontMatterContexts[index],
                    templateRenderer.Contexts[index]).ShouldBeTrue();
            }

            AssertContext(
                templateRenderer.Contexts.Single(
                    context => context.Kind == TemplateDocumentKind.Type),
                documentId: "T:Temp.Widget",
                @namespace: "Temp",
                symbol: "Widget",
                outputPath: "Temp.Widget.md");
            AssertContext(
                templateRenderer.Contexts.Single(
                    context => context.Kind == TemplateDocumentKind.Index),
                documentId: "xml2doc:index",
                @namespace: null,
                symbol: null,
                outputPath: "index.md");
            AssertContext(
                templateRenderer.Contexts.Single(
                    context => context.Kind == TemplateDocumentKind.NamespaceIndex),
                documentId: "N:Temp",
                @namespace: "Temp",
                symbol: null,
                outputPath: "namespaces/Temp.md");
            AssertContext(
                templateRenderer.Contexts.Single(
                    context => context.Kind == TemplateDocumentKind.NamespaceOverview),
                documentId: "xml2doc:namespaces",
                @namespace: null,
                symbol: null,
                outputPath: "namespaces.md");

            var firstRenderContexts = templateRenderer.Contexts.ToArray();
            templateRenderer.Contexts.Clear();
            frontMatterContexts.Clear();

            renderer.RenderToDirectory(output);

            templateRenderer.Contexts.Count.ShouldBe(firstRenderContexts.Length);
            for (var index = 0; index < firstRenderContexts.Length; index++)
                templateRenderer.Contexts[index].ShouldBe(firstRenderContexts[index]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SingleFileRendering_ProvidesResolvedAndInMemoryMetadata()
    {
        var root = CreateTestRoot();

        try
        {
            var xmlPath = Path.Join(root, "input.xml");
            var output = Path.Join(root, "docs", "api.md");
            await File.WriteAllTextAsync(xmlPath, FixtureXml, new UTF8Encoding(false));

            var templateRenderer = new RecordingTemplateRenderer();
            var model = Xml2Doc.Core.Models.Xml2Doc.Load(xmlPath);
            var renderer = new MarkdownRenderer(
                model,
                new RendererOptions(TemplateRenderer: templateRenderer));

            renderer.RenderToSingleFile(output);
            _ = renderer.RenderToString();

            templateRenderer.Contexts.Count.ShouldBe(2);
            AssertContext(
                templateRenderer.Contexts[0],
                documentId: "xml2doc:single-file",
                @namespace: null,
                symbol: null,
                outputPath: "api.md");
            AssertContext(
                templateRenderer.Contexts[1],
                documentId: "xml2doc:single-file",
                @namespace: null,
                symbol: null,
                outputPath: null);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

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
    public async Task FileTemplate_CanConsumeDocumentMetadataTokens()
    {
        var root = CreateTestRoot();

        try
        {
            var xmlPath = Path.Join(root, "input.xml");
            var templatePath = Path.Join(root, "template.md");
            var output = Path.Join(root, "docs");

            await File.WriteAllTextAsync(xmlPath, FixtureXml, new UTF8Encoding(false));
            await File.WriteAllTextAsync(
                templatePath,
                "{{documentId}}|{{namespace}}|{{symbol}}|{{outputPath}}\n{{content}}",
                new UTF8Encoding(false));

            var model = Xml2Doc.Core.Models.Xml2Doc.Load(xmlPath);
            var renderer = new MarkdownRenderer(
                model,
                new RendererOptions(
                    TemplatePath: templatePath,
                    GenerateIndex: false));

            renderer.RenderToDirectory(output);

            var type = await File.ReadAllTextAsync(Path.Join(output, "Temp.Widget.md"));
            type.ShouldStartWith(
                "T:Temp.Widget|Temp|Widget|Temp.Widget.md\n# Widget");
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

    private static void AssertContext(
        TemplateRenderContext context,
        string documentId,
        string? @namespace,
        string? symbol,
        string? outputPath)
    {
        var document = context.Document;
        document.ShouldNotBeNull();
        document!.Kind.ShouldBe(context.Kind);
        document.DocumentId.ShouldBe(documentId);
        document.Namespace.ShouldBe(@namespace);
        document.Symbol.ShouldBe(symbol);
        context.OutputPath.ShouldBe(outputPath);
    }

    private sealed class RecordingTemplateRenderer : ITemplateRenderer
    {
        public List<TemplateRenderContext> Contexts { get; } = new();

        public string Render(TemplateRenderContext context)
        {
            Contexts.Add(context);
            return context.Content;
        }
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
