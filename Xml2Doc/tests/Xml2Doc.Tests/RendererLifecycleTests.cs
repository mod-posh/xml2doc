using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xml2Doc.Core;
using Xml2Doc.Core.OutputLifecycle;
using Xunit;

namespace Xml2Doc.Tests
{
    public class RendererLifecycleTests
    {
        private static string TestProjectDirectory =>
            Path.GetFullPath(
                AppContext.BaseDirectory +
                ".." + Path.DirectorySeparatorChar +
                ".." + Path.DirectorySeparatorChar +
                "..");

        private static string SampleXml =>
            TestProjectDirectory + Path.DirectorySeparatorChar +
            "Assets" + Path.DirectorySeparatorChar +
            "Xml2Doc.Sample.xml";

        [Fact]
        public void RenderToDirectory_WithPruningDisabled_PreservesStaleFilesAndDoesNotCreateManifest()
        {
            using var output = TemporaryOutput.Create();
            output.Write("Stale.md", "stale");
            var renderer = CreateRenderer(new RendererOptions());

            renderer.RenderToDirectory(output.Path);

            output.Exists("Stale.md").ShouldBeTrue();
            Directory.Exists(
                output.Path + Path.DirectorySeparatorChar +
                ".xml2doc").ShouldBeFalse();
        }

        [Fact]
        public void RenderToDirectory_WithPruningEnabled_DeletesStaleOwnedFileAndUpdatesManifest()
        {
            using var output = TemporaryOutput.Create();
            var location = output.CreateLocation("sample-project");
            output.Write("Stale.md", "stale");
            OutputManifestStore.Save(
                location,
                new[] { "Stale.md" });
            var renderer = CreateRenderer(
                new RendererOptions(
                    PruneStaleFiles: true,
                    ManifestIdentity: "sample-project"));

            renderer.RenderToDirectory(output.Path);

            output.Exists("Stale.md").ShouldBeFalse();
            var expectedFiles = renderer
                .PlanOutputs(output.Path)
                .Select(path => Path.GetFileName(path)!)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            OutputManifestStore.Load(location)!.Files
                .ShouldBe(expectedFiles);
        }

        [Fact]
        public void RenderToDirectory_WithPruningEnabled_PreservesUntrackedFile()
        {
            using var output = TemporaryOutput.Create();
            output.Write("hand-authored.md", "keep");
            var renderer = CreateRenderer(
                new RendererOptions(
                    PruneStaleFiles: true,
                    ManifestIdentity: "sample-project"));

            renderer.RenderToDirectory(output.Path);

            output.Exists("hand-authored.md").ShouldBeTrue();
        }

        [Fact]
        public void RenderToDirectory_WithPruningEnabledAndMissingIdentity_ThrowsBeforeWriting()
        {
            using var output = TemporaryOutput.Create();
            output.Write("sentinel.md", "unchanged");
            var renderer = CreateRenderer(
                new RendererOptions(
                    PruneStaleFiles: true));

            var exception = Should.Throw<ArgumentException>(() =>
                renderer.RenderToDirectory(output.Path));

            exception.ParamName.ShouldBe("identity");
            output.Read("sentinel.md").ShouldBe("unchanged");
            Directory.GetFiles(output.Path)
                .ShouldBe(new[]
                {
                    output.Path + Path.DirectorySeparatorChar +
                    "sentinel.md"
                });
        }

        [Fact]
        public void RenderToDirectory_WithPruningEnabled_RecordsCompleteRendererPlan()
        {
            using var output = TemporaryOutput.Create();
            var options = new RendererOptions(
                EmitNamespaceIndex: true,
                PruneStaleFiles: true,
                ManifestIdentity: "sample-project");
            var renderer = CreateRenderer(options);
            var location = output.CreateLocation("sample-project");

            renderer.RenderToDirectory(output.Path);

            var rootWithSeparator =
                location.OutputRoot + Path.DirectorySeparatorChar;
            var expectedFiles = renderer
                .PlanOutputs(output.Path)
                .Select(path =>
                    Path.GetFullPath(path)
                        .Substring(rootWithSeparator.Length))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            OutputManifestStore.Load(location)!.Files
                .ShouldBe(expectedFiles);
            expectedFiles.ShouldContain("namespaces.md");
            expectedFiles.ShouldContain(path =>
                path.StartsWith(
                    "namespaces" + Path.DirectorySeparatorChar,
                    StringComparison.Ordinal));
        }

        [Fact]
        public void RenderToDirectory_WhenGenerationFails_PreservesStaleFileAndPreviousManifest()
        {
            using var output = TemporaryOutput.Create();
            var location = output.CreateLocation("sample-project");
            output.Write("Stale.md", "stale");
            OutputManifestStore.Save(
                location,
                new[] { "Stale.md" });
            var previousManifest = File.ReadAllBytes(location.ManifestPath);
            var renderer = CreateRenderer(
                new RendererOptions(
                    PruneStaleFiles: true,
                    ManifestIdentity: "sample-project"));
            var blockedOutput = renderer.PlanOutputs(output.Path)[0];
            Directory.CreateDirectory(blockedOutput);

            Should.Throw<UnauthorizedAccessException>(() =>
                renderer.RenderToDirectory(output.Path));

            output.Exists("Stale.md").ShouldBeTrue();
            File.ReadAllBytes(location.ManifestPath)
                .ShouldBe(previousManifest);
        }

        private static MarkdownRenderer CreateRenderer(
            RendererOptions options)
        {
            File.Exists(SampleXml).ShouldBeTrue(
                $"Missing fixture XML at: {SampleXml}");
            var model = Xml2Doc.Core.Models.Xml2Doc.Load(SampleXml);
            return new MarkdownRenderer(model, options);
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

            public OutputManifestLocation CreateLocation(string identity) =>
                OutputManifestLocation.Create(Path, identity);

            public void Write(string relativePath, string content)
            {
                Directory.CreateDirectory(Path);
                File.WriteAllText(
                    Path + System.IO.Path.DirectorySeparatorChar +
                    relativePath,
                    content,
                    new UTF8Encoding(false));
            }

            public bool Exists(string relativePath) =>
                File.Exists(
                    Path + System.IO.Path.DirectorySeparatorChar +
                    relativePath);

            public string Read(string relativePath) =>
                File.ReadAllText(
                    Path + System.IO.Path.DirectorySeparatorChar +
                    relativePath);

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
