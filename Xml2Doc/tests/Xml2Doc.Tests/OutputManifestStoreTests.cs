using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xml2Doc.Core.OutputLifecycle;
using Xunit;

namespace Xml2Doc.Tests
{
    public class OutputManifestStoreTests
    {
        [Fact]
        public void Load_WhenManifestDoesNotExist_ReturnsNullWithoutCreatingDirectories()
        {
            using var output = TemporaryOutput.Create();
            var location = output.CreateLocation("project");

            var manifest = OutputManifestStore.Load(location);

            manifest.ShouldBeNull();
            Directory.Exists(output.Path).ShouldBeFalse();
        }

        [Fact]
        public void Save_CreatesMetadataDirectoryAndManifestWithoutUtf8Bom()
        {
            using var output = TemporaryOutput.Create();
            var location = output.CreateLocation("project");

            OutputManifestStore.Save(
                location,
                new[] { "Widget.md", "index.md" });

            File.Exists(location.ManifestPath).ShouldBeTrue();
            File.ReadAllBytes(location.ManifestPath)
                .Take(3)
                .ShouldNotBe(new byte[] { 0xEF, 0xBB, 0xBF });

            var manifest = OutputManifestStore.Load(location);
            manifest.ShouldNotBeNull();
            manifest.Files.ShouldBe(new[] { "Widget.md", "index.md" });
        }

        [Fact]
        public void Save_WhenManifestExists_AtomicallyReplacesItsContents()
        {
            using var output = TemporaryOutput.Create();
            var location = output.CreateLocation("project");
            OutputManifestStore.Save(
                location,
                new[] { "Old.md" });

            OutputManifestStore.Save(
                location,
                new[] { "New.md", "index.md" });

            var manifest = OutputManifestStore.Load(location);
            manifest.ShouldNotBeNull();
            manifest.Files.ShouldBe(new[] { "New.md", "index.md" });
            GetTemporaryFiles(location).ShouldBeEmpty();
        }

        [Fact]
        public void Save_WhenNewOwnershipIsInvalid_PreservesPreviousManifest()
        {
            using var output = TemporaryOutput.Create();
            var location = output.CreateLocation("project");
            OutputManifestStore.Save(
                location,
                new[] { "Owned.md" });
            var previousBytes = File.ReadAllBytes(location.ManifestPath);

            Should.Throw<ArgumentException>(() =>
                OutputManifestStore.Save(
                    location,
                    new[] { Path.Combine("..", "outside.md") }));

            File.ReadAllBytes(location.ManifestPath)
                .ShouldBe(previousBytes);
            GetTemporaryFiles(location).ShouldBeEmpty();
        }

        [Fact]
        public void Save_WhenPublicationFails_RemovesTemporaryManifest()
        {
            using var output = TemporaryOutput.Create();
            var location = output.CreateLocation("project");
            Directory.CreateDirectory(location.ManifestPath);

            Should.Throw<IOException>(() =>
                OutputManifestStore.Save(
                    location,
                    new[] { "Owned.md" }));

            Directory.Exists(location.ManifestPath).ShouldBeTrue();
            GetTemporaryFiles(location).ShouldBeEmpty();
        }

        [Fact]
        public void Load_WhenManifestIsMalformed_ThrowsInvalidDataException()
        {
            using var output = TemporaryOutput.Create();
            var location = output.CreateLocation("project");
            Directory.CreateDirectory(
                Path.GetDirectoryName(location.ManifestPath)!);
            File.WriteAllText(
                location.ManifestPath,
                "{ not-json }",
                new UTF8Encoding(false));

            Should.Throw<InvalidDataException>(() =>
                OutputManifestStore.Load(location));
        }

        private static string[] GetTemporaryFiles(
            OutputManifestLocation location)
        {
            var directory = Path.GetDirectoryName(location.ManifestPath)!;

            return Directory.Exists(directory)
                ? Directory.GetFiles(directory, "*.tmp")
                : Array.Empty<string>();
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
                    System.IO.Path.Combine(
                        System.IO.Path.GetTempPath(),
                        "Xml2Doc.Tests",
                        Guid.NewGuid().ToString("N")));

            public OutputManifestLocation CreateLocation(string identity) =>
                OutputManifestLocation.Create(Path, identity);

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
