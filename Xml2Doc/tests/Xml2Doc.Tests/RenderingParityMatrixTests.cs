using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xml2Doc.Core;
using Xml2Doc.Core.Anchoring;
using Xunit;

namespace Xml2Doc.Tests
{
    public class RenderingParityMatrixTests
    {
        public static IEnumerable<object[]> LinkRoutingMatrix()
        {
            foreach (var algorithm in Enum.GetValues<AnchorAlgorithm>())
            {
                yield return new object[] { algorithm, false };
                yield return new object[] { algorithm, true };
            }
        }

        [Theory]
        [InlineData(AnchorAlgorithm.Default, "crme-brle-v20")]
        [InlineData(AnchorAlgorithm.Github, "creme-brulee-v2-0")]
        [InlineData(AnchorAlgorithm.Kramdown, "creme-brulee_-v2-0")]
        [InlineData(AnchorAlgorithm.Gfm, "crme-brle_-v2.0")]
        public void BuiltInAnchorGenerators_PreserveExistingHeadingSlugs(
            AnchorAlgorithm algorithm,
            string expected)
        {
            var generator = new DefaultAnchorGenerator(algorithm);

            generator.GenerateHeadingAnchor("Crème brûlée_ v2.0!")
                .ShouldBe(expected);
            generator.GenerateMemberAnchor("Temp.Widget.Run(System.Int32)")
                .ShouldBe("temp.widget.run(int)");
        }

        [Theory]
        [MemberData(nameof(LinkRoutingMatrix))]
        public async Task InternalLinks_ResolveForEveryModeAndAnchorAlgorithm(
            AnchorAlgorithm algorithm,
            bool singleFile)
        {
            var tempTestRoot = ChildPath(Path.GetTempPath(), "Xml2Doc.Tests");
            var root = ChildPath(
                tempTestRoot,
                Path.GetFileName(Path.GetRandomFileName()));

            try
            {
                Directory.CreateDirectory(root);
                var xmlPath = ChildPath(root, "matrix.xml");
                await File.WriteAllTextAsync(xmlPath, FixtureXml, new UTF8Encoding(false));
                var model = Xml2Doc.Core.Models.Xml2Doc.Load(xmlPath);
                var renderer = new MarkdownRenderer(
                    model,
                    new RendererOptions(
                        FileNameMode: FileNameMode.CleanGenerics,
                        RootNamespaceToTrim: null,
                        CodeBlockLanguage: "csharp",
                        AnchorAlgorithm: algorithm));

                if (singleFile)
                {
                    var output = ChildPath(root, "api.md");
                    renderer.RenderToSingleFile(output);
                    var markdown = await ReadRequiredMarkdownAsync(output);

                    var typeHref = ExtractHref(markdown, "HTTP_Parser_v2");
                    typeHref.ShouldStartWith("#");
                    AssertAnchorExists(markdown, typeHref.Substring(1));

                    var memberHref = ExtractHref(markdown, "Do(string)");
                    memberHref.ShouldStartWith("#");
                    AssertAnchorExists(markdown, memberHref.Substring(1));
                }
                else
                {
                    var output = ChildPath(root, "docs");
                    renderer.RenderToDirectory(output);
                    var consumer = await ReadRequiredMarkdownAsync(
                        ChildPath(output, "Temp.Consumer.md"));
                    var targetPath = ChildPath(output, "Temp.HTTP_Parser_v2.md");
                    var target = await ReadRequiredMarkdownAsync(targetPath);

                    ExtractHref(consumer, "HTTP_Parser_v2")
                        .ShouldBe("Temp.HTTP_Parser_v2.md");

                    var memberHref = ExtractHref(consumer, "Do(string)");
                    memberHref.ShouldStartWith("Temp.HTTP_Parser_v2.md#");
                    AssertAnchorExists(
                        target,
                        memberHref.Substring(memberHref.IndexOf('#') + 1));
                }
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }
            }
        }

        [Fact]
        public async Task CustomAnchorGenerator_IsUsedForEmittedAnchorsAndLinks()
        {
            var root = ChildPath(
                ChildPath(Path.GetTempPath(), "Xml2Doc.Tests"),
                Path.GetFileName(Path.GetRandomFileName()));

            try
            {
                Directory.CreateDirectory(root);
                var xmlPath = ChildPath(root, "custom-anchors.xml");
                await File.WriteAllTextAsync(xmlPath, FixtureXml, new UTF8Encoding(false));
                var model = Xml2Doc.Core.Models.Xml2Doc.Load(xmlPath);
                var renderer = new MarkdownRenderer(
                    model,
                    new RendererOptions(
                        AnchorGenerator: new PrefixAnchorGenerator()));
                var output = ChildPath(root, "api.md");

                renderer.RenderToSingleFile(output);
                var markdown = await ReadRequiredMarkdownAsync(output);

                var typeHref = ExtractHref(markdown, "HTTP_Parser_v2");
                typeHref.ShouldBe("#heading-http_parser_v2");
                AssertAnchorExists(markdown, typeHref.Substring(1));

                var memberHref = ExtractHref(markdown, "Do(string)");
                memberHref.ShouldBe("#member-temp.http_parser_v2.do(system.string)");
                AssertAnchorExists(markdown, memberHref.Substring(1));
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }
            }
        }

        private static string ExtractHref(string markdown, string label)
        {
            var marker = $"[{label}](";
            var start = markdown.IndexOf(marker, StringComparison.Ordinal);
            start.ShouldBeGreaterThanOrEqualTo(0, $"Missing link label '{label}'.");
            start += marker.Length;
            var depth = 1;
            var end = start;

            while (end < markdown.Length && depth > 0)
            {
                if (markdown[end] == '(')
                {
                    depth++;
                }
                else if (markdown[end] == ')')
                {
                    depth--;
                }

                end++;
            }

            end--;
            end.ShouldBeGreaterThan(start, $"Missing link destination for '{label}'.");
            return markdown.Substring(start, end - start);
        }

        private static void AssertAnchorExists(string markdown, string anchor) =>
            markdown.ShouldContain($"<a id=\"{anchor}\"></a>");

        private sealed class PrefixAnchorGenerator : IAnchorGenerator
        {
            public string GenerateHeadingAnchor(string heading) =>
                "heading-" + heading.ToLowerInvariant();

            public string GenerateMemberAnchor(string memberId) =>
                "member-" + memberId.ToLowerInvariant();
        }

        private static async Task<string> ReadRequiredMarkdownAsync(string path)
        {
            File.Exists(path).ShouldBeTrue($"Missing expected Markdown output: {path}");
            return Normalize(await File.ReadAllTextAsync(path));
        }

        private static string ChildPath(string root, string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
            {
                throw new ArgumentException(
                    "The child path must be relative.",
                    nameof(relativePath));
            }

            var canonicalRoot = Path.GetFullPath(root);
            var candidate = Path.GetFullPath(Path.Join(canonicalRoot, relativePath));
            var rootWithSeparator =
                Path.TrimEndingDirectorySeparator(canonicalRoot) +
                Path.DirectorySeparatorChar;
            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            if (!candidate.StartsWith(rootWithSeparator, comparison))
            {
                throw new ArgumentException(
                    "The child path must remain under the requested root.",
                    nameof(relativePath));
            }

            return candidate;
        }

        private static string Normalize(string markdown) =>
            markdown.Replace("\r\n", "\n");

        private const string FixtureXml = """
            <?xml version="1.0"?>
            <doc>
              <assembly><name>Temp</name></assembly>
              <members>
                <member name="T:Temp.Consumer">
                  <summary>
                    See <see cref="T:Temp.HTTP_Parser_v2"/> and
                    <see cref="M:Temp.HTTP_Parser_v2.Do(System.String)"/>.
                  </summary>
                </member>
                <member name="T:Temp.HTTP_Parser_v2">
                  <summary>Parses HTTP values.</summary>
                </member>
                <member name="M:Temp.HTTP_Parser_v2.Do(System.String)">
                  <summary>Does work.</summary>
                  <param name="value">Input value.</param>
                </member>
              </members>
            </doc>
            """;
    }
}
