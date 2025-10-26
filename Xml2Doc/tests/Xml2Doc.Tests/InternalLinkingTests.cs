using Shouldly;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xml2Doc.Core;
using Xunit;

public class InternalLinkingTests
{
    [Fact]
    public async Task PerType_Links_ResolveToFileAndAnchors()
    {
        // XML: one type with a method and summary <see> links to the type and method
        var xml = """
                <?xml version="1.0"?>
                <doc>
                  <assembly><name>Temp</name></assembly>
                  <members>
                    <member name="T:Temp.Foo">
                      <summary>
                        See <see cref="T:Temp.Foo" /> and <see cref="M:Temp.Foo.Do(System.String)" /> for details.
                      </summary>
                    </member>
                    <member name="M:Temp.Foo.Do(System.String)">
                      <summary>Does work.</summary>
                      <param name="s">input</param>
                    </member>
                  </members>
                </doc>
                """;

        var tmp = Path.Combine(Path.GetTempPath(), "Xml2Doc.Tests", Path.GetRandomFileName());
        Directory.CreateDirectory(tmp);
        var xmlPath = Path.Combine(tmp, "temp.xml");
        await File.WriteAllTextAsync(xmlPath, xml, new UTF8Encoding(false));

        var model = Xml2Doc.Core.Models.Xml2Doc.Load(xmlPath);
        var renderer = new MarkdownRenderer(model, new RendererOptions(
            FileNameMode: FileNameMode.CleanGenerics,
            RootNamespaceToTrim: null,
            CodeBlockLanguage: "csharp"
        ));

        var outDir = Path.Combine(tmp, "out");
        Directory.CreateDirectory(outDir);
        renderer.RenderToDirectory(outDir);

        var fooPath = Path.Combine(outDir, "Temp.Foo.md");
        File.Exists(fooPath).ShouldBeTrue($"Missing generated page: {fooPath}");

        var md = await File.ReadAllTextAsync(fooPath);
        md = md.Replace("\r\n", "\n");

        // Type link should point to the per-type file
        md.ShouldContain("[Foo](Temp.Foo.md)");

        // Method link should point to per-type file + correct anchor
        md.ShouldContain("[Do(string)](Temp.Foo.md#temp.foo.do(string))");

        // Referenced anchor must exist in the same file
        md.ShouldContain("<a id=\"temp.foo.do(string)\"></a>");
    }

    [Fact]
    public async Task SingleFile_Links_ResolveToInDocumentAnchors()
    {
        var xml = """
                <?xml version="1.0"?>
                <doc>
                  <assembly><name>Temp</name></assembly>
                  <members>
                    <member name="T:Temp.Foo">
                      <summary>
                        See <see cref="T:Temp.Foo" /> and <see cref="M:Temp.Foo.Do(System.String)" /> for details.
                      </summary>
                    </member>
                    <member name="M:Temp.Foo.Do(System.String)">
                      <summary>Does work.</summary>
                      <param name="s">input</param>
                    </member>
                  </members>
                </doc>
                """;

        var tmp = Path.Combine(Path.GetTempPath(), "Xml2Doc.Tests", Path.GetRandomFileName());
        Directory.CreateDirectory(tmp);
        var xmlPath = Path.Combine(tmp, "temp.xml");
        await File.WriteAllTextAsync(xmlPath, xml, new UTF8Encoding(false));

        var model = Xml2Doc.Core.Models.Xml2Doc.Load(xmlPath);
        var renderer = new MarkdownRenderer(model, new RendererOptions(
            FileNameMode: FileNameMode.CleanGenerics,
            RootNamespaceToTrim: null,
            CodeBlockLanguage: "csharp"
        ));

        var outFile = Path.Combine(tmp, "api.md");
        renderer.RenderToSingleFile(outFile);

        var md = await File.ReadAllTextAsync(outFile);
        md = md.Replace("\r\n", "\n");

        // Type link should point to type heading slug (#foo)
        md.ShouldContain("[Foo](#foo)");

        // Method link should point to in-document anchor
        md.ShouldContain("[Do(string)](#temp.foo.do(string))");

        // Member and type anchors must exist in the document
        md.ShouldContain("<a id=\"temp.foo.do(string)\"></a>");
        md.ShouldContain("<a id=\"foo\"></a>");
    }

    [Fact]
    public async Task NestedGeneric_MemberLinkTarget_MatchesAnchor()
    {
        // Method with nested generic parameter type; ensure link href == emitted anchor
        var xml = """
                <?xml version="1.0"?>
                <doc>
                  <assembly><name>Temp</name></assembly>
                  <members>
                    <member name="T:Temp.Nested">
                      <summary>
                        Calls <see cref="M:Temp.Nested.Transform``1(System.Collections.Generic.List{System.Collections.Generic.Dictionary{System.String,System.Int32}})" />.
                      </summary>
                    </member>
                    <member name="M:Temp.Nested.Transform``1(System.Collections.Generic.List{System.Collections.Generic.Dictionary{System.String,System.Int32}})">
                      <summary>Transforms nested structures.</summary>
                    </member>
                  </members>
                </doc>
                """;

        var tmp = Path.Combine(Path.GetTempPath(), "Xml2Doc.Tests", Path.GetRandomFileName());
        Directory.CreateDirectory(tmp);
        var xmlPath = Path.Combine(tmp, "temp.xml");
        await File.WriteAllTextAsync(xmlPath, xml, new UTF8Encoding(false));

        var model = Xml2Doc.Core.Models.Xml2Doc.Load(xmlPath);
        var renderer = new MarkdownRenderer(model, new RendererOptions(
            FileNameMode: FileNameMode.CleanGenerics,
            RootNamespaceToTrim: null,
            CodeBlockLanguage: "csharp"
        ));

        var outDir = Path.Combine(tmp, "out");
        Directory.CreateDirectory(outDir);
        renderer.RenderToDirectory(outDir);

        var page = Path.Combine(outDir, "Temp.Nested.md");
        File.Exists(page).ShouldBeTrue();

        var md = await File.ReadAllTextAsync(page);
        md = md.Replace("\r\n", "\n");

        // Capture the full anchor (including the closing ')') from the link href
        var linkMatch = Regex.Match(md,
            @"\[Transform<\s*T1\s*>\(.+?\)\]\(Temp\.Nested\.md#([^)]+\))\)",
            RegexOptions.Singleline);
        linkMatch.Success.ShouldBeTrue("Expected a link to Transform<T1>(...) with a member anchor.");

        var hrefAnchor = linkMatch.Groups[1].Value; // includes trailing ')'
        hrefAnchor.ShouldNotBeNullOrWhiteSpace();

        // The exact anchor must exist in the file
        md.ShouldContain($"<a id=\"{hrefAnchor}\"></a>", customMessage: "Link target anchor not found in the page.");
    }
}