using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Shouldly;
using System.Collections;
using System.Text.Json;
using System.Xml.Linq;
using Xml2Doc.MSBuild;
using Xunit;

namespace Xml2Doc.Tests;

public class GenerateMarkdownFromXmlDocsTests
{
    private sealed class TestBuildEngine : IBuildEngine
    {
        public List<BuildErrorEventArgs> Errors { get; } = new();
        public List<BuildWarningEventArgs> Warnings { get; } = new();
        public bool ContinueOnError => false;
        public int LineNumberOfTaskNode => 0;
        public int ColumnNumberOfTaskNode => 0;
        public string ProjectFileOfTaskNode => string.Empty;

        public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e);
        public void LogWarningEvent(BuildWarningEventArgs e) => Warnings.Add(e);
        public void LogMessageEvent(BuildMessageEventArgs e) { }
        public void LogCustomEvent(CustomBuildEventArgs e) { }

        public bool BuildProjectFile(
            string projectFileName,
            string[] targetNames,
            IDictionary globalProperties,
            IDictionary targetOutputs) => true;
    }

    private static string RepositoryRoot =>
        Path.GetFullPath(
            AppContext.BaseDirectory +
            ".." + Path.DirectorySeparatorChar +
            ".." + Path.DirectorySeparatorChar +
            ".." + Path.DirectorySeparatorChar +
            ".." + Path.DirectorySeparatorChar +
            "..");

    [Fact]
    public void AggregateInputs_RenderOneCanonicalOutputRegardlessOfInputOrder()
    {
        var root = CreateRoot();
        var alpha = Path.Combine(root, "Alpha.xml");
        var zulu = Path.Combine(root, "Zulu.xml");
        var firstOutput = Path.Combine(root, "first");
        var secondOutput = Path.Combine(root, "second");
        var report = Path.Combine(root, "aggregate-report.json");

        File.WriteAllText(alpha, """
            <doc><members>
              <member name="T:Alpha.Api.Widget"><summary>Alpha widget.</summary></member>
            </members></doc>
            """);
        File.WriteAllText(zulu, """
            <doc><members>
              <member name="T:Zulu.Api.Widget"><summary>Zulu widget.</summary></member>
            </members></doc>
            """);

        try
        {
            var reverse = CreateTask(
                firstOutput,
                new TaskItem(zulu),
                new TaskItem(alpha));
            reverse.ReportPath = report;

            reverse.Execute().ShouldBeTrue();
            reverse.DidWork.ShouldBeTrue();

            var forward = CreateTask(
                secondOutput,
                new TaskItem(alpha),
                new TaskItem(zulu));
            forward.Execute().ShouldBeTrue();

            var firstFiles = Directory.GetFiles(firstOutput, "*.md")
                .Select(Path.GetFileName)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            var secondFiles = Directory.GetFiles(secondOutput, "*.md")
                .Select(Path.GetFileName)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            firstFiles.ShouldBe(secondFiles);
            firstFiles.ShouldContain("Alpha.Api.Widget.md");
            firstFiles.ShouldContain("Zulu.Api.Widget.md");
            firstFiles.ShouldContain("index.md");

            foreach (var file in firstFiles)
            {
                File.ReadAllBytes(Path.Combine(firstOutput, file!))
                    .ShouldBe(File.ReadAllBytes(Path.Combine(secondOutput, file!)));
            }

            var index = File.ReadAllText(Path.Combine(firstOutput, "index.md"));
            var alphaIndex = index.IndexOf("Alpha.Api.Widget", StringComparison.Ordinal);
            var zuluIndex = index.IndexOf("Zulu.Api.Widget", StringComparison.Ordinal);
            alphaIndex.ShouldBeGreaterThanOrEqualTo(0);
            zuluIndex.ShouldBeGreaterThan(alphaIndex);

            using var reportDocument = JsonDocument.Parse(File.ReadAllText(report));
            var inputs = reportDocument.RootElement
                .GetProperty("xmlInputs")
                .EnumerateArray()
                .Select(element => element.GetString())
                .ToArray();
            inputs.ShouldBe(new[]
            {
                Path.GetFullPath(alpha),
                Path.GetFullPath(zulu)
            });
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void AggregateInputs_WhenAnyInputIsMissing_FailsWithoutPartialOutput()
    {
        var root = CreateRoot();
        var alpha = Path.Combine(root, "Alpha.xml");
        var missing = Path.Combine(root, "Missing.xml");
        var output = Path.Combine(root, "docs");
        Directory.CreateDirectory(root);
        File.WriteAllText(alpha, """
            <doc><members>
              <member name="T:Alpha.Api.Widget"><summary>Alpha widget.</summary></member>
            </members></doc>
            """);
        var buildEngine = new TestBuildEngine();

        try
        {
            var task = CreateTask(
                output,
                new TaskItem(alpha),
                new TaskItem(missing));
            task.BuildEngine = buildEngine;

            task.Execute().ShouldBeFalse();
            task.DidWork.ShouldBeFalse();
            Directory.Exists(output).ShouldBeFalse();
            buildEngine.Errors.ShouldContain(error =>
                error.Message!.Contains("aggregate XML input not found", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void BuildAssets_ExposeRepositoryAggregationAndIndexOwnershipDiagnostic()
    {
        var buildDirectory = Path.Combine(
            RepositoryRoot,
            "src",
            "Xml2Doc.MSBuild",
            "build");
        var targets = XDocument.Load(Path.Combine(
            buildDirectory,
            "Xml2Doc.MSBuild.Aggregation.targets"));
        var project = XDocument.Load(Path.Combine(
            RepositoryRoot,
            "src",
            "Xml2Doc.MSBuild",
            "Xml2Doc.MSBuild.csproj"));

        targets.Descendants("Xml2Doc_AggregateEnabled")
            .Single().Value.ShouldBe("false");
        targets.Descendants("Xml2Doc_AggregateValidateIndexOwnership")
            .Single().Value.ShouldBe("true");

        var aggregateTarget = targets.Descendants("Target")
            .Single(element => element.Attribute("Name")?.Value == "Xml2Doc_Aggregate");
        aggregateTarget.Attribute("AfterTargets")!.Value.ShouldBe("Build");
        aggregateTarget.Attribute("Inputs")!.Value.ShouldContain("@(_Xml2Doc_AggregateInput)");

        var task = aggregateTarget.Descendants("GenerateMarkdownFromXmlDocs").Single();
        task.Attribute("XmlPaths")!.Value.ShouldBe("@(_Xml2Doc_AggregateInput)");
        task.Attribute("GenerateIndex")!.Value.ShouldBe("$(Xml2Doc_GenerateIndex)");

        targets.Descendants("Error")
            .Any(element =>
                element.Attribute("Text")?.Value.Contains("XML2DOC007", StringComparison.Ordinal) == true)
            .ShouldBeTrue();

        project.Descendants("None")
            .Any(element =>
                element.Attribute("Include")?.Value == "build\\Xml2Doc.MSBuild.Aggregation.targets" &&
                element.Attribute("Pack")?.Value == "true")
            .ShouldBeTrue();
    }

    private static GenerateMarkdownFromXmlDocs CreateTask(
        string outputDirectory,
        params TaskItem[] inputs)
        => new()
        {
            BuildEngine = new TestBuildEngine(),
            XmlPaths = inputs,
            OutputDirectory = outputDirectory,
            FileNameMode = "clean"
        };

    private static string CreateRoot()
        => Path.Combine(
            Path.GetTempPath(),
            "Xml2Doc.Tests",
            Path.GetRandomFileName());
}
