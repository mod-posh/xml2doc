using Shouldly;
using System.Text;
using Xml2Doc.Core;
using Xml2Doc.Core.Linking;
using Xunit;

namespace Xml2Doc.Tests;

public class ExternalLinkingTests
{
    [Fact]
    public async Task PreferExternalForUnknown_UsesExternalOnlyForUnknownCrefs()
    {
        var resolver = new RecordingExternalResolver();
        var markdown = await RenderAsync(new RendererOptions(
            LinkPolicy: LinkPolicy.PreferExternalForUnknown,
            ExternalSymbolResolver: resolver));

        markdown.ShouldContain("[Widget](Temp.Widget.md)");
        markdown.ShouldContain("[String](https://docs.example/System.String)");
        resolver.Requests.ShouldBe(new[] { "T:System.String" });
    }

    [Fact]
    public async Task InternalOnly_PreservesExistingBehaviorAndSkipsProvider()
    {
        var resolver = new RecordingExternalResolver();
        var markdown = await RenderAsync(new RendererOptions(
            ExternalSymbolResolver: resolver));

        markdown.ShouldContain("[Widget](Temp.Widget.md)");
        markdown.ShouldContain("[String](System.String.md)");
        resolver.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task PreferExternalForUnknown_FallsBackWhenProviderDeclines()
    {
        var markdown = await RenderAsync(new RendererOptions(
            LinkPolicy: LinkPolicy.PreferExternalForUnknown,
            ExternalSymbolResolver: new DecliningExternalResolver()));

        markdown.ShouldContain("[String](System.String.md)");
    }

    [Fact]
    public void BaseUrlResolver_AppendsEscapedIdentifierWithoutKindPrefix()
    {
        var resolver = new BaseUrlExternalSymbolResolver(
            "https://learn.microsoft.com/dotnet/api/");

        resolver.TryResolve("T:System.String", out var href).ShouldBeTrue();
        href.ShouldBe("https://learn.microsoft.com/dotnet/api/System.String");
    }

    private static async Task<string> RenderAsync(RendererOptions options)
    {
        var root = Path.Join(
            Path.GetTempPath(),
            "Xml2Doc.Tests",
            Path.GetFileName(Path.GetRandomFileName()));
        Directory.CreateDirectory(root);

        try
        {
            var xmlPath = Path.Join(root, "input.xml");
            await File.WriteAllTextAsync(xmlPath, FixtureXml, new UTF8Encoding(false));
            var model = Xml2Doc.Core.Models.Xml2Doc.Load(xmlPath);
            var output = Path.Join(root, "docs");

            new MarkdownRenderer(model, options).RenderToDirectory(output);
            return await File.ReadAllTextAsync(
                Path.Join(output, "Temp.Consumer.md"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class RecordingExternalResolver : IExternalSymbolResolver
    {
        public List<string> Requests { get; } = new();

        public bool TryResolve(string cref, out string? href)
        {
            Requests.Add(cref);
            href = cref == "T:System.String"
                ? "https://docs.example/System.String"
                : null;
            return href is not null;
        }
    }

    private sealed class DecliningExternalResolver : IExternalSymbolResolver
    {
        public bool TryResolve(string cref, out string? href)
        {
            href = null;
            return false;
        }
    }

    private const string FixtureXml = """
        <doc>
          <members>
            <member name="T:Temp.Consumer">
              <summary>
                See <see cref="T:Temp.Widget"/> and <see cref="T:System.String"/>.
              </summary>
            </member>
            <member name="T:Temp.Widget">
              <summary>A local widget.</summary>
            </member>
          </members>
        </doc>
        """;
}
