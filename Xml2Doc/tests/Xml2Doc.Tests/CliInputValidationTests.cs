using Shouldly;
using Xml2Doc.Cli;
using Xunit;

namespace Xml2Doc.Tests;

public class CliInputValidationTests
{
    [Fact]
    public void Main_WhitespaceXmlReturnsInvalidArgumentExitCode()
    {
        var output = Path.Join(
            Path.GetTempPath(),
            "Xml2Doc.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            Program.Main(new[]
            {
                "--xml", "   ",
                "--out", output
            }).ShouldBe(1);

            Directory.Exists(output).ShouldBeFalse();
        }
        finally
        {
            if (Directory.Exists(output))
                Directory.Delete(output, recursive: true);
        }
    }
}
