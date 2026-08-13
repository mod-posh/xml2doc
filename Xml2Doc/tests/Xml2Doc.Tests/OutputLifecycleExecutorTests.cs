using Shouldly;
using System;
using System.IO;
using System.Text;
using Xml2Doc.Core.OutputLifecycle;
using Xunit;

namespace Xml2Doc.Tests
{
    public class OutputLifecycleExecutorTests
    {
        [Fact]
        public void ExecuteAfterSuccessfulGeneration_DeletesOnlyStaleOwnedFiles()
        {
            using var output = TemporaryOutput.Create();
            var location = output.CreateLocation("project");
            output.Write("Current.md");
            output.Write("Stale.md");
            output.Write("hand-authored.md");
            OutputManifestStore.Save(
                location,
                new[] { "Current.md", "Stale.md" });

            var plan = OutputLifecycleExecutor.ExecuteAfterSuccessfulGeneration(
                location,
                new[] { "Current.md" });

            plan.FilesToDelete.ShouldBe(new[] { "Stale.md" });
            output.Exists("Current.md").ShouldBeTrue();
            output.Exists("Stale.md").ShouldBeFalse();
            output.Exists("hand-authored.md").ShouldBeTrue();
            OutputManifestStore.Load(location)!.Files
                .ShouldBe(new[] { "Current.md" });
        }

        [Fact]
        public void ExecuteAfterSuccessfulGeneration_WhenStaleFileIsMissing_UpdatesManifest()
        {
            using var output = TemporaryOutput.Create();
            var location = output.CreateLocation("project");
            output.Write("Current.md");
            OutputManifestStore.Save(
                location,
                new[] { "Current.md", "Missing.md" });

            OutputLifecycleExecutor.ExecuteAfterSuccessfulGeneration(
                location,
                new[] { "Current.md" });

            OutputManifestStore.Load(location)!.Files
                .ShouldBe(new[] { "Current.md" });
        }

        [Fact]
        public void ExecuteAfterSuccessfulGeneration_WithDifferentIdentity_PreservesOtherOwnership()
        {
            using var output = TemporaryOutput.Create();
            var first = output.CreateLocation("project-one");
            var second = output.CreateLocation("project-two");
            output.Write("One.md");
            output.Write("Two.md");
            OutputManifestStore.Save(first, new[] { "One.md" });
            OutputManifestStore.Save(second, new[] { "Two.md" });

            OutputLifecycleExecutor.ExecuteAfterSuccessfulGeneration(
                first,
                Array.Empty<string>());

            output.Exists("One.md").ShouldBeFalse();
            output.Exists("Two.md").ShouldBeTrue();
            OutputManifestStore.Load(second)!.Files
                .ShouldBe(new[] { "Two.md" });
        }

        [Fact]
        public void ExecuteAfterSuccessfulGeneration_InDryRun_DoesNotMutateFilesOrManifest()
        {
            using var output = TemporaryOutput.Create();
            var location = output.CreateLocation("project");
            output.Write("Stale.md");
            OutputManifestStore.Save(location, new[] { "Stale.md" });
            var previousManifest = File.ReadAllBytes(location.ManifestPath);

            var plan = OutputLifecycleExecutor.ExecuteAfterSuccessfulGeneration(
                location,
                Array.Empty<string>(),
                dryRun: true);

            plan.FilesToDelete.ShouldBe(new[] { "Stale.md" });
            output.Exists("Stale.md").ShouldBeTrue();
            File.ReadAllBytes(location.ManifestPath)
                .ShouldBe(previousManifest);
            output.TransactionDirectories.ShouldBeEmpty();
        }

        [Fact]
        public void ExecuteAfterSuccessfulGeneration_WhenGeneratedFileIsMissing_PreservesStaleFilesAndManifest()
        {
            using var output = TemporaryOutput.Create();
            var location = output.CreateLocation("project");
            output.Write("Stale.md");
            OutputManifestStore.Save(location, new[] { "Stale.md" });
            var previousManifest = File.ReadAllBytes(location.ManifestPath);

            Should.Throw<InvalidOperationException>(() =>
                OutputLifecycleExecutor.ExecuteAfterSuccessfulGeneration(
                    location,
                    new[] { "Missing.md" }));

            output.Exists("Stale.md").ShouldBeTrue();
            File.ReadAllBytes(location.ManifestPath)
                .ShouldBe(previousManifest);
            output.TransactionDirectories.ShouldBeEmpty();
        }

        [Fact]
        public void ExecuteAfterSuccessfulGeneration_WithoutPreviousManifest_RecordsCurrentOwnership()
        {
            using var output = TemporaryOutput.Create();
            var location = output.CreateLocation("project");
            output.Write("Widget.md");

            var plan = OutputLifecycleExecutor.ExecuteAfterSuccessfulGeneration(
                location,
                new[] { "Widget.md" });

            plan.FilesToDelete.ShouldBeEmpty();
            OutputManifestStore.Load(location)!.Files
                .ShouldBe(new[] { "Widget.md" });
        }

        private sealed class TemporaryOutput : IDisposable
        {
            private TemporaryOutput(string path)
            {
                Path = path;
            }

            public string Path { get; }

            public string[] TransactionDirectories
            {
                get
                {
                    var directory =
                        Path + System.IO.Path.DirectorySeparatorChar +
                        ".xml2doc" + System.IO.Path.DirectorySeparatorChar +
                        "transactions";

                    return Directory.Exists(directory)
                        ? Directory.GetDirectories(directory)
                        : Array.Empty<string>();
                }
            }

            public static TemporaryOutput Create() =>
                new TemporaryOutput(
                    System.IO.Path.GetTempPath() +
                    "Xml2Doc.Tests" +
                    System.IO.Path.DirectorySeparatorChar +
                    Guid.NewGuid().ToString("N"));

            public OutputManifestLocation CreateLocation(string identity) =>
                OutputManifestLocation.Create(Path, identity);

            public void Write(string relativePath)
            {
                Directory.CreateDirectory(Path);
                File.WriteAllText(
                    Path + System.IO.Path.DirectorySeparatorChar +
                    relativePath,
                    "content",
                    new UTF8Encoding(false));
            }

            public bool Exists(string relativePath) =>
                File.Exists(
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
