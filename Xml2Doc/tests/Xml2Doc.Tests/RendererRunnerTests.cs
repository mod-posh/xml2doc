using Shouldly;
using System.Xml.Linq;
using Xml2Doc.Core;
using Xml2Doc.Core.Models;
using Xml2Doc.Core.OutputLifecycle;
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
        result.PlanningElapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        result.RenderingElapsed.ShouldBe(TimeSpan.Zero);
        result.LifecycleElapsed.ShouldBe(TimeSpan.Zero);
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
    public void Run_RepeatedGenerationSkipsUnchangedFilesAndPreservesTimestamps()
    {
        using var output = TemporaryDirectory.Create();
        var runner = CreateRunner();
        var request = new RendererRunRequest(output.Path);
        var first = runner.Run(request);
        var preservedTimestamp = new DateTime(
            2020,
            1,
            2,
            3,
            4,
            5,
            DateTimeKind.Utc);
        foreach (var path in first.WrittenFiles)
            File.SetLastWriteTimeUtc(path, preservedTimestamp);

        var second = runner.Run(request);

        second.WrittenFiles.ShouldBeEmpty();
        second.SkippedFiles.ShouldBe(second.PlannedFiles);
        second.SkippedFiles.All(path =>
            File.GetLastWriteTimeUtc(path) == preservedTimestamp)
            .ShouldBeTrue();
    }

    [Fact]
    public void Run_ChangedExistingFileIsRewrittenAndReported()
    {
        using var output = TemporaryDirectory.Create();
        var runner = CreateRunner();
        var request = new RendererRunRequest(output.Path);
        var first = runner.Run(request);
        var changedPath = first.PlannedFiles[0];
        File.WriteAllText(changedPath, "changed");

        var second = runner.Run(request);

        second.WrittenFiles.ShouldBe(new[] { changedPath });
        second.SkippedFiles.ShouldBe(
            second.PlannedFiles.Skip(1).ToArray());
        File.ReadAllText(changedPath).ShouldNotBe("changed");
    }

    [Fact]
    public void Run_WithPruningReportsOnlyFilesActuallyDeleted()
    {
        using var output = TemporaryDirectory.Create();
        Directory.CreateDirectory(output.Path);
        var stalePath = Path.Join(output.Path, "Stale.md");
        File.WriteAllText(stalePath, "stale");
        var location = OutputManifestLocation.Create(
            output.Path,
            "runner-tests");
        OutputManifestStore.Save(
            location,
            new[] { "Missing.md", "Stale.md" });
        var runner = CreateRunner(new RendererOptions(
            PruneStaleFiles: true,
            ManifestIdentity: "runner-tests"));

        var result = runner.Run(new RendererRunRequest(output.Path));

        result.PrunedFiles.ShouldBe(new[] { stalePath });
        File.Exists(stalePath).ShouldBeFalse();
        result.PlanningElapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        result.RenderingElapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        result.LifecycleElapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void Run_ParallelAndSerialPerTypeOutputsAreByteIdentical()
    {
        using var serialOutput = TemporaryDirectory.Create();
        using var parallelOutput = TemporaryDirectory.Create();

        var serialResult = CreateRunner(new RendererOptions(
            ParallelDegree: 1)).Run(
                new RendererRunRequest(serialOutput.Path));
        var parallelResult = CreateRunner(new RendererOptions(
            ParallelDegree: 4)).Run(
                new RendererRunRequest(parallelOutput.Path));

        var serialNames = serialResult.PlannedFiles
            .Select(Path.GetFileName)
            .ToArray();
        var parallelNames = parallelResult.PlannedFiles
            .Select(Path.GetFileName)
            .ToArray();
        parallelNames.ShouldBe(serialNames);

        foreach (var fileName in serialNames)
        {
            File.ReadAllBytes(Path.Join(serialOutput.Path, fileName!))
                .ShouldBe(File.ReadAllBytes(
                    Path.Join(parallelOutput.Path, fileName!)));
        }
    }

    [Fact]
    public void Run_RepeatedParallelGenerationReportsDeterministicSkips()
    {
        using var output = TemporaryDirectory.Create();
        var runner = CreateRunner(new RendererOptions(ParallelDegree: 4));
        var request = new RendererRunRequest(output.Path);
        runner.Run(request);

        var second = runner.Run(request);

        second.WrittenFiles.ShouldBeEmpty();
        second.SkippedFiles.ShouldBe(second.PlannedFiles);
    }

    [Fact]
    public void Run_RelativeSingleFileExecutesTheAbsolutePlan()
    {
        var relativeOutput =
            "xml2doc-runner-" + Guid.NewGuid().ToString("N") + ".md";
        var absoluteOutput = System.IO.Path.GetFullPath(relativeOutput);

        try
        {
            var result = CreateRunner().Run(new RendererRunRequest(
                relativeOutput,
                RendererRunMode.SingleFile));

            result.PlannedFiles.ShouldBe(new[] { absoluteOutput });
            result.WrittenFiles.ShouldBe(result.PlannedFiles);
            File.Exists(absoluteOutput).ShouldBeTrue();
        }
        finally
        {
            if (File.Exists(absoluteOutput))
                File.Delete(absoluteOutput);
        }
    }

    [Fact]
    public void Run_RelativePerTypeDirectoryExecutesTheAbsolutePlan()
    {
        var relativeOutput =
            "xml2doc-runner-" + Guid.NewGuid().ToString("N");
        var absoluteOutput = System.IO.Path.GetFullPath(relativeOutput);

        try
        {
            var result = CreateRunner().Run(
                new RendererRunRequest(relativeOutput));

            result.PlannedFiles.All(path =>
                path.StartsWith(
                    absoluteOutput + System.IO.Path.DirectorySeparatorChar,
                    StringComparison.Ordinal)).ShouldBeTrue();
            result.WrittenFiles.ShouldBe(result.PlannedFiles);
            result.WrittenFiles.All(File.Exists).ShouldBeTrue();
        }
        finally
        {
            if (Directory.Exists(absoluteOutput))
                Directory.Delete(absoluteOutput, recursive: true);
        }
    }

    [Fact]
    public void Plan_RejectsMissingOutputPathBeforeWriting()
    {
        var runner = CreateRunner();

        var exception = Should.Throw<ArgumentException>(() =>
            runner.Plan(new RendererRunRequest(" ")));

        exception.ParamName.ShouldBe("OutputPath");
    }

    private static RendererRunner CreateRunner(
        RendererOptions? options = null)
    {
        var model = new Xml2Doc.Core.Models.Xml2Doc();
        AddType(model, "T:Temp.Zebra");
        AddType(model, "T:Temp.Alpha");
        return new RendererRunner(new MarkdownRenderer(model, options));
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
