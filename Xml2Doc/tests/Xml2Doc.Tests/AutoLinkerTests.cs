using Shouldly;
using System.Text;
using Xml2Doc.Core;
using Xml2Doc.Core.AutoLinking;
using Xunit;

namespace Xml2Doc.Tests;

public class AutoLinkerTests
{
    [Fact]
    public void SimpleAutoLinker_SkipsProtectedMarkdownAndPartialIdentifiers()
    {
        var markdown = """
            Widget calls Run(string).
            `Widget` and [Widget](existing.md) and WidgetFactory
            ```csharp
            Widget calls Run(string).
            ```
            ~~~text
            Widget
            ~~~
            """;
        var context = new AutoLinkContext(new[]
        {
            new AutoLinkTarget("Widget", "Widget.md"),
            new AutoLinkTarget("Run(string)", "Widget.md#run")
        });

        var result = SimpleAutoLinker.Instance
            .Apply(markdown, context)
            .ReplaceLineEndings("\n");

        result.ShouldContain("[Widget](Widget.md) calls [Run(string)](Widget.md#run).");
        result.ShouldContain("`Widget` and [Widget](existing.md) and WidgetFactory");
        result.ShouldContain("```csharp\nWidget calls Run(string).\n```");
        result.ShouldContain("~~~text\nWidget\n~~~");
    }

    [Fact]
    public void SimpleAutoLinker_DoesNotCloseLongFenceWithShorterFence()
    {
        var markdown = """
            ````csharp
            Widget
            ```
            Widget
            ````
            Widget
            """;
        var context = new AutoLinkContext(new[]
        {
            new AutoLinkTarget("Widget", "Widget.md")
        });

        var result = SimpleAutoLinker.Instance
            .Apply(markdown, context)
            .ReplaceLineEndings("\n");

        result.ShouldBe(
            "````csharp\nWidget\n```\nWidget\n````\n[Widget](Widget.md)");
    }

    [Fact]
    public void SimpleAutoLinker_PreparesTargetsOncePerContext()
    {
        var targets = new TrackingTargets(
            new AutoLinkTarget("Widget", "Widget.md"));
        var context = new AutoLinkContext(targets);

        SimpleAutoLinker.Instance.Apply("Widget", context);
        SimpleAutoLinker.Instance.Apply("Widget again", context);

        targets.EnumerationCount.ShouldBe(1);
    }

    [Fact]
    public async Task Renderer_AutoLinksWithModeSpecificTargets()
    {
        var root = CreateTestRoot();

        try
        {
            var xmlPath = Path.Join(root, "input.xml");
            await File.WriteAllTextAsync(xmlPath, FixtureXml, new UTF8Encoding(false));
            var model = Xml2Doc.Core.Models.Xml2Doc.Load(xmlPath);

            var directory = Path.Join(root, "docs");
            new MarkdownRenderer(model, new RendererOptions(AutoLink: true))
                .RenderToDirectory(directory);
            var perType = await File.ReadAllTextAsync(
                Path.Join(directory, "Temp.Consumer.md"));

            perType.ShouldContain("[Widget](Temp.Widget.md)");
            perType.ShouldContain(
                "[Run(string)](Temp.Widget.md#temp.widget.run(string))");
            perType.ShouldContain("`Widget`");
            perType.ShouldContain("`T`");

            var singleFile = Path.Join(root, "api.md");
            new MarkdownRenderer(model, new RendererOptions(AutoLink: true))
                .RenderToSingleFile(singleFile);
            var single = await File.ReadAllTextAsync(singleFile);

            single.ShouldContain("[Widget](#widget)");
            single.ShouldContain("[Run(string)](#temp.widget.run(string))");
            single.ShouldContain("`Widget`");
            single.ShouldContain("`T`");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Renderer_LeavesFreeTextUnchangedByDefault()
    {
        var root = CreateTestRoot();

        try
        {
            var xmlPath = Path.Join(root, "input.xml");
            await File.WriteAllTextAsync(xmlPath, FixtureXml, new UTF8Encoding(false));
            var model = Xml2Doc.Core.Models.Xml2Doc.Load(xmlPath);
            var output = Path.Join(root, "docs");

            new MarkdownRenderer(model).RenderToDirectory(output);
            var markdown = await File.ReadAllTextAsync(
                Path.Join(output, "Temp.Consumer.md"));

            markdown.ShouldContain("Widget calls Run(string).");
            markdown.ShouldNotContain("[Widget](Temp.Widget.md)");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Renderer_UsesCustomAutoLinkerWhenEnabled()
    {
        var model = LoadModel();
        var renderer = new MarkdownRenderer(
            model,
            new RendererOptions(
                AutoLink: true,
                AutoLinker: new PrefixAutoLinker()));

        renderer.RenderToString().ShouldContain("linked:Widget calls Run(string).");
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

    private sealed class PrefixAutoLinker : IAutoLinker
    {
        public string Apply(string markdown, AutoLinkContext context) =>
            "linked:" + markdown;
    }

    private sealed class TrackingTargets : IReadOnlyList<AutoLinkTarget>
    {
        private readonly AutoLinkTarget[] _targets;

        public TrackingTargets(params AutoLinkTarget[] targets) =>
            _targets = targets;

        public int EnumerationCount { get; private set; }
        public int Count => _targets.Length;
        public AutoLinkTarget this[int index] => _targets[index];

        public IEnumerator<AutoLinkTarget> GetEnumerator()
        {
            EnumerationCount++;
            return ((IEnumerable<AutoLinkTarget>)_targets).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }

    private const string FixtureXml = """
        <doc>
          <members>
            <member name="T:Temp.Widget">
              <summary>A widget.</summary>
            </member>
            <member name="M:Temp.Widget.Run(System.String)">
              <summary>Runs a widget.</summary>
            </member>
            <member name="T:Temp.Consumer">
              <summary>Widget calls Run(string). Use <c>Widget</c> with <typeparamref name="T"/>.</summary>
            </member>
          </members>
        </doc>
        """;
}
