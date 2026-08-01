using Shouldly;
using System;
using System.IO;
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
        public void CreatePlan_WhenPreviousManifestOutputRootDoesNotMatchCurrentCanonicalRoot_ThrowsInvalidDataException()
        {
            var outputRoot = CreateOutputRoot();
            var otherOutputRoot = CreateOutputRoot();
            var previousManifest = CreateManifest(otherOutputRoot, "Owned.md");

            Should.Throw<InvalidDataException>(() =>
                OutputManifestPlanner.CreatePlan(
                    outputRoot,
                    new[] { "Owned.md" },
                    previousManifest));
        }

        [Fact]
        public void CreatePlan_WhenPreviousManifestContainsRootedEntry_ThrowsInvalidDataException()
        {
            var outputRoot = CreateOutputRoot();
            var rootedEntry = Path.Combine(
                Path.GetTempPath(),
                Path.GetRandomFileName() + ".md");
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
            var escapingEntry = Path.Combine("..", "outside.md");
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
            var rootedEntry = Path.Combine(
                Path.GetTempPath(),
                Path.GetRandomFileName() + ".md");

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
            var escapingEntry = Path.Combine("..", "outside.md");

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

        private static string CreateOutputRoot() =>
            Path.GetFullPath(
                Path.Combine(
                    Path.GetTempPath(),
                    "Xml2Doc.Tests",
                    Path.GetRandomFileName()));

        private static OutputManifest CreateManifest(
            string outputRoot,
            params string[] files) =>
            new(
                OutputManifest.CurrentSchemaVersion,
                Path.GetFullPath(outputRoot),
                files);
    }
}