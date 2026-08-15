using Shouldly;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xml2Doc.Core.OutputLifecycle;
using Xunit;

namespace Xml2Doc.Tests
{
    public class OutputManifestPlannerTests
    {
        [Fact]
        public void CreatePlan_WithoutPreviousManifest_WritesAllPlannedOutputsAndDeletesNothing()
        {
            var outputRoot = CreateOutputRoot();
            var plannedOutputs = new[]
            {
                "Widget.md",
                "index.md"
            };

            var plan = OutputManifestPlanner.CreatePlan(
                outputRoot,
                plannedOutputs,
                previousManifest: null);

            plan.FilesToWrite.ShouldBe(new[]
            {
                "Widget.md",
                "index.md"
            });
            plan.FilesToDelete.ShouldBeEmpty();
        }

        [Fact]
        public void CreatePlan_WhenPreviouslyOwnedFileIsAbsentFromCurrentPlan_MarksItForDeletion()
        {
            var outputRoot = CreateOutputRoot();
            var previousManifest = CreateManifest(
                outputRoot,
                "Current.md",
                "Stale.md");

            var plan = OutputManifestPlanner.CreatePlan(
                outputRoot,
                new[] { "Current.md" },
                previousManifest);

            plan.FilesToDelete.ShouldBe(new[] { "Stale.md" });
        }

        [Fact]
        public void CreatePlan_WhenPreviouslyOwnedFileRemains_WritesItAndDoesNotDeleteIt()
        {
            var outputRoot = CreateOutputRoot();
            var previousManifest = CreateManifest(
                outputRoot,
                "Retained.md",
                "Stale.md");

            var plan = OutputManifestPlanner.CreatePlan(
                outputRoot,
                new[] { "Retained.md" },
                previousManifest);

            plan.FilesToWrite.ShouldContain("Retained.md");
            plan.FilesToDelete.ShouldNotContain("Retained.md");
        }

        [Fact]
        public void CreatePlan_SortsWritesAndDeletionsUsingOrdinalComparison()
        {
            var outputRoot = CreateOutputRoot();
            var previousManifest = CreateManifest(
                outputRoot,
                "zeta-old.md",
                "beta-old.md",
                "Alpha-old.md");

            var plan = OutputManifestPlanner.CreatePlan(
                outputRoot,
                new[]
                {
                    "zeta.md",
                    "beta.md",
                    "Alpha.md"
                },
                previousManifest);

            plan.FilesToWrite.ShouldBe(new[]
            {
                "Alpha.md",
                "beta.md",
                "zeta.md"
            });

            plan.FilesToDelete.ShouldBe(new[]
            {
                "Alpha-old.md",
                "beta-old.md",
                "zeta-old.md"
            });
        }

        [Fact]
        public void CreatePlan_WhenPortableManifestMovesToAnotherOutputRoot_PreservesOwnership()
        {
            var outputRoot = CreateOutputRoot();
            var otherOutputRoot = CreateOutputRoot();
            var previousManifest = CreateManifest(otherOutputRoot, "Owned.md");

            var plan = OutputManifestPlanner.CreatePlan(
                outputRoot,
                new[] { "Owned.md" },
                previousManifest);

            plan.FilesToWrite.ShouldBe(new[] { "Owned.md" });
            plan.FilesToDelete.ShouldBeEmpty();
        }

        [Fact]
        public void CreatePlan_WhenPreviousManifestContainsRootedEntry_ThrowsInvalidDataException()
        {
            var outputRoot = CreateOutputRoot();
            var rootedEntry =
                Path.GetTempPath() +
                Path.GetRandomFileName() + ".md";
            var previousManifest = CreateManifest(outputRoot, rootedEntry);

            Should.Throw<InvalidDataException>(() =>
                OutputManifestPlanner.CreatePlan(
                    outputRoot,
                    Array.Empty<string>(),
                    previousManifest));
        }

        [Fact]
        public void CreatePlan_WhenPreviousManifestEntryEscapesOutputRoot_ThrowsInvalidDataException()
        {
            var outputRoot = CreateOutputRoot();
            var escapingEntry =
                ".." + Path.DirectorySeparatorChar + "outside.md";
            var previousManifest = CreateManifest(outputRoot, escapingEntry);

            Should.Throw<InvalidDataException>(() =>
                OutputManifestPlanner.CreatePlan(
                    outputRoot,
                    Array.Empty<string>(),
                    previousManifest));
        }

        [Fact]
        public void CreatePlan_WhenPreviousManifestContainsOrdinalDuplicateEntries_ThrowsInvalidDataException()
        {
            var outputRoot = CreateOutputRoot();
            var previousManifest = CreateManifest(
                outputRoot,
                "Duplicate.md",
                "Duplicate.md");

            Should.Throw<InvalidDataException>(() =>
                OutputManifestPlanner.CreatePlan(
                    outputRoot,
                    Array.Empty<string>(),
                    previousManifest));
        }

        [Fact]
        public void CreatePlan_WhenPreviousManifestSchemaVersionIsUnsupported_ThrowsInvalidDataException()
        {
            var outputRoot = CreateOutputRoot();
            var previousManifest = new OutputManifest(
                OutputManifest.CurrentSchemaVersion + 1,
                "test-invocation",
                outputRoot,
                new[] { "Owned.md" });

            Should.Throw<InvalidDataException>(() =>
                OutputManifestPlanner.CreatePlan(
                    outputRoot,
                    new[] { "Owned.md" },
                    previousManifest));
        }

        [Fact]
        public void CreatePlan_WhenCurrentPlanContainsRootedEntry_ThrowsArgumentException()
        {
            var outputRoot = CreateOutputRoot();
            var rootedEntry =
                Path.GetTempPath() +
                Path.GetRandomFileName() + ".md";

            Should.Throw<ArgumentException>(() =>
                OutputManifestPlanner.CreatePlan(
                    outputRoot,
                    new[] { rootedEntry },
                    previousManifest: null));
        }

        [Fact]
        public void CreatePlan_WhenCurrentPlanEntryEscapesOutputRoot_ThrowsArgumentException()
        {
            var outputRoot = CreateOutputRoot();
            var escapingEntry =
                ".." + Path.DirectorySeparatorChar + "outside.md";

            Should.Throw<ArgumentException>(() =>
                OutputManifestPlanner.CreatePlan(
                    outputRoot,
                    new[] { escapingEntry },
                    previousManifest: null));
        }

        [Fact]
        public void CreatePlan_WhenCurrentPlanContainsOrdinalDuplicateEntries_ThrowsArgumentException()
        {
            var outputRoot = CreateOutputRoot();

            Should.Throw<ArgumentException>(() =>
                OutputManifestPlanner.CreatePlan(
                    outputRoot,
                    new[]
                    {
                        "Duplicate.md",
                        "Duplicate.md"
                    },
                    previousManifest: null));
        }

        [Fact]
        public void CreatePlan_WhenCurrentOutputRootHasTrailingSeparator_MatchesManifestRootWithoutTrailingSeparator()
        {
            var outputRoot = CreateOutputRoot();
            var currentOutputRoot = EnsureTrailingDirectorySeparator(outputRoot);
            var previousManifest = CreateManifest(outputRoot, "Owned.md");

            var plan = OutputManifestPlanner.CreatePlan(
                currentOutputRoot,
                new[] { "Owned.md" },
                previousManifest);

            plan.FilesToWrite.ShouldBe(new[] { "Owned.md" });
            plan.FilesToDelete.ShouldBeEmpty();
        }

        [Fact]
        public void CreatePlan_WhenLegacyManifestOutputRootHasTrailingSeparator_MigratesOwnership()
        {
            var outputRoot = CreateOutputRoot();
            var previousManifest = new OutputManifest(
                1,
                "test-invocation",
                EnsureTrailingDirectorySeparator(outputRoot),
                new[] { "Owned.md" });

            var plan = OutputManifestPlanner.CreatePlan(
                outputRoot,
                new[] { "Owned.md" },
                previousManifest);

            plan.FilesToWrite.ShouldBe(new[] { "Owned.md" });
            plan.FilesToDelete.ShouldBeEmpty();
        }

        [Fact]
        public void CreatePlan_WhenCurrentOutputContainsInternalTraversal_NormalizesRelativePath()
        {
            var outputRoot = CreateOutputRoot();
            var unnormalizedPath =
                "folder" + Path.DirectorySeparatorChar +
                ".." + Path.DirectorySeparatorChar + "File.md";

            var plan = OutputManifestPlanner.CreatePlan(
                outputRoot,
                new[] { unnormalizedPath },
                previousManifest: null);

            plan.FilesToWrite.ShouldBe(new[] { "File.md" });
            plan.FilesToDelete.ShouldBeEmpty();
        }

        [Fact]
        public void CreatePlan_WhenManifestEntryContainsInternalTraversal_NormalizesOwnedPath()
        {
            var outputRoot = CreateOutputRoot();
            var unnormalizedPath =
                "folder" + Path.DirectorySeparatorChar +
                ".." + Path.DirectorySeparatorChar + "File.md";
            var previousManifest = CreateManifest(
                outputRoot,
                unnormalizedPath);

            var plan = OutputManifestPlanner.CreatePlan(
                outputRoot,
                Array.Empty<string>(),
                previousManifest);

            plan.FilesToWrite.ShouldBeEmpty();
            plan.FilesToDelete.ShouldBe(new[] { "File.md" });
        }

        [Fact]
        public void CreatePlan_WhenCurrentOutputsCollideAfterNormalization_ThrowsArgumentException()
        {
            var outputRoot = CreateOutputRoot();
            var equivalentPath =
                "folder" + Path.DirectorySeparatorChar +
                ".." + Path.DirectorySeparatorChar + "File.md";

            Should.Throw<ArgumentException>(() =>
                OutputManifestPlanner.CreatePlan(
                    outputRoot,
                    new[]
                    {
                        "File.md",
                        equivalentPath
                    },
                    previousManifest: null));
        }

        [Fact]
        public void CreatePlan_WhenManifestEntriesCollideAfterNormalization_ThrowsInvalidDataException()
        {
            var outputRoot = CreateOutputRoot();
            var equivalentPath =
                "folder" + Path.DirectorySeparatorChar +
                ".." + Path.DirectorySeparatorChar + "File.md";
            var previousManifest = CreateManifest(
                outputRoot,
                "File.md",
                equivalentPath);

            Should.Throw<InvalidDataException>(() =>
                OutputManifestPlanner.CreatePlan(
                    outputRoot,
                    Array.Empty<string>(),
                    previousManifest));
        }

        [Fact]
        public void CreatePlan_WhenCurrentOutputIsNested_PreservesOutputRootRelativePath()
        {
            var outputRoot = CreateOutputRoot();
            var nestedPath =
                "api" + Path.DirectorySeparatorChar +
                "models" + Path.DirectorySeparatorChar + "Widget.md";

            var plan = OutputManifestPlanner.CreatePlan(
                outputRoot,
                new[] { nestedPath },
                previousManifest: null);

            plan.FilesToWrite.ShouldBe(new[] { nestedPath });
            plan.FilesToDelete.ShouldBeEmpty();
        }

        [Fact]
        public void CreatePlan_WhenCurrentAndPreviousPathsDifferOnlyByCase_UsesPlatformPathIdentity()
        {
            var outputRoot = CreateOutputRoot();
            var previousManifest = CreateManifest(
                outputRoot,
                "Widget.md");

            var plan = OutputManifestPlanner.CreatePlan(
                outputRoot,
                new[] { "widget.md" },
                previousManifest);

            plan.FilesToWrite.ShouldBe(new[] { "widget.md" });

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                plan.FilesToDelete.ShouldBeEmpty();
            }
            else
            {
                plan.FilesToDelete.ShouldBe(new[] { "Widget.md" });
            }
        }

        [Fact]
        public void CreatePlan_WhenCurrentOutputsDifferOnlyByCase_UsesPlatformPathIdentityAndOrdinalOrdering()
        {
            var outputRoot = CreateOutputRoot();
            var plannedOutputs = new[]
            {
                "widget.md",
                "Widget.md"
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Should.Throw<ArgumentException>(() =>
                    OutputManifestPlanner.CreatePlan(
                        outputRoot,
                        plannedOutputs,
                        previousManifest: null));
            }
            else
            {
                var plan = OutputManifestPlanner.CreatePlan(
                    outputRoot,
                    plannedOutputs,
                    previousManifest: null);

                plan.FilesToWrite.ShouldBe(new[]
                {
                    "Widget.md",
                    "widget.md"
                });
                plan.FilesToDelete.ShouldBeEmpty();
            }
        }

        [Fact]
        public void CreatePlan_WhenManifestEntriesDifferOnlyByCase_UsesPlatformPathIdentityAndOrdinalOrdering()
        {
            var outputRoot = CreateOutputRoot();
            var previousManifest = CreateManifest(
                outputRoot,
                "widget.md",
                "Widget.md");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Should.Throw<InvalidDataException>(() =>
                    OutputManifestPlanner.CreatePlan(
                        outputRoot,
                        Array.Empty<string>(),
                        previousManifest));
            }
            else
            {
                var plan = OutputManifestPlanner.CreatePlan(
                    outputRoot,
                    Array.Empty<string>(),
                    previousManifest);

                plan.FilesToWrite.ShouldBeEmpty();
                plan.FilesToDelete.ShouldBe(new[]
                {
                    "Widget.md",
                    "widget.md"
                });
            }
        }

        [Fact]
        public void CreatePlan_WhenCurrentOutputRootIsInvalid_ThrowsArgumentException()
        {
            var exception = Should.Throw<ArgumentException>(() =>
                OutputManifestPlanner.CreatePlan(
                    "\0",
                    Array.Empty<string>(),
                    previousManifest: null));

            exception.ParamName.ShouldBe("outputRoot");
            exception.InnerException.ShouldNotBeNull();
        }

        private static string CreateOutputRoot() =>
            Path.GetFullPath(
                Path.GetTempPath() +
                "Xml2Doc.Tests" + Path.DirectorySeparatorChar +
                Path.GetRandomFileName());

        private static string EnsureTrailingDirectorySeparator(string path)
        {
            var fullPath = Path.GetFullPath(path);

            if (fullPath.EndsWith(
                    Path.DirectorySeparatorChar.ToString(),
                    StringComparison.Ordinal) ||
                fullPath.EndsWith(
                    Path.AltDirectorySeparatorChar.ToString(),
                    StringComparison.Ordinal))
            {
                return fullPath;
            }

            return fullPath + Path.DirectorySeparatorChar;
        }

        private static OutputManifest CreateManifest(
            string outputRoot,
            params string[] files) =>
            new(
                OutputManifest.CurrentSchemaVersion,
                "test-invocation",
                OutputManifest.PortableOutputRoot,
                files);
    }
}
