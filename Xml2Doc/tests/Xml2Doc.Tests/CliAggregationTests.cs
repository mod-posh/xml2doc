using Shouldly;
using System.Text.Json;
using Xml2Doc.Cli;
using Xunit;

namespace Xml2Doc.Tests;

public class CliAggregationTests
{
    [Fact]
    public void Main_RepeatedXmlArgumentsRenderOneAggregateOutput()
    {
        using var workspace = TemporaryWorkspace.Create();
        var zebra = workspace.WriteXml("Zebra.xml", "T:Project.Zebra");
        var alpha = workspace.WriteXml("Alpha.xml", "T:Project.Alpha");
        var output = workspace.FullPath("docs");
        var reportPath = workspace.FullPath("report.json");

        Program.Main(new[]
        {
            "--xml", zebra,
            "--xml", alpha,
            "--out", output,
            "--report", reportPath
        }).ShouldBe(0);

        File.Exists(Path.Join(output, "index.md")).ShouldBeTrue();
        var index = File.ReadAllText(Path.Join(output, "index.md"));
        index.ShouldContain("Project.Alpha");
        index.ShouldContain("Project.Zebra");
        index.IndexOf("Project.Alpha", StringComparison.Ordinal)
            .ShouldBeLessThan(index.IndexOf(
                "Project.Zebra",
                StringComparison.Ordinal));

        using var report = JsonDocument.Parse(File.ReadAllText(reportPath));
        var inputs = report.RootElement.GetProperty("xmlInputs");
        inputs.GetArrayLength().ShouldBe(2);
        inputs[0].GetString().ShouldBe(Path.GetFullPath(alpha));
        inputs[1].GetString().ShouldBe(Path.GetFullPath(zebra));
    }

    [Fact]
    public void Main_ReversingXmlArgumentsDoesNotChangeGeneratedOutput()
    {
        using var workspace = TemporaryWorkspace.Create();
        var zebra = workspace.WriteXml("Zebra.xml", "T:Project.Zebra");
        var alpha = workspace.WriteXml("Alpha.xml", "T:Project.Alpha");
        var forwardOutput = workspace.FullPath("forward");
        var reverseOutput = workspace.FullPath("reverse");

        Program.Main(new[]
        {
            "--xml", alpha,
            "--xml", zebra,
            "--out", forwardOutput
        }).ShouldBe(0);
        Program.Main(new[]
        {
            "--xml", zebra,
            "--xml", alpha,
            "--out", reverseOutput
        }).ShouldBe(0);

        var forwardFiles = Directory.GetFiles(forwardOutput)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();
        var reverseFiles = Directory.GetFiles(reverseOutput)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();

        reverseFiles.Select(Path.GetFileName)
            .ShouldBe(forwardFiles.Select(Path.GetFileName));
        for (var index = 0; index < forwardFiles.Length; index++)
        {
            File.ReadAllBytes(reverseFiles[index])
                .ShouldBe(File.ReadAllBytes(forwardFiles[index]));
        }
    }

    [Fact]
    public void Main_JsonXmlInputsRenderOneAggregateOutput()
    {
        using var workspace = TemporaryWorkspace.Create();
        var alpha = workspace.WriteXml("Alpha.xml", "T:Project.Alpha");
        var zebra = workspace.WriteXml("Zebra.xml", "T:Project.Zebra");
        var output = workspace.FullPath("docs");
        var configPath = workspace.Write(
            "xml2doc.json",
            JsonSerializer.Serialize(new CliConfig
            {
                Xml = workspace.FullPath("ignored.xml"),
                XmlInputs = new[] { zebra, alpha },
                Out = output
            }));

        Program.Main(new[] { "--config", configPath }).ShouldBe(0);

        var index = File.ReadAllText(Path.Join(output, "index.md"));
        index.ShouldContain("Project.Alpha");
        index.ShouldContain("Project.Zebra");
    }

    [Fact]
    public void Main_CliXmlArgumentsOverrideConfiguredXmlInputs()
    {
        using var workspace = TemporaryWorkspace.Create();
        var cliInput = workspace.WriteXml("Cli.xml", "T:Project.Cli");
        var configInput = workspace.WriteXml("Config.xml", "T:Project.Config");
        var output = workspace.FullPath("docs");
        var configPath = workspace.Write(
            "xml2doc.json",
            JsonSerializer.Serialize(new CliConfig
            {
                XmlInputs = new[] { configInput },
                Out = output
            }));

        Program.Main(new[]
        {
            "--config", configPath,
            "--xml", cliInput
        }).ShouldBe(0);

        var index = File.ReadAllText(Path.Join(output, "index.md"));
        index.ShouldContain("Project.Cli");
        index.ShouldNotContain("Project.Config");
    }

    private sealed class TemporaryWorkspace : IDisposable
    {
        private TemporaryWorkspace(string path) => Path = path;

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

        public string WriteXml(string fileName, string memberId) =>
            Write(
                fileName,
                $"<doc><members><member name=\"{memberId}\">" +
                "<summary>Documented type.</summary>" +
                "</member></members></doc>");

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
