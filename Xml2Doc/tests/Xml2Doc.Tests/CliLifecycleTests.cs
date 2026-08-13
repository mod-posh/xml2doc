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
                "--manifest-id", "sample-project"
            });

            exitCode.ShouldBe(0);
            output.Exists("stale.md").ShouldBeFalse();
            output.Exists("hand-authored.md").ShouldBeTrue();
            OutputManifestStore.Load(location)!.Files.ShouldNotBeEmpty();
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
