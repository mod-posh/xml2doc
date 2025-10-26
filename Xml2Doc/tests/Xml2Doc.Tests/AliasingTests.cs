using Shouldly;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xml2Doc.Core;
using Xml2Doc.Sample;
using Xunit;

public class AliasingTests
{
    // Resolve project directory from the test's bin folder
    private static string ProjectDir =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

    [Fact]
    public async Task TokenAwareAliasing_DoesNotCorruptIdentifiers()
    {
        // Build sample XML
        var xml = Path.ChangeExtension(typeof(AliasingPlayground).Assembly.Location, ".xml");
        File.Exists(xml).ShouldBeTrue($"Missing XML: {xml}");

        var model = Xml2Doc.Core.Models.Xml2Doc.Load(xml);
        var options = new RendererOptions(
            FileNameMode: FileNameMode.CleanGenerics,
            RootNamespaceToTrim: "Xml2Doc.Sample",
            CodeBlockLanguage: "csharp"
        );
        var renderer = new MarkdownRenderer(model, options);

        // Render per-type and read the AliasingPlayground page
        var outDir = Path.Combine(Path.GetTempPath(), "Xml2Doc.Tests", Path.GetRandomFileName());
        Directory.CreateDirectory(outDir);
        renderer.RenderToDirectory(outDir);

        var mdPath = Path.Combine(outDir, "Xml2Doc.Sample.AliasingPlayground.md");
        File.Exists(mdPath).ShouldBeTrue($"Missing generated page: {mdPath}");

        var md = await File.ReadAllTextAsync(mdPath);
        md = md.Replace("\r\n", "\n");

        // Sanity: page header
        md.ShouldContain("# AliasingPlayground");

        // Remove explicit anchors from consideration to avoid false-positives on lowercased ids
        var mdNoAnchors = Regex.Replace(md, "<a id=\"[^\"]+\"></a>\\s*", "", RegexOptions.IgnoreCase);

        // 1) Ensure we did NOT corrupt identifiers containing "String" in visible text
        // Expect the method header to show the un-aliased BCL identifier: StringComparer
        Regex.IsMatch(mdNoAnchors, @"(?im)^##\s+Method:\s*UseComparer\s*\(\s*StringComparer\s*\)")
            .ShouldBeTrue("Expected visible header 'Method: UseComparer(StringComparer)'.");

        // Ensure the lowercase variant does NOT appear (case-sensitive check)
        mdNoAnchors.IndexOf("stringComparer", System.StringComparison.Ordinal).ShouldBe(-1);
        mdNoAnchors.IndexOf("System.stringComparer", System.StringComparison.Ordinal).ShouldBe(-1);

        // 2) Ensure true tokens were aliased as expected in the Mix signature
        // Expect "Method: Mix(string, int, uint)" somewhere (header or bullet)
        var pattern = @"(?im)Method:\s*Mix\s*\(\s*string\s*,\s*int\s*,\s*uint\s*\)";
        Regex.IsMatch(mdNoAnchors, pattern).ShouldBeTrue(
            "Expected Mix(string, int, uint) signature (header or bullet) with aliases applied."
        );
    }
}