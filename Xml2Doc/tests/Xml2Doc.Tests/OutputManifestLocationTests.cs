using Shouldly;
using System;
using System.IO;
using System.Text;
using Xml2Doc.Core.OutputLifecycle;
using Xunit;

namespace Xml2Doc.Tests
{
    public class OutputManifestLocationTests
    {
        [Fact]
        public void Create_UsesExactUtf8IdentityToCalculateDeterministicManifestPath()
        {
            var outputRoot = CreateOutputRoot();

            var location = OutputManifestLocation.Create(
                outputRoot,
                "sample-project");

            location.Identity.ShouldBe("sample-project");
            location.IdentityHash.ShouldBe(
                "f04e14ea9a6660391a43a451467efd4372021aaf8806180f653481079d33fa16");
            location.ManifestPath.ShouldBe(Path.Combine(
                Path.GetFullPath(outputRoot),
                ".xml2doc",
                "manifests",
                location.IdentityHash + ".json"));
        }

        [Fact]
        public void Create_WhenIdentityContainsPathSyntax_UsesOnlyHashAsFilename()
        {
            var outputRoot = CreateOutputRoot();
            var identity = "../project:docs\\api";

            var location = OutputManifestLocation.Create(outputRoot, identity);

            Path.GetFileName(location.ManifestPath)
                .ShouldBe(location.IdentityHash + ".json");
            Path.GetDirectoryName(location.ManifestPath)
                .ShouldBe(Path.Combine(
                    Path.GetFullPath(outputRoot),
                    ".xml2doc",
                    "manifests"));
        }

        [Fact]
        public void Create_WhenIdentitiesDifferOnlyByCase_PreservesOrdinalIdentity()
        {
            var outputRoot = CreateOutputRoot();

            var lowercase = OutputManifestLocation.Create(outputRoot, "project");
            var uppercase = OutputManifestLocation.Create(outputRoot, "Project");

            lowercase.ShouldNotBe(uppercase);
            lowercase.IdentityHash.ShouldNotBe(uppercase.IdentityHash);
            lowercase.ManifestPath.ShouldNotBe(uppercase.ManifestPath);
        }

        [Fact]
        public void Create_WhenOutputRootsAreEquivalent_ReturnsEqualLocations()
        {
            var outputRoot = CreateOutputRoot();
            var outputRootWithSeparator = outputRoot + Path.DirectorySeparatorChar;

            var first = OutputManifestLocation.Create(outputRoot, "project");
            var second = OutputManifestLocation.Create(
                outputRootWithSeparator,
                "project");

            first.ShouldBe(second);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WhenIdentityIsMissing_ThrowsArgumentException(
            string? identity)
        {
            var exception = Should.Throw<ArgumentException>(() =>
                OutputManifestLocation.Create(CreateOutputRoot(), identity!));

            exception.ParamName.ShouldBe("identity");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WhenOutputRootIsMissing_ThrowsArgumentException(
            string? outputRoot)
        {
            var exception = Should.Throw<ArgumentException>(() =>
                OutputManifestLocation.Create(outputRoot!, "project"));

            exception.ParamName.ShouldBe("outputRoot");
        }

        [Fact]
        public void Create_WhenOutputRootIsInvalid_WrapsPathException()
        {
            var exception = Should.Throw<ArgumentException>(() =>
                OutputManifestLocation.Create("\0", "project"));

            exception.ParamName.ShouldBe("outputRoot");
            exception.InnerException.ShouldNotBeNull();
        }

        [Fact]
        public void Create_WhenIdentityContainsInvalidUnicode_ThrowsArgumentException()
        {
            var invalidIdentity = "project\ud800";

            var exception = Should.Throw<ArgumentException>(() =>
                OutputManifestLocation.Create(
                    CreateOutputRoot(),
                    invalidIdentity));

            exception.ParamName.ShouldBe("identity");
            exception.InnerException.ShouldBeOfType<EncoderFallbackException>();
        }

        private static string CreateOutputRoot() =>
            Path.Combine(
                Path.GetTempPath(),
                "xml2doc-tests",
                Guid.NewGuid().ToString("N"));
    }
}
