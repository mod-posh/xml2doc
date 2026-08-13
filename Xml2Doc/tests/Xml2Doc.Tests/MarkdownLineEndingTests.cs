using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xml2Doc.Core;
using Xunit;

namespace Xml2Doc.Tests
{
    public class MarkdownLineEndingTests
    {
        private static string TestProjectDirectory =>
            Path.GetFullPath(
                AppContext.BaseDirectory +
                ".." + Path.DirectorySeparatorChar +
                ".." + Path.DirectorySeparatorChar +
                "..");

        private static string SampleXml =>
            TestProjectDirectory + Path.DirectorySeparatorChar +
            "Assets" + Path.DirectorySeparatorChar +
            "Xml2Doc.Sample.xml";

        [Fact]
        public void RenderToString_DefaultsToLfIndependentlyOfHostNewline()
        {
            var renderer = CreateRenderer(new RendererOptions());

            var markdown = renderer.RenderToString();

            markdown.ShouldContain("\n");
            markdown.ShouldNotContain("\r");
        }

        [Fact]
        public void RenderToString_WhenCrLfSelected_UsesOnlyCrLf()
        {
            var renderer = CreateRenderer(
                new RendererOptions(LineEndings: LineEndingStyle.CrLf));

            var markdown = renderer.RenderToString();

            AssertUsesOnly(markdown, "\r\n");
        }

        [Fact]
        public void RenderToString_WhenNativeSelected_UsesOnlyHostNewline()
        {
            var renderer = CreateRenderer(
                new RendererOptions(LineEndings: LineEndingStyle.Native));

            var markdown = renderer.RenderToString();

            AssertUsesOnly(markdown, Environment.NewLine);
        }

        [Fact]
        public void RenderToDirectory_NormalizesEveryMarkdownOutputToLf()
        {
            var outputDirectory =
                Path.GetTempPath() +
                "Xml2Doc.Tests" + Path.DirectorySeparatorChar +
                Guid.NewGuid().ToString("N");

            try
            {
                var renderer = CreateRenderer(
                    new RendererOptions(EmitNamespaceIndex: true));

                renderer.RenderToDirectory(outputDirectory);

                var markdownFiles = Directory
                    .GetFiles(
                        outputDirectory,
                        "*.md",
                        SearchOption.AllDirectories)
                    .ToArray();
                markdownFiles.ShouldNotBeEmpty();

                foreach (var markdownFile in markdownFiles)
                {
                    var content = File.ReadAllText(markdownFile);
                    content.Contains('\r').ShouldBeFalse(
                        $"Expected LF-only output in {markdownFile}.");
                }
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                {
                    Directory.Delete(outputDirectory, recursive: true);
                }
            }
        }

        [Fact]
        public void RenderToSingleFile_UsesConfiguredCrLfBytes()
        {
            var outputDirectory =
                Path.GetTempPath() +
                "Xml2Doc.Tests" + Path.DirectorySeparatorChar +
                Guid.NewGuid().ToString("N");
            var outputFile =
                outputDirectory + Path.DirectorySeparatorChar + "api.md";

            try
            {
                var renderer = CreateRenderer(
                    new RendererOptions(LineEndings: LineEndingStyle.CrLf));

                renderer.RenderToSingleFile(outputFile);

                var bytes = File.ReadAllBytes(outputFile);
                bytes.ShouldContain((byte)'\n');
                (bytes.Length >= 3 &&
                 bytes[0] == 0xEF &&
                 bytes[1] == 0xBB &&
                 bytes[2] == 0xBF).ShouldBeFalse(
                    "Markdown must be UTF-8 without a byte-order mark.");

                for (var index = 0; index < bytes.Length; index++)
                {
                    if (bytes[index] == (byte)'\n')
                    {
                        index.ShouldBeGreaterThan(0);
                        bytes[index - 1].ShouldBe((byte)'\r');
                    }

                    if (bytes[index] == (byte)'\r')
                    {
                        index.ShouldBeLessThan(bytes.Length - 1);
                        bytes[index + 1].ShouldBe((byte)'\n');
                    }
                }
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                {
                    Directory.Delete(outputDirectory, recursive: true);
                }
            }
        }

        private static MarkdownRenderer CreateRenderer(
            RendererOptions options)
        {
            File.Exists(SampleXml).ShouldBeTrue(
                $"Missing fixture XML at: {SampleXml}");
            var model = Xml2Doc.Core.Models.Xml2Doc.Load(SampleXml);
            return new MarkdownRenderer(model, options);
        }

        private static void AssertUsesOnly(
            string content,
            string expectedLineEnding)
        {
            content.ShouldContain(expectedLineEnding);
            var withoutExpected =
                content.Replace(expectedLineEnding, string.Empty);
            withoutExpected.ShouldNotContain("\r");
            withoutExpected.ShouldNotContain("\n");
        }
    }
}
