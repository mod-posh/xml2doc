using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using Xml2Doc.Core;
using Xml2Doc.Sample;

public class RenderSnapshots
{
    // Resolve project directory from the test's bin folder
    private static string ProjectDir =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

    private static string SnapRoot => Path.Combine(ProjectDir, "__snapshots__");

    [Fact]
    public async Task SingleFile_CleanNames_Basic()
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

        var outPath = Path.GetTempFileName();
        renderer.RenderToSingleFile(outPath);
        var actual = await File.ReadAllTextAsync(outPath);

        var expectedPath = Path.Combine(SnapRoot, "SingleFile_CleanNames_Basic.verified.md");
        File.Exists(expectedPath).ShouldBeTrue($"Missing snapshot: {expectedPath}. Seed it once from current output, then commit it.");
        var expected = await File.ReadAllTextAsync(expectedPath);

        Normalize(actual).ShouldBe(Normalize(expected));
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

    private static string Normalize(string s) =>
        s.Replace("\r\n", "\n").Trim();
}
