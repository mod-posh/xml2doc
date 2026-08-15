using Shouldly;
using System;
using System.IO;
using Xml2Doc.Core.OutputLifecycle;
using Xunit;

namespace Xml2Doc.Tests
{
    public class OutputManifestSerializerTests
    {
        [Fact]
        public void Serialize_ProducesDeterministicJsonWithNormalizedOrdinallySortedFiles()
        {
            var location = CreateLocation("sample-project");
            var files = new[]
            {
                "zeta.md",
                "folder" + Path.DirectorySeparatorChar +
                ".." + Path.DirectorySeparatorChar + "beta.md",
                "Alpha.md"
            };

            var json = OutputManifestSerializer.Serialize(location, files);

            json.ShouldBe(
                "{\n" +
                "  \"schemaVersion\": 2,\n" +
                "  \"identity\": \"sample-project\",\n" +
                "  \"outputRoot\": \".\",\n" +
                "  \"files\": [\n" +
                "    \"Alpha.md\",\n" +
                "    \"beta.md\",\n" +
                "    \"zeta.md\"\n" +
                "  ]\n" +
                "}");
        }

        [Fact]
        public void Serialize_WithSameInputs_ProducesSameJson()
        {
            var location = CreateLocation("project");
            var files = new[] { "Widget.md", "index.md" };

            var first = OutputManifestSerializer.Serialize(location, files);
            var second = OutputManifestSerializer.Serialize(location, files);

            first.ShouldBe(second);
        }

        [Fact]
        public void Serialize_UsesPortableForwardSlashesForNestedFiles()
        {
            var location = CreateLocation("project");
            var nested = "api" + Path.DirectorySeparatorChar + "Widget.md";

            var json = OutputManifestSerializer.Serialize(location, new[] { nested });

            json.ShouldContain("\"api/Widget.md\"");
        }

        [Fact]
        public void Serialize_SortsAfterConvertingToPortableSeparators()
        {
            var location = CreateLocation("project");
            var nested = "api" + Path.DirectorySeparatorChar + "Widget.md";

            var json = OutputManifestSerializer.Serialize(
                location,
                new[] { "api0.md", nested });

            json.IndexOf("\"api/Widget.md\"", StringComparison.Ordinal)
                .ShouldBeLessThan(
                    json.IndexOf("\"api0.md\"", StringComparison.Ordinal));
        }

        [Fact]
        public void Serialize_WhenOwnedPathIsUnsafe_ThrowsArgumentException()
        {
            var location = CreateLocation("project");

            Should.Throw<ArgumentException>(() =>
                OutputManifestSerializer.Serialize(
                    location,
                    new[]
                    {
                        ".." + Path.DirectorySeparatorChar + "outside.md"
                    }));
        }

        [Fact]
        public void Deserialize_RoundTripsValidatedManifest()
        {
            var location = CreateLocation("project");
            var json = OutputManifestSerializer.Serialize(
                location,
                new[] { "Widget.md", "index.md" });

            var manifest = OutputManifestSerializer.Deserialize(json, location);

            manifest.SchemaVersion.ShouldBe(OutputManifest.CurrentSchemaVersion);
            manifest.Identity.ShouldBe("project");
            manifest.OutputRoot.ShouldBe(OutputManifest.PortableOutputRoot);
            manifest.Files.ShouldBe(new[] { "Widget.md", "index.md" });
        }

        [Fact]
        public void Deserialize_WhenJsonIsMalformed_ThrowsInvalidDataException()
        {
            Should.Throw<InvalidDataException>(() =>
                OutputManifestSerializer.Deserialize(
                    "{ not-json }",
                    CreateLocation("project")));
        }

        [Fact]
        public void Deserialize_WhenManifestIdentityDiffersByCase_ThrowsInvalidDataException()
        {
            var storedLocation = CreateLocation("project");
            var requestedLocation = OutputManifestLocation.Create(
                storedLocation.OutputRoot,
                "Project");
            var json = OutputManifestSerializer.Serialize(
                storedLocation,
                new[] { "Widget.md" });

            Should.Throw<InvalidDataException>(() =>
                OutputManifestSerializer.Deserialize(json, requestedLocation));
        }

        [Fact]
        public void Deserialize_WhenManifestIsMovedToAnotherOutputRoot_LoadsPortableOwnership()
        {
            var storedLocation = CreateLocation("project");
            var requestedLocation = CreateLocation("project");
            var json = OutputManifestSerializer.Serialize(
                storedLocation,
                new[] { "Widget.md" });

            var manifest = OutputManifestSerializer.Deserialize(json, requestedLocation);

            manifest.Files.ShouldBe(new[] { "Widget.md" });
            manifest.OutputRoot.ShouldBe(OutputManifest.PortableOutputRoot);
        }

        [Fact]
        public void Deserialize_WhenSchemaVersionIsUnsupported_ThrowsInvalidDataException()
        {
            var location = CreateLocation("project");
            var json = OutputManifestSerializer.Serialize(
                    location,
                    new[] { "Widget.md" })
                .Replace(
                    "\"schemaVersion\": 2",
                    "\"schemaVersion\": 3");

            Should.Throw<InvalidDataException>(() =>
                OutputManifestSerializer.Deserialize(json, location));
        }

        [Fact]
        public void Deserialize_WhenOwnedPathEscapesOutputRoot_ThrowsInvalidDataException()
        {
            var location = CreateLocation("project");
            var json = OutputManifestSerializer.Serialize(
                    location,
                    new[] { "Widget.md" })
                .Replace(
                    "\"Widget.md\"",
                    "\"../outside.md\"");

            Should.Throw<InvalidDataException>(() =>
                OutputManifestSerializer.Deserialize(json, location));
        }

        [Fact]
        public void Deserialize_WhenLegacyManifestMovesAcrossWindowsAndUnixRoots_MigratesSafely()
        {
            var location = CreateLocation("project");
            var json =
                "{" +
                "\"schemaVersion\":1," +
                "\"identity\":\"project\"," +
                "\"outputRoot\":\"D:\\\\old-checkout\\\\docs\"," +
                "\"files\":[\"nested\\\\Widget.md\"]" +
                "}";

            var manifest = OutputManifestSerializer.Deserialize(json, location);

            manifest.SchemaVersion.ShouldBe(OutputManifest.CurrentSchemaVersion);
            manifest.OutputRoot.ShouldBe(OutputManifest.PortableOutputRoot);
            manifest.Files.ShouldBe(new[]
            {
                "nested" + Path.DirectorySeparatorChar + "Widget.md"
            });
        }

        [Fact]
        public void Deserialize_WhenPortableRootMarkerIsInvalid_ThrowsInvalidDataException()
        {
            var location = CreateLocation("project");
            var json = OutputManifestSerializer.Serialize(
                    location,
                    new[] { "Widget.md" })
                .Replace("\"outputRoot\": \".\"", "\"outputRoot\": \"../outside\"");

            Should.Throw<InvalidDataException>(() =>
                OutputManifestSerializer.Deserialize(json, location));
        }

        [Fact]
        public void Deserialize_WhenFileListIsMissing_ThrowsInvalidDataException()
        {
            var location = CreateLocation("project");
            var json =
                "{" +
                "\"schemaVersion\":1," +
                "\"identity\":\"project\"," +
                $"\"outputRoot\":{SerializeJsonString(location.OutputRoot)}" +
                "}";

            Should.Throw<InvalidDataException>(() =>
                OutputManifestSerializer.Deserialize(json, location));
        }

        [Theory]
        [InlineData(null)]
        public void Deserialize_WhenJsonIsNull_ThrowsArgumentNullException(
            string? json)
        {
            Should.Throw<ArgumentNullException>(() =>
                OutputManifestSerializer.Deserialize(
                    json!,
                    CreateLocation("project")));
        }

        private static OutputManifestLocation CreateLocation(string identity) =>
            OutputManifestLocation.Create(
                Path.GetTempPath() +
                "Xml2Doc.Tests" + Path.DirectorySeparatorChar +
                Guid.NewGuid().ToString("N"),
                identity);

        private static string SerializeJsonString(string value) =>
            System.Text.Json.JsonSerializer.Serialize(value);
    }
}
