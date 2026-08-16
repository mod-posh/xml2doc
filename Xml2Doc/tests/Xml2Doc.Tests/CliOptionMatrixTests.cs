using Shouldly;
using System.Text.Json;
using Xml2Doc.Cli;
using Xunit;

namespace Xml2Doc.Tests;

public class CliOptionMatrixTests
{
    private static string SampleXml =>
        Path.GetFullPath(
            AppContext.BaseDirectory +
            ".." + Path.DirectorySeparatorChar +
            ".." + Path.DirectorySeparatorChar +
            ".." + Path.DirectorySeparatorChar +
            "Assets" + Path.DirectorySeparatorChar +
            "Xml2Doc.Sample.xml");

    [Fact]
    public void Main_WithCompatibleCliOptionsMapsEveryValue()
    {
        using var workspace = TemporaryWorkspace.Create();
        var docsPath = workspace.FullPath("docs");
        var reportPath = workspace.FullPath("cli-report.json");
        var templatePath = workspace.Write(
            "template.md",
            "<!-- {{kind}}:{{title}} -->\n{{content}}");
        var frontMatterPath = workspace.Write(
            "front-matter.yml",
            "---\nlayout: api\n---\n");
        var aliasMapPath = workspace.Write("aliases.json", "{}");

        var exitCode = Program.Main(new[]
        {
            "--xml", SampleXml,
            "--out", docsPath,
            "--file-names", "clean",
            "--rootns", "Xml2Doc.Sample",
            "--trim-rootns-filenames",
            "--lang", "text",
            "--report", reportPath,
            "--anchor-algorithm", "kramdown",
            "--template", templatePath,
            "--front-matter", frontMatterPath,
            "--auto-link",
            "--alias-map", aliasMapPath,
            "--external-docs", "https://docs.example/api",
            "--toc",
            "--namespace-index",
            "--no-index",
            "--basename-only",
            "--parallel", "2",
            "--line-endings", "lf"
        });

        exitCode.ShouldBe(0);
        File.Exists(Path.Join(docsPath, "index.md")).ShouldBeFalse();
        File.Exists(Path.Join(docsPath, "namespaces.md")).ShouldBeTrue();
        using var report = JsonDocument.Parse(File.ReadAllText(reportPath));
        AssertMappedOptions(
            report.RootElement,
            templatePath,
            frontMatterPath,
            aliasMapPath);
    }

    [Fact]
    public void Main_WithCompatibleJsonOptionsMapsEveryValue()
    {
        using var workspace = TemporaryWorkspace.Create();
        var docsPath = workspace.FullPath("docs");
        var reportPath = workspace.FullPath("config-report.json");
        var templatePath = workspace.Write(
            "template.md",
            "<!-- {{kind}}:{{title}} -->\n{{content}}");
        var frontMatterPath = workspace.Write(
            "front-matter.yml",
            "---\nlayout: api\n---\n");
        var aliasMapPath = workspace.Write("aliases.json", "{}");
        var configPath = workspace.Write(
            "xml2doc.json",
            JsonSerializer.Serialize(new CliConfig
            {
                Xml = SampleXml,
                Out = docsPath,
                FileNames = "clean",
                RootNamespace = "Xml2Doc.Sample",
                TrimRootNamespaceInFileNames = true,
                CodeLanguage = "text",
                Report = reportPath,
                AnchorAlgorithm = "kramdown",
                Template = templatePath,
                FrontMatter = frontMatterPath,
                AutoLink = true,
                AliasMap = aliasMapPath,
                ExternalDocs = "https://docs.example/api",
                Toc = true,
                NamespaceIndex = true,
                GenerateIndex = false,
                BasenameOnly = true,
                Parallel = 2,
                LineEndings = "lf"
            }));

        var exitCode = Program.Main(new[] { "--config", configPath });

        exitCode.ShouldBe(0);
        File.Exists(Path.Join(docsPath, "index.md")).ShouldBeFalse();
        File.Exists(Path.Join(docsPath, "namespaces.md")).ShouldBeTrue();
        using var report = JsonDocument.Parse(File.ReadAllText(reportPath));
        AssertMappedOptions(
            report.RootElement,
            templatePath,
            frontMatterPath,
            aliasMapPath);
    }

    [Fact]
    public void Main_CliValuesOverrideJsonConfiguration()
    {
        using var workspace = TemporaryWorkspace.Create();
        var outputPath = workspace.FullPath("api.md");
        var configReportPath = workspace.FullPath("config-report.json");
        var cliReportPath = workspace.FullPath("cli-report.json");
        var configPath = workspace.Write(
            "xml2doc.json",
            JsonSerializer.Serialize(new CliConfig
            {
                Xml = SampleXml,
                Out = outputPath,
                Single = false,
                FileNames = "verbatim",
                RootNamespace = "Config.Namespace",
                CodeLanguage = "fsharp",
                Report = configReportPath,
                AnchorAlgorithm = "github",
                ExternalDocs = "https://config.example/api",
                BasenameOnly = false,
                Parallel = 1,
                LineEndings = "crlf"
            }));

        var exitCode = Program.Main(new[]
        {
            "--config", configPath,
            "--single",
            "--file-names", "clean",
            "--rootns", "Cli.Namespace",
            "--lang", "csharp",
            "--report", cliReportPath,
            "--anchor-algorithm", "kramdown",
            "--external-docs", "https://cli.example/api",
            "--basename-only",
            "--parallel", "2",
            "--line-endings", "lf"
        });

        exitCode.ShouldBe(0);
        File.Exists(outputPath).ShouldBeTrue();
        File.Exists(configReportPath).ShouldBeFalse();
        using var report = JsonDocument.Parse(File.ReadAllText(cliReportPath));
        var root = report.RootElement;
        root.GetProperty("single").GetBoolean().ShouldBeTrue();
        var options = root.GetProperty("options");
        options.GetProperty("fileNameMode").GetString()
            .ShouldBe("CleanGenerics");
        options.GetProperty("rootNs").GetString()
            .ShouldBe("Cli.Namespace");
        options.GetProperty("lang").GetString().ShouldBe("csharp");
        options.GetProperty("anchorAlgorithm").GetString()
            .ShouldBe("kramdown");
        options.GetProperty("externalDocs").GetString()
            .ShouldBe("https://cli.example/api");
        options.GetProperty("basenameOnly").GetBoolean().ShouldBeTrue();
        options.GetProperty("parallel").GetInt32().ShouldBe(2);
        options.GetProperty("lineEndings").GetString().ShouldBe("Lf");
    }

    [Fact]
    public void Main_WithJsonSingleWritesOneCombinedFile()
    {
        using var workspace = TemporaryWorkspace.Create();
        var outputPath = workspace.FullPath("api.md");
        var configPath = workspace.Write(
            "xml2doc.json",
            JsonSerializer.Serialize(new CliConfig
            {
                Xml = SampleXml,
                Out = outputPath,
                Single = true
            }));

        Program.Main(new[] { "--config", configPath }).ShouldBe(0);

        File.Exists(outputPath).ShouldBeTrue();
        File.ReadAllText(outputPath).ShouldContain("# API Reference");
    }

    [Fact]
    public void Main_WithJsonDryRunReportsPlanWithoutWriting()
    {
        using var workspace = TemporaryWorkspace.Create();
        var docsPath = workspace.FullPath("docs");
        var reportPath = workspace.FullPath("report.json");
        var configPath = workspace.Write(
            "xml2doc.json",
            JsonSerializer.Serialize(new CliConfig
            {
                Xml = SampleXml,
                Out = docsPath,
                DryRun = true,
                Report = reportPath
            }));

        Program.Main(new[] { "--config", configPath }).ShouldBe(0);

        Directory.Exists(docsPath).ShouldBeFalse();
        using var report = JsonDocument.Parse(File.ReadAllText(reportPath));
        report.RootElement.GetProperty("dryRun").GetBoolean()
            .ShouldBeTrue();
        report.RootElement.GetProperty("wouldWrite")
            .GetArrayLength().ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Main_WithMinimalArgumentsPreservesCompatibleDefaults()
    {
        using var workspace = TemporaryWorkspace.Create();
        var docsPath = workspace.FullPath("docs");
        var reportPath = workspace.FullPath("report.json");

        Program.Main(new[]
        {
            "--xml", SampleXml,
            "--out", docsPath,
            "--report", reportPath
        }).ShouldBe(0);

        using var report = JsonDocument.Parse(File.ReadAllText(reportPath));
        var root = report.RootElement;
        root.GetProperty("single").GetBoolean().ShouldBeFalse();
        root.GetProperty("dryRun").GetBoolean().ShouldBeFalse();
        root.GetProperty("diffRequested").GetBoolean().ShouldBeFalse();
        var options = root.GetProperty("options");
        options.GetProperty("fileNameMode").GetString()
            .ShouldBe("Verbatim");
        options.GetProperty("rootNs").ValueKind.ShouldBe(
            JsonValueKind.Null);
        options.GetProperty("trimRootNsInFileNames")
            .GetBoolean().ShouldBeFalse();
        options.GetProperty("lang").GetString().ShouldBe("csharp");
        options.GetProperty("anchorAlgorithm").GetString()
            .ShouldBe("default");
        options.GetProperty("autoLink").GetBoolean().ShouldBeFalse();
        options.GetProperty("toc").GetBoolean().ShouldBeFalse();
        options.GetProperty("namespaceIndex").GetBoolean().ShouldBeFalse();
        options.GetProperty("generateIndex").GetBoolean().ShouldBeTrue();
        options.GetProperty("basenameOnly").GetBoolean().ShouldBeFalse();
        options.GetProperty("parallel").ValueKind.ShouldBe(
            JsonValueKind.Null);
        options.GetProperty("pruneStaleFiles")
            .GetBoolean().ShouldBeFalse();
        options.GetProperty("lineEndings").GetString().ShouldBe("Lf");
    }

    private static void AssertMappedOptions(
        JsonElement report,
        string templatePath,
        string frontMatterPath,
        string aliasMapPath)
    {
        report.GetProperty("single").GetBoolean().ShouldBeFalse();
        var options = report.GetProperty("options");
        options.GetProperty("fileNameMode").GetString()
            .ShouldBe("CleanGenerics");
        options.GetProperty("rootNs").GetString()
            .ShouldBe("Xml2Doc.Sample");
        options.GetProperty("trimRootNsInFileNames")
            .GetBoolean().ShouldBeTrue();
        options.GetProperty("lang").GetString().ShouldBe("text");
        options.GetProperty("anchorAlgorithm").GetString()
            .ShouldBe("kramdown");
        options.GetProperty("templatePath").GetString()
            .ShouldBe(templatePath);
        options.GetProperty("frontMatterPath").GetString()
            .ShouldBe(frontMatterPath);
        options.GetProperty("autoLink").GetBoolean().ShouldBeTrue();
        options.GetProperty("aliasMapPath").GetString()
            .ShouldBe(aliasMapPath);
        options.GetProperty("externalDocs").GetString()
            .ShouldBe("https://docs.example/api");
        options.GetProperty("toc").GetBoolean().ShouldBeTrue();
        options.GetProperty("namespaceIndex").GetBoolean().ShouldBeTrue();
        options.GetProperty("generateIndex").GetBoolean().ShouldBeFalse();
        options.GetProperty("basenameOnly").GetBoolean().ShouldBeTrue();
        options.GetProperty("parallel").GetInt32().ShouldBe(2);
        options.GetProperty("pruneStaleFiles").GetBoolean().ShouldBeFalse();
        options.GetProperty("lineEndings").GetString().ShouldBe("Lf");
    }

    private sealed class TemporaryWorkspace : IDisposable
    {
        private TemporaryWorkspace(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryWorkspace Create() =>
            new(System.IO.Path.Join(
                System.IO.Path.GetTempPath(),
                "Xml2Doc.Tests",
                Guid.NewGuid().ToString("N")));

        public string FullPath(string relativePath) =>
            System.IO.Path.Join(Path, relativePath);

        public string Write(string relativePath, string content)
        {
            var fullPath = FullPath(relativePath);
            Directory.CreateDirectory(
                System.IO.Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content);
            return fullPath;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
