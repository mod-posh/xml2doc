using Shouldly;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xml2Doc.Core;
using Xunit;

public class NormalizationTests
{
    [Fact]
    public async Task Normalize_PreservesParagraphs_TrimsIntraLine_ProtectsCodeBlocks()
    {
        // Build a synthetic XML doc file
        var xml = """
                <?xml version="1.0"?>
                <doc>
                  <assembly><name>Temp</name></assembly>
                  <members>
                    <member name="T:Temp.Norm">
                      <summary>
                        First paragraph with  multiple   spaces and stray space before punctuation .
                        <para>
                          Second paragraph with   tabs	and spaces , links via <see href="https://example.com">Ex</see> and inline <c>foo  bar</c>.
                        </para>
                      </summary>
                      <remarks>
                        Alpha . <para>Beta , gamma ; delta : epsilon ) ]</para>
                      </remarks>
                    </member>

                    <member name="M:Temp.Norm.Demo(System.String)">
                      <summary>Contains inline <c>foo  bar</c> and a code sample.</summary>
                      <example>
                        <code>
                line1
                  indented  line2
                	tabbed	line3
                        </code>
                      </example>
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

        var mdPath = Path.Combine(outDir, "Temp.Norm.md");
        File.Exists(mdPath).ShouldBeTrue($"Missing generated page: {mdPath}");

        var md = await File.ReadAllTextAsync(mdPath);
        md = md.Replace("\r\n", "\n");

        // Sanity
        md.ShouldContain("# Norm");

        // Extract prose only (strip explicit anchors and fenced code to make prose assertions clean)
        var mdNoAnchors = Regex.Replace(md, "<a id=\"[^\"]+\"></a>\\s*", "", RegexOptions.IgnoreCase);
        var prose = Regex.Replace(mdNoAnchors, "(?s)```[^`]*```", ""); // remove fenced code blocks

        // 1) Paragraphs preserved: blank line between summary paragraphs
        prose.ShouldContain("First paragraph with multiple spaces and stray space before punctuation.\n\nSecond paragraph");

        // 2) Intra-line spaces collapsed in prose (no "  " sequences where not in code)
        Regex.IsMatch(prose, @"[^\n]  [^\n]").ShouldBeFalse("Found double spaces in prose lines.");

        // 3) Stray spaces before punctuation removed in prose
        prose.ShouldNotContain(" .");
        prose.ShouldNotContain(" ,");
        prose.ShouldNotContain(" ;");
        prose.ShouldNotContain(" :");
        prose.ShouldNotContain(" )");
        prose.ShouldNotContain(" ]");

        // 4) Inline code rendered as backticks (content may be space-collapsed by line-normalization; presence is enough)
        prose.ShouldContain("`foo bar`");

        // 5) Example rendered as fenced code with language and preserved formatting
        var fenceMatch = Regex.Match(md, "(?s)```csharp\\n(.*?)\\n```");
        fenceMatch.Success.ShouldBeTrue("Expected a fenced csharp code block from <example><code>.");

        var code = fenceMatch.Groups[1].Value;
        // Should preserve exact spacing/tabs inside the fenced code block
        code.ShouldContain("line1");
        code.ShouldContain("  indented  line2"); // two spaces before and between 'indented' and 'line2'
        code.ShouldContain("\ttabbed\tline3");   // tabs preserved
    }
}