using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xml2Doc.Cli;
using Xml2Doc.Core.OutputLifecycle;
using Xunit;

namespace Xml2Doc.Tests
{
    public class CliLifecycleTests
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
        public void Main_WhenOptionIsUnknown_ReturnsValidationFailure()
        {
            var result = CaptureStandardError(() => Program.Main(new[]
            {
                "--unknown"
            }));

            result.ExitCode.ShouldBe(1);
            result.StandardError.ShouldContain("Unknown option: --unknown");
        }

        [Fact]
        public void Main_WhenOptionValueIsMissing_ReturnsValidationFailure()
        {
            var result = CaptureStandardError(() => Program.Main(new[]
            {
                "--xml", "--out", "docs"
            }));

            result.ExitCode.ShouldBe(1);
            result.StandardError.ShouldContain(
                "Option --xml requires a value.");
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("0")]
        [InlineData("-1")]
        public void Main_WhenParallelValueIsInvalid_ReturnsValidationFailure(
            string value)
        {
            var result = CaptureStandardError(() => Program.Main(new[]
            {
                "--parallel", value
            }));

            result.ExitCode.ShouldBe(1);
            result.StandardError.ShouldContain(
                "--parallel must be an integer greater than zero.");
        }

        [Theory]
        [InlineData("--file-names", "invalid", "--file-names must be one of")]
        [InlineData("--anchor-algorithm", "invalid", "--anchor-algorithm must be one of")]
        public void Main_WhenEnumeratedValueIsInvalid_ReturnsValidationFailure(
            string option,
            string value,
            string expectedError)
        {
            using var output = TemporaryOutput.Create();
            var result = CaptureStandardError(() => Program.Main(new[]
            {
                "--xml", SampleXml,
                "--out", output.Path,
                option, value
            }));

            result.ExitCode.ShouldBe(1);
            result.StandardError.ShouldContain(expectedError);
            Directory.Exists(output.Path).ShouldBeFalse();
        }

        [Fact]
        public void Main_WhenConfigFileIsMissing_ReturnsValidationFailure()
        {
            using var output = TemporaryOutput.Create();
            var configPath = output.FullPath("missing.json");

            var result = CaptureStandardError(() => Program.Main(new[]
            {
                "--config", configPath
            }));

            result.ExitCode.ShouldBe(1);
            result.StandardError.ShouldContain(
                "Configuration file was not found:");
        }

        [Theory]
        [InlineData("{")]
        [InlineData("{\"Unexpected\":true}")]
        public void Main_WhenConfigJsonIsInvalid_ReturnsValidationFailure(
            string json)
        {
            using var output = TemporaryOutput.Create();
            var configPath = output.FullPath("xml2doc.json");
            output.Write("xml2doc.json", json);

            var result = CaptureStandardError(() => Program.Main(new[]
            {
                "--config", configPath
            }));

            result.ExitCode.ShouldBe(1);
            result.StandardError.ShouldContain("Invalid configuration file");
        }

        [Fact]
        public void Main_WhenConfiguredParallelValueIsInvalid_ReturnsValidationFailure()
        {
            using var output = TemporaryOutput.Create();
            var configPath = output.FullPath("xml2doc.json");
            output.Write(
                "xml2doc.json",
                JsonSerializer.Serialize(new CliConfig
                {
                    Xml = SampleXml,
                    Out = output.FullPath("docs"),
                    Parallel = 0
                }));

            var result = CaptureStandardError(() => Program.Main(new[]
            {
                "--config", configPath
            }));

            result.ExitCode.ShouldBe(1);
            result.StandardError.ShouldContain(
                "--parallel must be an integer greater than zero.");
            Directory.Exists(output.FullPath("docs")).ShouldBeFalse();
        }

        [Theory]
        [InlineData("--toc", "--toc is only supported for directory output.")]
        [InlineData("--namespace-index", "--namespace-index is only supported for directory output.")]
        public void Main_WhenDirectoryFeatureIsUsedWithSingleFile_ReturnsValidationFailure(
            string option,
            string expectedError)
        {
            using var output = TemporaryOutput.Create();
            var outputPath = output.FullPath("api.md");
            var result = CaptureStandardError(() => Program.Main(new[]
            {
                "--xml", SampleXml,
                "--out", outputPath,
                "--single",
                option
            }));

            result.ExitCode.ShouldBe(1);
            result.StandardError.ShouldContain(expectedError);
            File.Exists(outputPath).ShouldBeFalse();
        }

        [Fact]
        public void Main_WhenDryRunAndDiffAreCombined_ReturnsValidationFailure()
        {
            using var output = TemporaryOutput.Create();
            var result = CaptureStandardError(() => Program.Main(new[]
            {
                "--xml", SampleXml,
                "--out", output.Path,
                "--dry-run",
                "--diff"
            }));

            result.ExitCode.ShouldBe(1);
            result.StandardError.ShouldContain(
                "--dry-run and --diff cannot be used together");
            Directory.Exists(output.Path).ShouldBeFalse();
        }

        [Fact]
        public void Main_WhenPruningHasNoIdentity_ReturnsValidationFailure()
        {
            using var output = TemporaryOutput.Create();

            var exitCode = Program.Main(new[]
            {
                "--xml", SampleXml,
                "--out", output.Path,
                "--prune-stale"
            });

            exitCode.ShouldBe(1);
            Directory.Exists(output.Path).ShouldBeFalse();
        }

        [Fact]
        public void Main_WhenPruningIsUsedWithSingleFile_ReturnsValidationFailure()
        {
            using var output = TemporaryOutput.Create();

            var exitCode = Program.Main(new[]
            {
                "--xml", SampleXml,
                "--out", output.FullPath("api.md"),
                "--single",
                "--prune-stale",
                "--manifest-id", "sample-project"
            });

            exitCode.ShouldBe(1);
            Directory.Exists(output.Path).ShouldBeFalse();
        }

        [Fact]
        public void Main_WithPruning_DeletesOwnedStaleFileAndPreservesUntrackedFile()
        {
            using var output = TemporaryOutput.Create();
            var reportPath = output.FullPath("report.json");
            var location = OutputManifestLocation.Create(
                output.Path,
                "sample-project");
            output.Write("stale.md", "stale");
            output.Write("hand-authored.md", "keep");
            OutputManifestStore.Save(location, new[] { "stale.md" });

            var exitCode = Program.Main(new[]
            {
                "--xml", SampleXml,
                "--out", output.Path,
                "--prune-stale",
                "--manifest-id", "sample-project",
                "--report", reportPath
            });

            exitCode.ShouldBe(0);
            output.Exists("stale.md").ShouldBeFalse();
            output.Exists("hand-authored.md").ShouldBeTrue();
            OutputManifestStore.Load(location)!.Files.ShouldNotBeEmpty();
            using var report = JsonDocument.Parse(
                File.ReadAllText(reportPath));
            var plannedFiles = report.RootElement
                .GetProperty("plannedFiles")
                .EnumerateArray()
                .Select(item => item.GetString())
                .ToArray();
            var writtenFiles = report.RootElement
                .GetProperty("writtenFiles")
                .EnumerateArray()
                .Select(item => item.GetString())
                .ToArray();
            writtenFiles.ShouldBe(plannedFiles);
            report.RootElement.GetProperty("skippedFiles")
                .GetArrayLength().ShouldBe(0);
            report.RootElement.GetProperty("prunedFiles")
                .EnumerateArray()
                .Select(item => item.GetString())
                .ToArray()
                .ShouldBe(new[] { output.FullPath("stale.md") });
        }

        [Fact]
        public void Main_WithConfig_EnablesInvocationScopedPruning()
        {
            using var output = TemporaryOutput.Create();
            var location = OutputManifestLocation.Create(
                output.Path,
                "config-project");
            output.Write("stale.md", "stale");
            OutputManifestStore.Save(location, new[] { "stale.md" });
            var configPath = output.FullPath("xml2doc.json");
            output.Write(
                "xml2doc.json",
                JsonSerializer.Serialize(new CliConfig
                {
                    Xml = SampleXml,
                    Out = output.Path,
                    PruneStaleFiles = true,
                    ManifestIdentity = "config-project"
                }));

            var exitCode = Program.Main(new[] { "--config", configPath });

            exitCode.ShouldBe(0);
            output.Exists("stale.md").ShouldBeFalse();
            OutputManifestStore.Load(location)!.Files.ShouldNotBeEmpty();
        }

        [Fact]
        public void Main_WhenCliAndConfigSpecifyIdentity_UsesCliIdentity()
        {
            using var output = TemporaryOutput.Create();
            var cliLocation = OutputManifestLocation.Create(
                output.Path,
                "cli-project");
            var configLocation = OutputManifestLocation.Create(
                output.Path,
                "config-project");
            output.Write("stale.md", "stale");
            OutputManifestStore.Save(cliLocation, new[] { "stale.md" });
            var configPath = output.FullPath("xml2doc.json");
            output.Write(
                "xml2doc.json",
                JsonSerializer.Serialize(new CliConfig
                {
                    Xml = SampleXml,
                    Out = output.Path,
                    PruneStaleFiles = true,
                    ManifestIdentity = "config-project"
                }));

            var exitCode = Program.Main(new[]
            {
                "--config", configPath,
                "--manifest-id", "cli-project"
            });

            exitCode.ShouldBe(0);
            output.Exists("stale.md").ShouldBeFalse();
            OutputManifestStore.Load(cliLocation)!.Files.ShouldNotBeEmpty();
            File.Exists(configLocation.ManifestPath).ShouldBeFalse();
        }

        [Fact]
        public void Main_WithDryRun_ReportsOnlyStaleFilesOwnedByIdentity()
        {
            using var output = TemporaryOutput.Create();
            var location = OutputManifestLocation.Create(
                output.Path,
                "sample-project");
            output.Write("stale.md", "stale");
            output.Write("hand-authored.md", "keep");
            OutputManifestStore.Save(location, new[] { "stale.md" });
            var previousManifest = File.ReadAllBytes(location.ManifestPath);
            var reportPath = output.FullPath("report.json");

            var exitCode = Program.Main(new[]
            {
                "--xml", SampleXml,
                "--out", output.Path,
                "--dry-run",
                "--report", reportPath,
                "--prune-stale",
                "--manifest-id", "sample-project"
            });

            exitCode.ShouldBe(0);
            output.Exists("stale.md").ShouldBeTrue();
            output.Exists("hand-authored.md").ShouldBeTrue();
            File.ReadAllBytes(location.ManifestPath).ShouldBe(previousManifest);

            using var report = JsonDocument.Parse(File.ReadAllText(reportPath));
            var wouldDelete = report.RootElement
                .GetProperty("wouldDelete")
                .EnumerateArray()
                .Select(item => item.GetString())
                .ToArray();
            wouldDelete.ShouldBe(new[] { output.FullPath("stale.md") });
            var plannedFiles = report.RootElement
                .GetProperty("plannedFiles")
                .EnumerateArray()
                .Select(item => item.GetString())
                .ToArray();
            var wouldWrite = report.RootElement
                .GetProperty("wouldWrite")
                .EnumerateArray()
                .Select(item => item.GetString())
                .ToArray();
            wouldWrite.ShouldBe(plannedFiles);
            report.RootElement.GetProperty("writtenFiles")
                .GetArrayLength().ShouldBe(0);
            report.RootElement.GetProperty("skippedFiles")
                .GetArrayLength().ShouldBe(0);
            report.RootElement.GetProperty("prunedFiles")
                .GetArrayLength().ShouldBe(0);
            report.RootElement.TryGetProperty("timings", out _)
                .ShouldBeTrue();
            report.RootElement.TryGetProperty("timestamp", out _)
                .ShouldBeFalse();
        }

        [Fact]
        public void Main_WithDiffAndMissingOutputReportsAddedWithoutWriting()
        {
            using var output = TemporaryOutput.Create();
            var docsPath = output.FullPath("docs");
            var reportPath = output.FullPath("report.json");

            var exitCode = Program.Main(new[]
            {
                "--xml", SampleXml,
                "--out", docsPath,
                "--diff",
                "--report", reportPath
            });

            exitCode.ShouldBe(3);
            Directory.Exists(docsPath).ShouldBeFalse();
            using var report = JsonDocument.Parse(
                File.ReadAllText(reportPath));
            var differences = report.RootElement
                .GetProperty("differences");
            differences.GetProperty("hasDifferences")
                .GetBoolean().ShouldBeTrue();
            differences.GetProperty("addedFiles")
                .GetArrayLength().ShouldBeGreaterThan(0);
            differences.GetProperty("changedFiles")
                .GetArrayLength().ShouldBe(0);
            differences.GetProperty("unchangedFiles")
                .GetArrayLength().ShouldBe(0);
            differences.GetProperty("removedFiles")
                .GetArrayLength().ShouldBe(0);
            report.RootElement.GetProperty("writtenFiles")
                .GetArrayLength().ShouldBe(0);
        }

        [Fact]
        public void Main_WithDiffAndMatchingOutputReportsNoDifferences()
        {
            using var output = TemporaryOutput.Create();
            var docsPath = output.FullPath("docs");
            var reportPath = output.FullPath("report.json");
            var generationArgs = new[]
            {
                "--xml", SampleXml,
                "--out", docsPath
            };
            Program.Main(generationArgs).ShouldBe(0);
            var previousFiles = Directory.GetFiles(docsPath, "*.md");
            var previousBytes = previousFiles.ToDictionary(
                path => path,
                File.ReadAllBytes);

            var exitCode = Program.Main(generationArgs.Concat(new[]
            {
                "--diff",
                "--report", reportPath
            }).ToArray());

            exitCode.ShouldBe(0);
            foreach (var file in previousFiles)
                File.ReadAllBytes(file).ShouldBe(previousBytes[file]);
            using var report = JsonDocument.Parse(
                File.ReadAllText(reportPath));
            var differences = report.RootElement
                .GetProperty("differences");
            differences.GetProperty("hasDifferences")
                .GetBoolean().ShouldBeFalse();
            differences.GetProperty("unchangedFiles")
                .GetArrayLength().ShouldBe(previousFiles.Length);
        }

        [Fact]
        public void Main_WithDiffReportsChangedAndRemovedFilesWithoutMutation()
        {
            using var output = TemporaryOutput.Create();
            var docsPath = output.FullPath("docs");
            var reportPath = output.FullPath("report.json");
            var manifestIdentity = "diff-project";
            var generationArgs = new[]
            {
                "--xml", SampleXml,
                "--out", docsPath,
                "--prune-stale",
                "--manifest-id", manifestIdentity
            };
            Program.Main(generationArgs).ShouldBe(0);
            var changedPath = Directory.GetFiles(docsPath, "*.md").First();
            File.WriteAllText(changedPath, "locally changed");
            var stalePath = Path.Join(docsPath, "stale.md");
            File.WriteAllText(stalePath, "stale");
            var location = OutputManifestLocation.Create(
                docsPath,
                manifestIdentity);
            var ownedFiles = OutputManifestStore.Load(location)!.Files
                .Concat(new[] { "stale.md" })
                .ToArray();
            OutputManifestStore.Save(location, ownedFiles);
            var previousManifest = File.ReadAllBytes(location.ManifestPath);

            var exitCode = Program.Main(generationArgs.Concat(new[]
            {
                "--diff",
                "--report", reportPath
            }).ToArray());

            exitCode.ShouldBe(3);
            File.ReadAllText(changedPath).ShouldBe("locally changed");
            File.ReadAllText(stalePath).ShouldBe("stale");
            File.ReadAllBytes(location.ManifestPath)
                .ShouldBe(previousManifest);
            using var report = JsonDocument.Parse(
                File.ReadAllText(reportPath));
            var differences = report.RootElement
                .GetProperty("differences");
            differences.GetProperty("changedFiles")
                .EnumerateArray()
                .Select(item => item.GetString())
                .ShouldContain(changedPath);
            differences.GetProperty("removedFiles")
                .EnumerateArray()
                .Select(item => item.GetString())
                .ShouldBe(new[] { stalePath });
        }

        [Fact]
        public void Main_WithConfiguredDiffUsesNonMutatingComparison()
        {
            using var output = TemporaryOutput.Create();
            var docsPath = output.FullPath("docs");
            var reportPath = output.FullPath("report.json");
            var configPath = output.FullPath("xml2doc.json");
            output.Write(
                "xml2doc.json",
                JsonSerializer.Serialize(new CliConfig
                {
                    Xml = SampleXml,
                    Out = docsPath,
                    Diff = true,
                    Report = reportPath
                }));

            var exitCode = Program.Main(new[] { "--config", configPath });

            exitCode.ShouldBe(3);
            Directory.Exists(docsPath).ShouldBeFalse();
            using var report = JsonDocument.Parse(
                File.ReadAllText(reportPath));
            report.RootElement.GetProperty("diffRequested")
                .GetBoolean().ShouldBeTrue();
        }

        [Fact]
        public void Main_RepeatedGenerationReportsAllUnchangedFilesAsSkipped()
        {
            using var output = TemporaryOutput.Create();
            var reportPath = output.FullPath("report.json");
            var arguments = new[]
            {
                "--xml", SampleXml,
                "--out", output.Path,
                "--parallel", "4"
            };

            Program.Main(arguments).ShouldBe(0);
            Program.Main(arguments.Concat(new[]
            {
                "--report", reportPath
            }).ToArray()).ShouldBe(0);

            using var report = JsonDocument.Parse(
                File.ReadAllText(reportPath));
            var plannedFiles = report.RootElement
                .GetProperty("plannedFiles")
                .EnumerateArray()
                .Select(item => item.GetString())
                .ToArray();
            var skippedFiles = report.RootElement
                .GetProperty("skippedFiles")
                .EnumerateArray()
                .Select(item => item.GetString())
                .ToArray();

            skippedFiles.ShouldBe(plannedFiles);
            report.RootElement.GetProperty("writtenFiles")
                .GetArrayLength().ShouldBe(0);
            report.RootElement.GetProperty("prunedFiles")
                .GetArrayLength().ShouldBe(0);
        }

        [Fact]
        public void Main_WhenLineEndingsValueIsInvalid_ReturnsValidationFailure()
        {
            using var output = TemporaryOutput.Create();

            var exitCode = Program.Main(new[]
            {
                "--xml", SampleXml,
                "--out", output.Path,
                "--line-endings", "invalid"
            });

            exitCode.ShouldBe(1);
            Directory.Exists(output.Path).ShouldBeFalse();
        }

        [Fact]
        public void Main_WhenCrLfSelected_WritesCrLfMarkdown()
        {
            using var output = TemporaryOutput.Create();

            var exitCode = Program.Main(new[]
            {
                "--xml", SampleXml,
                "--out", output.Path,
                "--line-endings", "crlf"
            });

            exitCode.ShouldBe(0);
            var markdown = Directory
                .GetFiles(output.Path, "*.md")
                .Select(File.ReadAllText)
                .First();
            AssertUsesOnly(markdown, "\r\n");
        }

        [Fact]
        public void Main_WhenCliOverridesConfigLineEndings_UsesCliValue()
        {
            using var output = TemporaryOutput.Create();
            var configPath = output.FullPath("xml2doc.json");
            output.Write(
                "xml2doc.json",
                JsonSerializer.Serialize(new CliConfig
                {
                    Xml = SampleXml,
                    Out = output.Path,
                    LineEndings = "crlf"
                }));

            var exitCode = Program.Main(new[]
            {
                "--config", configPath,
                "--line-endings", "lf"
            });

            exitCode.ShouldBe(0);
            var markdown = Directory
                .GetFiles(output.Path, "*.md")
                .Select(File.ReadAllText)
                .First();
            markdown.ShouldNotContain("\r");
        }

        [Fact]
        public void Main_WithExternalDocs_UsesExternalFallbackForUnknownCrefs()
        {
            using var output = TemporaryOutput.Create();
            var xmlPath = WriteExternalLinkFixture(output);
            var docsPath = output.FullPath("docs");

            var exitCode = Program.Main(new[]
            {
                "--xml", xmlPath,
                "--out", docsPath,
                "--external-docs", "https://docs.example/api"
            });

            exitCode.ShouldBe(0);
            File.ReadAllText(Path.Join(docsPath, "Temp.Consumer.md"))
                .ShouldContain(
                    "[String](https://docs.example/api/System.String)");
        }

        [Fact]
        public void Main_WithoutExternalDocs_PreservesInternalLinkBehavior()
        {
            using var output = TemporaryOutput.Create();
            var xmlPath = WriteExternalLinkFixture(output);
            var docsPath = output.FullPath("docs");

            var exitCode = Program.Main(new[]
            {
                "--xml", xmlPath,
                "--out", docsPath
            });

            exitCode.ShouldBe(0);
            File.ReadAllText(Path.Join(docsPath, "Temp.Consumer.md"))
                .ShouldContain("[String](System.String.md)");
        }

        [Fact]
        public void Main_WithConfiguredExternalDocs_UsesExternalFallback()
        {
            using var output = TemporaryOutput.Create();
            var xmlPath = WriteExternalLinkFixture(output);
            var docsPath = output.FullPath("docs");
            var configPath = output.FullPath("xml2doc.json");
            output.Write(
                "xml2doc.json",
                JsonSerializer.Serialize(new CliConfig
                {
                    Xml = xmlPath,
                    Out = docsPath,
                    ExternalDocs = "https://config.example/api"
                }));

            var exitCode = Program.Main(new[] { "--config", configPath });

            exitCode.ShouldBe(0);
            File.ReadAllText(Path.Join(docsPath, "Temp.Consumer.md"))
                .ShouldContain(
                    "[String](https://config.example/api/System.String)");
        }

        [Fact]
        public void Main_WhenCliOverridesConfiguredExternalDocs_UsesCliBaseUrl()
        {
            using var output = TemporaryOutput.Create();
            var xmlPath = WriteExternalLinkFixture(output);
            var docsPath = output.FullPath("docs");
            var configPath = output.FullPath("xml2doc.json");
            output.Write(
                "xml2doc.json",
                JsonSerializer.Serialize(new CliConfig
                {
                    Xml = xmlPath,
                    Out = docsPath,
                    ExternalDocs = "https://config.example/api"
                }));

            var exitCode = Program.Main(new[]
            {
                "--config", configPath,
                "--external-docs", "https://cli.example/api"
            });

            exitCode.ShouldBe(0);
            var markdown = File.ReadAllText(
                Path.Join(docsPath, "Temp.Consumer.md"));
            markdown.ShouldContain(
                "[String](https://cli.example/api/System.String)");
            markdown.ShouldNotContain("https://config.example");
        }

        [Fact]
        public void Main_WithDiagnosticWarning_WritesStableWarningAndSucceeds()
        {
            using var output = TemporaryOutput.Create();
            var xmlPath = WriteExternalLinkFixture(output);
            var docsPath = output.FullPath("docs");

            var result = CaptureStandardError(() => Program.Main(new[]
            {
                "--xml", xmlPath,
                "--out", docsPath
            }));

            result.ExitCode.ShouldBe(0);
            result.StandardError.ShouldContain(
                "xml2doc warning XML2DOC001:");
            result.StandardError.ShouldContain("T:System.String");
        }

        [Fact]
        public void Main_WithDiagnosticError_WritesStableErrorAndFails()
        {
            using var output = TemporaryOutput.Create();
            output.Write("malformed.xml", "<doc><members></doc>");
            var xmlPath = output.FullPath("malformed.xml");

            var result = CaptureStandardError(() => Program.Main(new[]
            {
                "--xml", xmlPath,
                "--out", output.FullPath("docs")
            }));

            result.ExitCode.ShouldBe(2);
            result.StandardError.ShouldContain(
                "xml2doc error XML2DOC003:");
            result.StandardError.ShouldContain(xmlPath + "(");
            result.StandardError.ShouldNotContain(
                "System.Xml.XmlException");
        }

        private static (int ExitCode, string StandardError)
            CaptureStandardError(Func<int> action)
        {
            var original = Console.Error;
            using var writer = new StringWriter();
            Console.SetError(writer);

            try
            {
                var exitCode = action();
                return (exitCode, writer.ToString());
            }
            finally
            {
                Console.SetError(original);
            }
        }

        private static string WriteExternalLinkFixture(TemporaryOutput output)
        {
            const string relativePath = "external-links.xml";
            output.Write(relativePath, """
                <doc>
                  <members>
                    <member name="T:Temp.Consumer">
                      <summary>Uses <see cref="T:System.String"/>.</summary>
                    </member>
                  </members>
                </doc>
                """);
            return output.FullPath(relativePath);
        }

        private static void AssertUsesOnly(
            string content,
            string expectedLineEnding)
        {
            content.ShouldContain(expectedLineEnding);
            var withoutExpected =
                content.Replace(expectedLineEnding, string.Empty);
            withoutExpected.ShouldNotContain("\r");
            withoutExpected.ShouldNotContain("\n");
        }

        private sealed class TemporaryOutput : IDisposable
        {
            private TemporaryOutput(string path)
            {
                Path = path;
            }

            public string Path { get; }

            public static TemporaryOutput Create() =>
                new TemporaryOutput(
                    System.IO.Path.GetTempPath() +
                    "Xml2Doc.Tests" +
                    System.IO.Path.DirectorySeparatorChar +
                    Guid.NewGuid().ToString("N"));

            public string FullPath(string relativePath) =>
                Path + System.IO.Path.DirectorySeparatorChar + relativePath;

            public void Write(string relativePath, string content)
            {
                Directory.CreateDirectory(Path);
                File.WriteAllText(FullPath(relativePath), content);
            }

            public bool Exists(string relativePath) =>
                File.Exists(FullPath(relativePath));

            public void Dispose()
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
        }
    }
}
