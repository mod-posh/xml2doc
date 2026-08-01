using Microsoft.Build.Framework;
using Shouldly;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Xml2Doc.Core;
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

    }
}
