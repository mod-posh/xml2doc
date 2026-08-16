using Shouldly;
using System.Xml.Linq;
using Xml2Doc.Core;
using Xml2Doc.Core.Models;
using Xml2Doc.Core.Pipeline;
using Xunit;

namespace Xml2Doc.Tests;

public class RendererRunnerTests
{
    [Fact]
    public void Plan_ReturnsRendererPlanWithoutWriting()
    {
        using var output = TemporaryDirectory.Create();
        var runner = CreateRunner();
        var request = new RendererRunRequest(output.Path);

        var planned = runner.Plan(request);

        planned.ShouldBe(new[]
        {
            Path.Join(output.Path, "Temp.Alpha.md"),
            Path.Join(output.Path, "Temp.Zebra.md"),
            Path.Join(output.Path, "index.md")
        });
        Directory.Exists(output.Path).ShouldBeFalse();
    }

    [Fact]
    public void Run_DryRunReportsPlanWithoutWriting()
    {
        using var output = TemporaryDirectory.Create();
        var runner = CreateRunner();

        var result = runner.Run(new RendererRunRequest(
            output.Path,
            DryRun: true));

        result.DryRun.ShouldBeTrue();
        result.PlannedFiles.Count.ShouldBe(3);
        result.WrittenFiles.ShouldBeEmpty();
        result.SkippedFiles.ShouldBeEmpty();
        result.PrunedFiles.ShouldBeEmpty();
        result.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        Directory.Exists(output.Path).ShouldBeFalse();
    }

    [Fact]
    public void Run_PerTypeWritesEveryPlannedOutput()
    {
        using var output = TemporaryDirectory.Create();
        var runner = CreateRunner();

        var result = runner.Run(new RendererRunRequest(output.Path));

        result.DryRun.ShouldBeFalse();
        result.WrittenFiles.ShouldBe(result.PlannedFiles);
        result.WrittenFiles.All(File.Exists).ShouldBeTrue();
    }

    [Fact]
    public void Run_SingleFileWritesThePlannedOutput()
    {
        using var output = TemporaryDirectory.Create();
        var outputFile = Path.Join(output.Path, "api.md");
        var runner = CreateRunner();

        var result = runner.Run(new RendererRunRequest(
            outputFile,
            RendererRunMode.SingleFile));

        result.PlannedFiles.ShouldBe(new[] { outputFile });
        result.WrittenFiles.ShouldBe(result.PlannedFiles);
        File.Exists(outputFile).ShouldBeTrue();
    }

    [Fact]
    public void Plan_RejectsMissingOutputPathBeforeWriting()
    {
        var runner = CreateRunner();

        var exception = Should.Throw<ArgumentException>(() =>
            runner.Plan(new RendererRunRequest(" ")));

        exception.ParamName.ShouldBe("OutputPath");
    }

    private static RendererRunner CreateRunner()
    {
        var model = new Xml2Doc.Core.Models.Xml2Doc();
        AddType(model, "T:Temp.Zebra");
        AddType(model, "T:Temp.Alpha");
        return new RendererRunner(new MarkdownRenderer(model));
    }

    private static void AddType(
        Xml2Doc.Core.Models.Xml2Doc model,
        string documentationId)
    {
        model.Members[documentationId] = new XMember(
            documentationId,
            new XElement(
                "member",
                new XAttribute("name", documentationId),
                new XElement("summary", "A documented type.")));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private TemporaryDirectory(string path) => Path = path;

        public string Path { get; }

        public static TemporaryDirectory Create() =>
            new(System.IO.Path.Join(
                System.IO.Path.GetTempPath(),
                "Xml2Doc.Tests",
                System.IO.Path.GetRandomFileName()));

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
