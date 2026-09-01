using System.Text;
using System.Text.Json;
using Shouldly;
using Xml2Doc.Cli;

namespace Xml2Doc.Tests;

public class CliMetadataTests
{
    private const string FixtureXml = """
        <?xml version="1.0"?>
        <doc>
          <assembly><name>Fixture</name></assembly>
          <members>
            <member name="T:Temp.Widget">
              <summary>A widget.</summary>
            </member>
          </members>
        </doc>
        """;

    [Fact]
    public void Main_RepeatedMetadataArgumentsEmitDocumentFrontMatter()
    {
        var root = CreateTestRoot();

        try
        {
            var xmlPath = Path.Join(root, "input.xml");
            var outputPath = Path.Join(root, "api.md");
            File.WriteAllText(xmlPath, FixtureXml, new UTF8Encoding(false));

            var exitCode = Program.Main(new[]
            {
                "--xml", xmlPath,
                "--out", outputPath,
                "--single",
                "--metadata", "package=Fixture",
                "--metadata", "documentId=caller-id"
            });

            exitCode.ShouldBe(0);
            File.ReadAllText(outputPath).ShouldStartWith(
                "---\n" +
                "documentId: \"xml2doc:single-file\"\n" +
                "documentKind: \"singlefile\"\n" +
                "namespace: null\n" +
                "outputPath: \"api.md\"\n" +
                "package: \"Fixture\"\n" +
                "symbol: null\n" +
                "---\n");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Main_JsonMetadataSupportsListsAndCliKeyPrecedence()
    {
        var root = CreateTestRoot();

        try
        {
            var xmlPath = Path.Join(root, "input.xml");
            var outputPath = Path.Join(root, "api.md");
            var configPath = Path.Join(root, "xml2doc.json");
            File.WriteAllText(xmlPath, FixtureXml, new UTF8Encoding(false));
            File.WriteAllText(
                configPath,
                JsonSerializer.Serialize(new CliConfig
                {
                    Xml = xmlPath,
                    Out = outputPath,
                    Single = true,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["package"] = "config",
                        ["published"] = true,
                        ["tags"] = new[] { "api", "stable" }
                    }
                }),
                new UTF8Encoding(false));

            var exitCode = Program.Main(new[]
            {
                "--config", configPath,
                "--metadata", "package=cli"
            });

            exitCode.ShouldBe(0);
            var markdown = File.ReadAllText(outputPath);
            markdown.ShouldContain("package: \"cli\"");
            markdown.ShouldContain("published: true");
            markdown.ShouldContain("tags: [\"api\", \"stable\"]");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Main_MetadataWithoutEqualsReturnsValidationFailure()
    {
        Program.Main(new[] { "--metadata", "missing-separator" }).ShouldBe(1);
    }

    private static string CreateTestRoot()
    {
        var root = Path.Join(
            Path.GetTempPath(),
            "xml2doc-cli-metadata-tests",
            Path.GetFileName(Path.GetRandomFileName()));
        Directory.CreateDirectory(root);
        return root;
    }
}
