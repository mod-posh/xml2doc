using Shouldly;
using System.Text.Json;
using Xml2Doc.Cli;
using Xunit;

namespace Xml2Doc.Tests;

public class CliAggregationTests
{
    [Fact]
    public void Main_MultipleInputsResolveMatchingInterfaceInheritDoc()
    {
        using var workspace = TemporaryWorkspace.Create();
        var contracts = workspace.Write(
            "Contracts.xml",
            """
            <doc><members>
              <member name="T:Contracts.IResourceTypeRegistryFactory">
                <summary>Registry factory contract.</summary>
              </member>
              <member name="M:Contracts.IResourceTypeRegistryFactory.FromAssemblies(System.Collections.Generic.IEnumerable{System.Reflection.Assembly})">
                <summary>Builds the intended registry.</summary>
              </member>
              <member name="T:Contracts.IOtherFactory">
                <summary>Other factory contract.</summary>
              </member>
              <member name="M:Contracts.IOtherFactory.FromAssemblies(System.Collections.Generic.IEnumerable{System.Reflection.Assembly})">
                <summary>Builds an unrelated registry.</summary>
              </member>
            </members></doc>
            """);
        var implementation = workspace.Write(
            "Implementation.xml",
            """
            <doc><members>
              <member name="T:Runtime.ResourceTypeRegistryFactory">
                <summary>Registry factory implementation.</summary>
              </member>
              <member name="M:Runtime.ResourceTypeRegistryFactory.FromAssemblies(System.Collections.Generic.IEnumerable{System.Reflection.Assembly})">
                <inheritdoc/>
              </member>
            </members></doc>
            """);
        var output = workspace.FullPath("docs");
        var originalError = Console.Error;
        using var standardError = new StringWriter();

        int exitCode;
        try
        {
            Console.SetError(standardError);
            exitCode = Program.Main(new[]
            {
                "--xml", implementation,
                "--xml", contracts,
                "--out", output,
                "--file-names", "clean"
            });
        }
        finally
        {
            Console.SetError(originalError);
        }

        exitCode.ShouldBe(0);
        var generated = File.ReadAllText(Path.Join(
            output,
            "Runtime.ResourceTypeRegistryFactory.md"));
        generated.ShouldContain("Builds the intended registry.");
        generated.ShouldNotContain("Builds an unrelated registry.");
        standardError.ToString().ShouldNotContain("XML2DOC004");
        standardError.ToString().ShouldNotContain("XML2DOC005");
    }

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
        var inputs = report.RootElement.GetProperty("xmlInputs")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToArray();
        inputs.ShouldBe(new[]
        {
            Path.GetFullPath(alpha),
            Path.GetFullPath(zebra)
        });
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
