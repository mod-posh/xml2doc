using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xml2Doc.Core;
using Xunit;

public class RenderSnapshots
{
    // BaseDirectory: .../tests/Xml2Doc.Tests/bin/<cfg>/<tfm>/
    // Go up to .../tests/Xml2Doc.Tests/
    private static string TestProjectDir =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

    private static string AssetsDir => Path.Combine(TestProjectDir, "Assets");
    private static string SampleXml => Path.Combine(AssetsDir, "Xml2Doc.Sample.xml");

    private static string SnapRoot => Path.Combine(TestProjectDir, "__snapshots__");

    private static Xml2Doc.Core.Models.Xml2Doc LoadFixtureModel()
    {
        File.Exists(SampleXml).ShouldBeTrue(
            $"Missing fixture XML at: {SampleXml}\n" +
            $"Run scripts/update-fixtures.ps1 to generate it and commit the result."
        );

        return Xml2Doc.Core.Models.Xml2Doc.Load(SampleXml);
    }

    private static RendererOptions DefaultOptions() => new RendererOptions(
        FileNameMode: FileNameMode.CleanGenerics,
        RootNamespaceToTrim: "Xml2Doc.Sample",
        CodeBlockLanguage: "csharp"
    );

    [Fact]
    public void SingleFile_CleanNames_Basic()
    {
        var model = LoadFixtureModel();
        var renderer = new MarkdownRenderer(model, DefaultOptions());

        var md = renderer.RenderToString();
        md.ShouldNotBeNullOrWhiteSpace("Rendered markdown is empty.");

        md = md.Replace("\r\n", "\n");

        // Extract a section by heading text in a single-file document.
        // The renderer emits:
        //   <a id="..."></a>
        //   # Heading
        static string ExtractH1Section(string content, string headingText)
        {
            content = content.Replace("\r\n", "\n");

            // Find "# {headingText}" line
            var rx = new Regex($@"(?m)^\#\s+{Regex.Escape(headingText)}\s*$");
            var m = rx.Match(content);
            if (!m.Success)
            {
                throw new Shouldly.ShouldAssertException(
                    $"Could not find heading '# {headingText}'.\nFirst 300 chars:\n" +
                    content.Substring(0, Math.Min(300, content.Length))
                );
            }

            var start = m.Index;
            // next H1
            var next = new Regex(@"(?m)^\#\s+\S.*$").Match(content, start + m.Length);
            var end = next.Success ? next.Index : content.Length;

            return content.Substring(start, end - start);
        }

        var mathx = ExtractH1Section(md, "Mathx");

        static string Norm(string s)
        {
            s = s.Replace("\r\n", "\n");
            s = s.Replace("`", "");
            s = Regex.Replace(s, @"[ \t]+", " ");
            s = Regex.Replace(s, @"\n{3,}", "\n\n");
            return s.Trim();
        }

        var norm = Norm(mathx);

        norm.ShouldContain("# Mathx");

        // Tolerant signature checks (param names optional)
        var ident = @"(?:\s+[A-Za-z_][A-Za-z0-9_]*)?";
        Regex.IsMatch(norm, $@"(?i)\bAdd\s*\(\s*int{ident}\s*,\s*int{ident}\s*\)")
            .ShouldBeTrue("Expected Add(int, int) signature (param names optional) somewhere in Mathx.");
        Regex.IsMatch(norm, $@"(?i)\bAdd\s*\(\s*int{ident}\s*,\s*int{ident}\s*,\s*int{ident}\s*\)")
            .ShouldBeTrue("Expected Add(int, int, int) signature (param names optional) somewhere in Mathx.");

        norm.ShouldContain("Add two integers.");
        norm.ShouldContain("Add three integers.");
        norm.ShouldContain("Parameters");
        norm.ShouldContain("Returns");
        norm.ShouldContain("var s = Mathx.Add(1,2); // 3");

        norm.ShouldContain("Alias that calls [Add(int, int)]");

        // Guardrails
        norm.ShouldNotContain("Int32)");
        norm.ShouldNotContain("})");

        // Anchor stability (member anchors are part of the output contract)
        md.ShouldContain("<a id=\"xml2doc.sample.mathx.add(int,int)\"></a>");
        md.ShouldContain("<a id=\"xml2doc.sample.mathx.add(int,int,int)\"></a>");
    }

    [Fact]
    public async Task PerType_CleanNames_Basic()
    {
        var model = LoadFixtureModel();
        var renderer = new MarkdownRenderer(model, DefaultOptions());

        var outDir = Path.Combine(Path.GetTempPath(), "Xml2Doc.Tests", Path.GetRandomFileName());
        Directory.CreateDirectory(outDir);
        renderer.RenderToDirectory(outDir);

        var snapDir = Path.Combine(SnapRoot, "PerType_CleanNames");
        Directory.Exists(snapDir).ShouldBeTrue(
            $"Missing snapshot directory: {snapDir}\n" +
            $"Run scripts/seed-snapshots.ps1 and commit the results."
        );

        var actualFiles = Directory.GetFiles(outDir, "*.md", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray()!;

        var expectedFiles = Directory.GetFiles(snapDir, "*.verified.md", SearchOption.TopDirectoryOnly)
            .Select(f =>
            {
                var name = Path.GetFileName(f);
                return name is null ? null : name.Replace(".verified.md", ".md", StringComparison.Ordinal);
            })
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray()!;

        actualFiles.ShouldBe(expectedFiles, customMessage: "Per-type output file set differs from snapshots.");

        foreach (var file in actualFiles)
        {
            var actualPath = Path.Combine(outDir, file!);
            var expectedPath = Path.Combine(snapDir, file!.Replace(".md", ".verified.md", StringComparison.Ordinal));

            File.Exists(expectedPath).ShouldBeTrue($"Missing snapshot: {expectedPath}");

            var actual = await File.ReadAllTextAsync(actualPath);
            var expected = await File.ReadAllTextAsync(expectedPath);

            Normalize(actual).ShouldBe(Normalize(expected), customMessage: $"Mismatch in {file}");
        }
    }

    [Fact]
    public async Task Generic_BraceHandling_IsClean()
    {
        var model = LoadFixtureModel();
        var renderer = new MarkdownRenderer(model, DefaultOptions());

        var outDir = Path.Combine(Path.GetTempPath(), "Xml2Doc.Tests", Path.GetRandomFileName());
        Directory.CreateDirectory(outDir);
        renderer.RenderToDirectory(outDir);

        var mdPath = Path.Combine(outDir, "Xml2Doc.Sample.GenericPlayground.md");
        File.Exists(mdPath).ShouldBeTrue($"Missing generated page: {mdPath}");

        var md = await File.ReadAllTextAsync(mdPath);

        md = md.Replace("\r\n", "\n");
        md = md.Replace("`", "");
        md = Regex.Replace(md, @"\s+", " ");

        string xitem = @"(?:Xml2Doc\.Sample\.)?XItem";
        string ident = @"(?:\s+[A-Za-z_][A-Za-z0-9_]*)?";

        var flattenPattern =
            $@"(?i)Method:\s*Flatten\s*\(\s*IEnumerable<\s*IEnumerable<\s*{xitem}\s*>\s*>\s*{ident}\s*\)";

        var indexPattern =
            $@"(?i)Method:\s*Index\s*\(\s*Dictionary\s*<\s*string\s*,\s*List<\s*{xitem}\s*>\s*>\s*{ident}\s*\)";

        var transformPattern =
            $@"(?i)Method:\s*Transform<\s*T1\s*,\s*T2\s*>\s*\(\s*List<\s*Dictionary<\s*T1\s*,\s*List<\s*T2\s*>\s*>\s*>\s*{ident}\s*\)";

        Regex.IsMatch(md, flattenPattern).ShouldBeTrue("Expected Flatten(IEnumerable<IEnumerable<XItem>>) signature.");
        Regex.IsMatch(md, indexPattern).ShouldBeTrue("Expected Index(Dictionary<string, List<XItem>>) signature.");
        Regex.IsMatch(md, transformPattern).ShouldBeTrue("Expected Transform<T1,T2>(List<Dictionary<T1, List<T2>>>) signature.");

        md.ShouldNotContain("})");
        md.ShouldNotContain("Int32)");
    }

    private static string Normalize(string s) =>
        s.Replace("\r\n", "\n").Trim();
}