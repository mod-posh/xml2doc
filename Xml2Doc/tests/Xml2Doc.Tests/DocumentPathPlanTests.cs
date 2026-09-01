using Shouldly;
using Xml2Doc.Core;
using Xml2Doc.Core.Diagnostics;
using Xml2Doc.Core.Paths;
using Xunit;

namespace Xml2Doc.Tests;

public class DocumentPathPlanTests
{
    [Fact]
    public void NamespaceFolders_UsesOnePlanForFilesAndRelativeLinks()
    {
        var model = LoadModel(
            """
            <doc><members>
              <member name="T:Alpha.Root.Widget">
                <summary>Uses <see cref="T:Alpha.Other.Helper"/>.</summary>
              </member>
              <member name="T:Alpha.Other.Helper">
                <summary>Helps widgets.</summary>
              </member>
            </members></doc>
            """);
        var renderer = new MarkdownRenderer(
            model,
            new RendererOptions(
                EmitNamespaceIndex: true,
                Layout: DocumentLayout.NamespaceFolders));
        var output = TemporaryDirectory();

        try
        {
            renderer.DocumentPlan.Select(entry => entry.Path).ShouldBe(new[]
            {
                "namespaces/Alpha/Other/Helper.md",
                "namespaces/Alpha/Root/Widget.md",
                "index.md",
                "namespaces/Alpha/Other/index.md",
                "namespaces/Alpha/Root/index.md",
                "namespaces.md"
            });

            var planned = renderer.PlanOutputs(output);
            renderer.RenderToDirectory(output);

            var actual = Directory.GetFiles(output, "*.md", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            actual.ShouldBe(planned.OrderBy(path => path, StringComparer.Ordinal));

            File.ReadAllText(Path.Join(
                    output,
                    "namespaces",
                    "Alpha",
                    "Root",
                    "Widget.md"))
                .ShouldContain("[Helper](../Other/Helper.md)");
            File.ReadAllText(Path.Join(output, "index.md"))
                .ShouldContain("namespaces/Alpha/Root/Widget.md");
            File.ReadAllText(Path.Join(output, "namespaces.md"))
                .ShouldContain("namespaces/Alpha/Root/index.md");
        }
        finally
        {
            Directory.Delete(output, recursive: true);
        }
    }

    [Fact]
    public void FlatLayout_PreservesCompatiblePaths()
    {
        var renderer = new MarkdownRenderer(
            LoadModel("""
                <doc><members>
                  <member name="T:Alpha.Root.Widget"><summary>A widget.</summary></member>
                </members></doc>
                """),
            new RendererOptions(Layout: DocumentLayout.Flat));

        renderer.DocumentPlan.Select(entry => entry.Path).ShouldBe(new[]
        {
            "Alpha.Root.Widget.md",
            "index.md"
        });
    }

    [Fact]
    public void NamespaceFolders_HonorsConfiguredRootNamespaceTrimming()
    {
        var renderer = new MarkdownRenderer(
            LoadModel("""
                <doc><members>
                  <member name="T:Alpha.Root.Widget"><summary>A widget.</summary></member>
                </members></doc>
                """),
            new RendererOptions(
                RootNamespaceToTrim: "Alpha",
                TrimRootNamespaceInFileNames: true,
                Layout: DocumentLayout.NamespaceFolders));

        renderer.DocumentPlan.Get("T:Alpha.Root.Widget").Path
            .ShouldBe("namespaces/Root/Widget.md");
    }

    [Theory]
    [InlineData("../outside.md")]
    [InlineData("/rooted.md")]
    [InlineData("folder\\windows.md")]
    public void CustomResolver_RejectsUnsafePathsBeforeWriting(string path)
    {
        var sink = new RecordingSink();
        var output = Path.Join(
            Path.GetTempPath(),
            "Xml2Doc.Tests",
            Path.GetFileName(Path.GetRandomFileName()));
        var exception = Should.Throw<DocumentPathException>(() =>
        {
            new MarkdownRenderer(
                LoadModel("""
                    <doc><members>
                      <member name="T:Alpha.Widget"><summary>A widget.</summary></member>
                    </members></doc>
                    """),
                new RendererOptions(
                    DiagnosticSink: sink,
                    DocumentPathResolver: new ConstantResolver(path)))
                .RenderToDirectory(output);
        });

        exception.DiagnosticCode.ShouldBe(DiagnosticIds.UnsafeDocumentPath);
        sink.Diagnostics.Single().Code.ShouldBe(DiagnosticIds.UnsafeDocumentPath);
        Directory.Exists(output).ShouldBeFalse();
    }

    [Fact]
    public void CustomResolver_RejectsCaseInsensitiveCollisionsOnEveryHost()
    {
        var exception = Should.Throw<DocumentPathException>(() =>
        {
            _ = new MarkdownRenderer(
                LoadModel("""
                    <doc><members>
                      <member name="T:Alpha.First"><summary>First.</summary></member>
                      <member name="T:Alpha.Second"><summary>Second.</summary></member>
                    </members></doc>
                    """),
                new RendererOptions(
                    GenerateIndex: false,
                    DocumentPathResolver: new CaseCollisionResolver()))
                .DocumentPlan;
        });

        exception.DiagnosticCode.ShouldBe(DiagnosticIds.DuplicateDocumentPath);
    }

    [Fact]
    public void SingleFile_DoesNotConsultMultiDocumentLayoutResolver()
    {
        var renderer = new MarkdownRenderer(
            LoadModel("""
                <doc><members>
                  <member name="T:Alpha.Widget"><summary>A widget.</summary></member>
                </members></doc>
                """),
            new RendererOptions(
                DocumentPathResolver: new ConstantResolver("../outside.md")));

        renderer.RenderToString().ShouldContain("# Widget");
    }

    private static Xml2Doc.Core.Models.Xml2Doc LoadModel(string xml)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, xml);
        try
        {
            return Xml2Doc.Core.Models.Xml2Doc.Load(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string TemporaryDirectory()
    {
        var path = Path.Join(
            Path.GetTempPath(),
            "Xml2Doc.Tests",
            Path.GetFileName(Path.GetRandomFileName()));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class ConstantResolver(string path) : IDocumentPathResolver
    {
        public string GetPath(DocumentPathContext context) => path;
    }

    private sealed class CaseCollisionResolver : IDocumentPathResolver
    {
        public string GetPath(DocumentPathContext context) =>
            context.Document.DocumentId.EndsWith("First", StringComparison.Ordinal)
                ? "Types/Widget.md"
                : "types/widget.md";
    }

    private sealed class RecordingSink : IDiagnosticSink
    {
        public List<Xml2DocDiagnostic> Diagnostics { get; } = new();

        public void Report(Xml2DocDiagnostic diagnostic) =>
            Diagnostics.Add(diagnostic);
    }
}
