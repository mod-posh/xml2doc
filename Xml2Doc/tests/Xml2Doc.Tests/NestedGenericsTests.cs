using Shouldly;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xml2Doc.Core;
using Xunit;

public class NestedGenericsTests
{
    [Fact]
    public async Task MemberHeader_DepthAwareParams_AreFormattedCorrectly()
    {
        // Synthetic XML with a deeply nested generic parameter list
        var xml = """
                <?xml version="1.0"?>
                <doc>
                  <assembly><name>Temp</name></assembly>
                  <members>
                    <member name="T:Temp.Nested"/>
                    <!-- Transform``2(List<Dictionary<``0, List<``1>>> arg) -->
                    <member name="M:Temp.Nested.Transform``2(System.Collections.Generic.List{System.Collections.Generic.Dictionary{``0,System.Collections.Generic.List{``1}}})">
                      <summary>Transforms nested structures.</summary>
                    </member>
                  </members>
                </doc>
                """;

        var tmpDir = Path.Combine(Path.GetTempPath(), "Xml2Doc.Tests", Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        var xmlPath = Path.Combine(tmpDir, "temp.xml");
        await File.WriteAllTextAsync(xmlPath, xml, new UTF8Encoding(false));

        var model = Xml2Doc.Core.Models.Xml2Doc.Load(xmlPath);
        var renderer = new MarkdownRenderer(model, new RendererOptions(
            FileNameMode: FileNameMode.CleanGenerics,
            RootNamespaceToTrim: null,
            CodeBlockLanguage: "csharp"
        ));

        var outDir = Path.Combine(tmpDir, "out");
        Directory.CreateDirectory(outDir);
        renderer.RenderToDirectory(outDir);

        var mdPath = Path.Combine(outDir, "Temp.Nested.md");
        File.Exists(mdPath).ShouldBeTrue($"Missing generated page: {mdPath}");

        var md = await File.ReadAllTextAsync(mdPath);
        md = md.Replace("\r\n", "\n");

        // Assert the method header uses depth-aware splitting and formatting:
        // Method: Transform<T1,T2>(List<Dictionary<T1, List<T2>>>)
        var pattern = @"(?im)^##\s*Method:\s*Transform<\s*T1\s*,\s*T2\s*>\s*\(\s*List<\s*Dictionary<\s*T1\s*,\s*List<\s*T2\s*>\s*>\s*>\s*\)";
        Regex.IsMatch(md, pattern).ShouldBeTrue(
            "Expected depth-aware formatted header for Transform<T1,T2>(List<Dictionary<T1, List<T2>>>)"
        );

        // Guardrails: ensure commas inside nested generics did not split at the wrong depth
        md.ShouldNotContain(">,,");
        md.ShouldNotContain("},,");
    }

    [Fact]
    public async Task ShortLabelFromCref_FormatsNestedGenericType_AndMethodLabels()
    {
        // Synthetic XML with <see> to nested generic TYPE and METHOD
        var xml = """
                <?xml version="1.0"?>
                <doc>
                  <assembly><name>Temp</name></assembly>
                  <members>
                    <member name="T:Temp.Refs">
                      <summary>
                        Consumes
                        <see cref="T:System.Collections.Generic.Dictionary{System.String,System.Collections.Generic.List{System.Collections.Generic.Dictionary{System.String,System.Int32}}}" />
                        and calls
                        <see cref="M:Temp.Refs.Transform``2(System.Collections.Generic.List{System.Collections.Generic.Dictionary{``0,System.Collections.Generic.List{``1}}})" />.
                      </summary>
                    </member>

                    <!-- Method referenced above -->
                    <member name="M:Temp.Refs.Transform``2(System.Collections.Generic.List{System.Collections.Generic.Dictionary{``0,System.Collections.Generic.List{``1}}})">
                      <summary>Transforms nested structures.</summary>
                    </member>
                  </members>
                </doc>
                """;

        var tmpDir = Path.Combine(Path.GetTempPath(), "Xml2Doc.Tests", Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        var xmlPath = Path.Combine(tmpDir, "temp.xml");
        await File.WriteAllTextAsync(xmlPath, xml, new UTF8Encoding(false));

        var model = Xml2Doc.Core.Models.Xml2Doc.Load(xmlPath);
        var renderer = new MarkdownRenderer(model, new RendererOptions(
            FileNameMode: FileNameMode.CleanGenerics,
            RootNamespaceToTrim: null,
            CodeBlockLanguage: "csharp"
        ));

        var outDir = Path.Combine(tmpDir, "out");
        Directory.CreateDirectory(outDir);
        renderer.RenderToDirectory(outDir);

        var mdPath = Path.Combine(outDir, "Temp.Refs.md");
        File.Exists(mdPath).ShouldBeTrue($"Missing generated page: {mdPath}");

        var md = await File.ReadAllTextAsync(mdPath);
        md = md.Replace("\r\n", "\n");

        // Strip anchors to avoid false positives on lowercase ids
        var mdNoAnchors = Regex.Replace(md, "<a id=\"[^\"]+\"></a>\\s*", "", RegexOptions.IgnoreCase);

        // 1) TYPE label should be trimmed/aliased: Dictionary<string, List<Dictionary<string, int>>>
        mdNoAnchors.ShouldContain("Dictionary<string, List<Dictionary<string, int>>>",
            customMessage: "Expected nested generic TYPE label with aliases and trimmed namespaces.");

        // 2) METHOD label should be depth-aware and aliased: Transform<T1,T2>(List<Dictionary<T1, List<T2>>>)
        mdNoAnchors.ShouldContain("Transform<T1,T2>(List<Dictionary<T1, List<T2>>>)",
            customMessage: "Expected nested generic METHOD label with depth-aware formatting.");
    }
}