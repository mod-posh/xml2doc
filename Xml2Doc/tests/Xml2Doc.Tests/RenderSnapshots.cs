using Shouldly;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xml2Doc.Core;
using Xml2Doc.Sample;
using Xunit;

public class RenderSnapshots
{
    // Resolve project directory from the test's bin folder
    private static string ProjectDir =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

    private static string SnapRoot => Path.Combine(ProjectDir, "__snapshots__");

    [Fact]
    public void SingleFile_CleanNames_Basic()
    {
        // Build sample XML
        var xml = Path.ChangeExtension(typeof(Xml2Doc.Sample.Mathx).Assembly.Location, ".xml");
        File.Exists(xml).ShouldBeTrue($"Missing XML: {xml}");

        var model = Xml2Doc.Core.Models.Xml2Doc.Load(xml);
        var options = new RendererOptions(
            FileNameMode: FileNameMode.CleanGenerics,   // keep in sync with your CLI seed
            RootNamespaceToTrim: "Xml2Doc.Sample",      // ditto
            CodeBlockLanguage: "csharp"
        );
        var renderer = new MarkdownRenderer(model, options);

        // Render to a string (instead of writing a temp file) so we can inspect output directly
        var md = renderer.RenderToString();

        // Write output to console to aid debugging/inspection during test runs
        Console.WriteLine(md);

        md.ShouldNotBeNullOrWhiteSpace("Rendered markdown is empty.");

        // Normalize line endings to avoid Windows/Linux variance
        md = md.Replace("\r\n", "\n");

        // Extract the section whose heading text equals `headingText`,
        // returning content until the next heading of the same or higher level.
        static string ExtractSection(string content, string headingText)
        {
            // Normalize newlines for predictable regex behavior
            content = content.Replace("\r\n", "\n");

            // Compute a slug similar to the renderer's HeadingSlug (lowercase, spaces -> '-', remove non [a-z0-9-])
            var slug = Regex.Replace(headingText.Trim().ToLowerInvariant(), @"\s+", "-");
            slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");

            // Try to find an HTML anchor for the section first (single-file output uses these)
            var anchor = $"<a id=\"{slug}\"></a>";
            var anchorIdx = content.IndexOf(anchor, StringComparison.Ordinal);
            var headingMatch = (anchorIdx >= 0)
                ? new Regex(@"(?im)^\s*#{1,6}\s+\S.*$").Match(content, anchorIdx + anchor.Length)
                : Regex.Match(content, $"(?im)^(\\s*#{1,6})\\s+{Regex.Escape(headingText)}\\s*$");

            if (!headingMatch.Success)
            {
                // Fallback: original behavior — include first 300 chars for debugging
                throw new Shouldly.ShouldAssertException(
                    $"Could not find heading for '{headingText}'.\nFirst 300 chars:\n{content.Substring(0, Math.Min(300, content.Length))}"
                );
            }

            // If we found a heading (either via anchor->next-heading or direct heading), determine its level
            var headLine = headingMatch.Value;
            var hashes = Regex.Match(headLine, @"^(\s*#+)").Groups[1].Value;
            var level = hashes.Count(c => c == '#');

            var start = headingMatch.Index; // include the heading line itself

            // Find the next heading of level <= current
            var nextPattern = new Regex(@"(?im)^\s*#{1,6}\s+\S.*$");
            var next = nextPattern.Match(content, start + headingMatch.Length);

            while (next.Success)
            {
                // Count level of the next heading
                var line = next.Value;
                var nextLevel = 0;
                foreach (var ch in line)
                {
                    if (ch == '#') nextLevel++;
                    else if (char.IsWhiteSpace(ch)) continue;
                    else break;
                }

                if (nextLevel <= level)
                    break; // stop at the first heading with level <= current

                // Otherwise keep looking
                next = nextPattern.Match(content, next.Index + next.Length);
            }

            var end = next.Success ? next.Index : content.Length;
            return content.Substring(start, end - start);
        }

        var mathx = ExtractSection(md, "Mathx");

        // Light normalization for comparisons
        static string Norm(string s)
        {
            s = s.Replace("`", ""); // don’t trip over backticks in bullets
            s = Regex.Replace(s, @"[ \t]+", " ");
            s = Regex.Replace(s, @"\n{3,}", "\n\n");
            return s.Trim();
        }

        var norm = Norm(mathx);

        // ——— Assertions: just the essentials ———
        norm.ShouldContain("# Mathx");

        // We still want to see both overloads referenced somewhere in the section text:
        var ident = @"(?:\s+[A-Za-z_][A-Za-z0-9_]*)?"; // optional param name
        Regex.IsMatch(norm, $@"(?i)\bAdd\s*\(\s*int{ident}\s*,\s*int{ident}\s*\)")
            .ShouldBeTrue("Expected Add(int, int) signature (param names optional) somewhere in Mathx.");
        Regex.IsMatch(norm, $@"(?i)\bAdd\s*\(\s*int{ident}\s*,\s*int{ident}\s*,\s*int{ident}\s*\)")
            .ShouldBeTrue("Expected Add(int, int, int) signature (param names optional) somewhere in Mathx.");

        // Text we absolutely expect (from your verified Mathx page)  :contentReference[oaicite:1]{index=1}
        norm.ShouldContain("Add two integers.");
        norm.ShouldContain("Add three integers.");
        norm.ShouldContain("Parameters");
        norm.ShouldContain("Returns");
        norm.ShouldContain("var s = Mathx.Add(1,2); // 3");

        // Alias method: just assert the label — path/anchor may vary between single-file/per-type
        norm.ShouldContain("Alias that calls [Add(int, int)]");

        // Guard against old formatting bugs
        norm.ShouldNotContain("Int32)");
        norm.ShouldNotContain("})");

        // Diagnostic: if any Int32 artifacts exist, print location + context for debugging
        {
            string FindContext(string haystack, string needle, int ctx = 60)
            {
                var idx = haystack.IndexOf(needle, StringComparison.Ordinal);
                if (idx < 0) return null!;
                var start = Math.Max(0, idx - ctx);
                var len = Math.Min(haystack.Length - start, needle.Length + ctx * 2);
                return haystack.Substring(start, len).Replace("\n", "\\n");
            }

            if (norm.Contains("Int32)"))
            {
                Console.WriteLine("DEBUG: Found literal 'Int32)' in normalized section.");
                Console.WriteLine(FindContext(norm, "Int32)"));
            }
            if (norm.IndexOf("int32)", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Console.WriteLine("DEBUG: Found case-insensitive 'int32)' in normalized section.");
                Console.WriteLine(FindContext(norm, "int32)"));
            }

            // Also check full md to see if anchors use Int32
            if (md.IndexOf("Int32)", StringComparison.Ordinal) >= 0 || md.IndexOf("int32)", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Console.WriteLine("DEBUG: Found 'Int32' in full markdown output. Show 300-char slice around first occurrence:");
                var first = md.IndexOf("Int32)", StringComparison.Ordinal);
                if (first < 0) first = md.IndexOf("int32)", StringComparison.OrdinalIgnoreCase);
                var start = Math.Max(0, first - 120);
                var length = Math.Min(300, md.Length - start);
                Console.WriteLine(md.Substring(start, length).Replace("\n", "\\n"));
            }
        }

        md.ShouldContain("<a id=\"xml2doc.sample.mathx.add(int,int)\"></a>");
        md.ShouldContain("<a id=\"xml2doc.sample.mathx.add(int,int,int)\"></a>");
    }

    [Fact]
    public async Task PerType_CleanNames_Basic()
    {
        var sampleAssemblyPath = typeof(Mathx).Assembly.Location;
        var xml = Path.ChangeExtension(sampleAssemblyPath, ".xml");
        File.Exists(xml).ShouldBeTrue($"Sample XML not found at: {xml}. Did you build Xml2Doc.Sample with GenerateDocumentationFile enabled?");

        var model = Xml2Doc.Core.Models.Xml2Doc.Load(xml);
        var options = new RendererOptions(
            FileNameMode: FileNameMode.CleanGenerics,
            RootNamespaceToTrim: "Xml2Doc.Sample",
            CodeBlockLanguage: "csharp"
        );
        var renderer = new MarkdownRenderer(model, options);

        // Render to a temp directory
        var outDir = Path.Combine(Path.GetTempPath(), "Xml2Doc.Tests", Path.GetRandomFileName());
        Directory.CreateDirectory(outDir);
        renderer.RenderToDirectory(outDir);

        // Expected snapshots under the project dir
        var snapDir = Path.Combine(SnapRoot, "PerType_CleanNames");
        Directory.Exists(snapDir).ShouldBeTrue($"Missing snapshot directory: {snapDir}. Seed it once from current output, then commit it.");

        var actualFiles = Directory.GetFiles(outDir, "*.md", SearchOption.TopDirectoryOnly)
                                   .Select(f => Path.GetFileName(f) ?? string.Empty)
                                   .Where(n => n.Length > 0)
                                   .OrderBy(x => x)
                                   .ToArray();

        var expectedFiles = Directory.GetFiles(snapDir, "*.verified.md", SearchOption.TopDirectoryOnly)
                                     .Select(f => {
                                         var name = Path.GetFileName(f);
                                         return name is null ? null : name.Replace(".verified.md", ".md");
                                     })
                                     .Where(n => !string.IsNullOrEmpty(n))
                                     .OrderBy(x => x)
                                     .ToArray();

        actualFiles.ShouldBe(expectedFiles, customMessage: "Per-type output file set differs from snapshots.");

        foreach (var file in actualFiles)
        {
            var actualPath = Path.Combine(outDir, file);
            var expectedPath = Path.Combine(snapDir, file.Replace(".md", ".verified.md"));

            File.Exists(expectedPath).ShouldBeTrue($"Missing snapshot: {expectedPath}. Seed it once from current output, then commit it.");

            var actual = await File.ReadAllTextAsync(actualPath);
            var expected = await File.ReadAllTextAsync(expectedPath);

            Normalize(actual).ShouldBe(Normalize(expected), customMessage: $"Mismatch in {file}");
        }
    }

    [Fact]
    public async Task Generic_BraceHandling_IsClean()
    {
        var xml = Path.ChangeExtension(typeof(Xml2Doc.Sample.GenericPlayground).Assembly.Location, ".xml");
        File.Exists(xml).ShouldBeTrue($"Missing XML: {xml}");

        var model = Xml2Doc.Core.Models.Xml2Doc.Load(xml);
        var options = new RendererOptions(
            FileNameMode: FileNameMode.CleanGenerics,
            RootNamespaceToTrim: "Xml2Doc.Sample",
            CodeBlockLanguage: "csharp"
        );
        var renderer = new MarkdownRenderer(model, options);

        // Render per-type and read the GenericPlayground page
        var outDir = Path.Combine(Path.GetTempPath(), "Xml2Doc.Tests", Path.GetRandomFileName());
        Directory.CreateDirectory(outDir);
        renderer.RenderToDirectory(outDir);

        var mdPath = Path.Combine(outDir, "Xml2Doc.Sample.GenericPlayground.md");
        File.Exists(mdPath).ShouldBeTrue($"Missing generated page: {mdPath}");

        var md = await File.ReadAllTextAsync(mdPath);

        // Normalize: strip backticks, collapse whitespace a bit
        md = md.Replace("\r\n", "\n");
        md = md.Replace("`", "");
        md = Regex.Replace(md, @"\s+", " ");

        // Build tolerant regexes (case-insensitive)
        // Optional namespace on XItem, optional parameter name after type
        string xitem = @"(?:Xml2Doc\.Sample\.)?XItem";
        string ident = @"(?:\s+[A-Za-z_][A-Za-z0-9_]*)?";

        // Flatten(IEnumerable<IEnumerable<XItem>> [param])
        var flattenPattern =
            $@"(?i)Method:\s*Flatten\s*\(\s*IEnumerable<\s*IEnumerable<\s*{xitem}\s*>\s*>\s*{ident}\s*\)";

        // Index(Dictionary<string, List<XItem>> [param])
        var indexPattern =
            $@"(?i)Method:\s*Index\s*\(\s*Dictionary\s*<\s*string\s*,\s*List<\s*{xitem}\s*>\s*>\s*{ident}\s*\)";

        // Transform<T1,T2>(List<Dictionary<T1, List<T2>>> [param])
        var transformPattern =
            $@"(?i)Method:\s*Transform<\s*T1\s*,\s*T2\s*>\s*\(\s*List<\s*Dictionary<\s*T1\s*,\s*List<\s*T2\s*>\s*>\s*>\s*{ident}\s*\)";

        Regex.IsMatch(md, flattenPattern).ShouldBeTrue(
            "Expected Flatten(IEnumerable<IEnumerable<XItem>>) signature (header or bullet) with optional param name."
        );
        Regex.IsMatch(md, indexPattern).ShouldBeTrue(
            "Expected Index(Dictionary<string, List<XItem>>) signature (header or bullet) with optional param name."
        );
        Regex.IsMatch(md, transformPattern).ShouldBeTrue(
            "Expected Transform<T1,T2>(List<Dictionary<T1, List<T2>>>) signature (header or bullet) with optional param name."
        );

        // Guardrails: no artifacts from prior bugs
        md.ShouldNotContain("})");
        md.ShouldNotContain("Int32)"); // old trailing-paren bug
    }
    private static string Normalize(string s) =>
        s.Replace("\r\n", "\n").Trim();
}
