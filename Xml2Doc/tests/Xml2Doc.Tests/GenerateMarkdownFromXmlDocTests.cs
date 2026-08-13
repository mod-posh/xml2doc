using Microsoft.Build.Framework;
using Shouldly;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xml2Doc.Core;
using Xml2Doc.Core.OutputLifecycle;
using Xml2Doc.MSBuild;
using Xunit;

namespace Xml2Doc.Tests
{
    public class GenerateMarkdownFromXmlDocTests
    {
        private sealed class TestBuildEngine : IBuildEngine
        {
            public bool ContinueOnError => false;
            public int LineNumberOfTaskNode => 0;
            public int ColumnNumberOfTaskNode => 0;
            public string ProjectFileOfTaskNode => string.Empty;

            public void LogErrorEvent(BuildErrorEventArgs e) { }
            public void LogWarningEvent(BuildWarningEventArgs e) { }
            public void LogMessageEvent(BuildMessageEventArgs e) { }
            public void LogCustomEvent(CustomBuildEventArgs e) { }

            public bool BuildProjectFile(
                string projectFileName,
                string[] targetNames,
                IDictionary globalProperties,
                IDictionary targetOutputs) => true;
        }

        private static readonly string SampleXml =
            Path.Combine(
                AppContext.BaseDirectory,
                "Xml2Doc.Sample.xml");

        private static string RepositoryRoot =>
            Path.GetFullPath(
                AppContext.BaseDirectory +
                ".." + Path.DirectorySeparatorChar +
                ".." + Path.DirectorySeparatorChar +
                ".." + Path.DirectorySeparatorChar +
                ".." + Path.DirectorySeparatorChar +
                "..");

        private static RendererOptions DefaultOptions() => new(
            FileNameMode: FileNameMode.CleanGenerics,
            RootNamespaceToTrim: "Xml2Doc.Sample",
            CodeBlockLanguage: "csharp",
            TrimRootNamespaceInFileNames: true
        );


        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PerType_DryRun_ReportsPlannedFilesWithoutWriting(bool generateIndex)
        {
            var outDir = Path.Combine(
                Path.GetTempPath(),
                "Xml2Doc.Tests",
                Path.GetRandomFileName());

            var task = new GenerateMarkdownFromXmlDoc
            {
                BuildEngine = new TestBuildEngine(),
                XmlPath = SampleXml,
                OutputDirectory = outDir,
                SingleFile = false,
                DryRun = true,
                GenerateIndex = generateIndex,
                FileNameMode = "clean",
                RootNamespaceToTrim = "Xml2Doc.Sample",
                TrimRootNamespaceInFileNames = true
            };

            task.Execute().ShouldBeTrue();
            task.DidWork.ShouldBeFalse();

            var model = Xml2Doc.Core.Models.Xml2Doc.Load(SampleXml);
            var renderer = new MarkdownRenderer(
                model,
                DefaultOptions() with { GenerateIndex = generateIndex });

            var expected = renderer.PlanOutputs(outDir)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            var actual = task.GeneratedFiles
                .Select(item => item.ItemSpec)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            actual.ShouldBe(expected);

            actual.Any(path =>
                    string.Equals(
                        Path.GetFileName(path),
                        "index.md",
                        StringComparison.OrdinalIgnoreCase))
                .ShouldBe(generateIndex);

            Directory.Exists(outDir).ShouldBeFalse(
                "Dry-run must not create the output directory or Markdown files.");
        }

        [Fact]
        public void Pruning_WhenManifestIdentityIsMissing_FailsBeforeWriting()
        {
            var outDir = CreateOutputDirectory();
            var task = CreateTask(outDir);
            task.PruneStaleFiles = true;

            task.Execute().ShouldBeFalse();

            task.DidWork.ShouldBeFalse();
            Directory.Exists(outDir).ShouldBeFalse();
        }

        [Fact]
        public void Pruning_WhenSingleFileIsEnabled_FailsBeforeWriting()
        {
            var outDir = CreateOutputDirectory();
            var task = CreateTask(outDir);
            task.SingleFile = true;
            task.OutputFile = outDir + Path.DirectorySeparatorChar + "api.md";
            task.PruneStaleFiles = true;
            task.ManifestIdentity = "sample-project";

            task.Execute().ShouldBeFalse();

            task.DidWork.ShouldBeFalse();
            Directory.Exists(outDir).ShouldBeFalse();
        }

        [Fact]
        public void PerType_WithPruning_DeletesOwnedStaleFileAndPreservesUntrackedFile()
        {
            var outDir = CreateOutputDirectory();

            try
            {
                var location = OutputManifestLocation.Create(
                    outDir,
                    "sample-project");
                Directory.CreateDirectory(outDir);
                File.WriteAllText(
                    outDir + Path.DirectorySeparatorChar + "stale.md",
                    "stale");
                File.WriteAllText(
                    outDir + Path.DirectorySeparatorChar + "hand-authored.md",
                    "keep");
                OutputManifestStore.Save(location, new[] { "stale.md" });
                var task = CreateTask(outDir);
                task.PruneStaleFiles = true;
                task.ManifestIdentity = "sample-project";

                task.Execute().ShouldBeTrue();

                task.DidWork.ShouldBeTrue();
                File.Exists(
                    outDir + Path.DirectorySeparatorChar + "stale.md")
                    .ShouldBeFalse();
                File.Exists(
                    outDir + Path.DirectorySeparatorChar + "hand-authored.md")
                    .ShouldBeTrue();
                OutputManifestStore.Load(location)!.Files.ShouldNotBeEmpty();
            }
            finally
            {
                if (Directory.Exists(outDir))
                {
                    Directory.Delete(outDir, recursive: true);
                }
            }
        }

        [Fact]
        public void PerType_DryRunWithPruning_DoesNotChangeFilesOrManifest()
        {
            var outDir = CreateOutputDirectory();

            try
            {
                var location = OutputManifestLocation.Create(
                    outDir,
                    "sample-project");
                Directory.CreateDirectory(outDir);
                var stalePath =
                    outDir + Path.DirectorySeparatorChar + "stale.md";
                File.WriteAllText(stalePath, "stale");
                OutputManifestStore.Save(location, new[] { "stale.md" });
                var previousManifest = File.ReadAllBytes(location.ManifestPath);
                var task = CreateTask(outDir);
                task.DryRun = true;
                task.PruneStaleFiles = true;
                task.ManifestIdentity = "sample-project";

                task.Execute().ShouldBeTrue();

                task.DidWork.ShouldBeFalse();
                File.Exists(stalePath).ShouldBeTrue();
                File.ReadAllBytes(location.ManifestPath)
                    .ShouldBe(previousManifest);
            }
            finally
            {
                if (Directory.Exists(outDir))
                {
                    Directory.Delete(outDir, recursive: true);
                }
            }
        }

        [Fact]
        public void BuildAssets_ExposeAndWirePruningProperties()
        {
            var buildDirectory =
                RepositoryRoot + Path.DirectorySeparatorChar +
                "src" + Path.DirectorySeparatorChar +
                "Xml2Doc.MSBuild" + Path.DirectorySeparatorChar +
                "build";
            var props = XDocument.Load(
                buildDirectory + Path.DirectorySeparatorChar +
                "Xml2Doc.MSBuild.props");
            var targets = XDocument.Load(
                buildDirectory + Path.DirectorySeparatorChar +
                "Xml2Doc.MSBuild.targets");

            props.Descendants("Xml2Doc_PruneStaleFiles")
                .Single().Value.ShouldBe("false");
            props.Descendants("Xml2Doc_ManifestIdentity")
                .Single().Value.ShouldBeEmpty();

            var taskElement = targets
                .Descendants("GenerateMarkdownFromXmlDoc")
                .Single();
            taskElement.Attribute("PruneStaleFiles")!.Value
                .ShouldBe("$(Xml2Doc_PruneStaleFiles)");
            taskElement.Attribute("ManifestIdentity")!.Value
                .ShouldBe("$(Xml2Doc_ManifestIdentity)");
        }

        private static GenerateMarkdownFromXmlDoc CreateTask(string outDir) =>
            new GenerateMarkdownFromXmlDoc
            {
                BuildEngine = new TestBuildEngine(),
                XmlPath = SampleXml,
                OutputDirectory = outDir,
                FileNameMode = "clean",
                RootNamespaceToTrim = "Xml2Doc.Sample",
                TrimRootNamespaceInFileNames = true
            };

        private static string CreateOutputDirectory() =>
            Path.GetTempPath() +
            "Xml2Doc.Tests" + Path.DirectorySeparatorChar +
            Path.GetRandomFileName();

    }
}
