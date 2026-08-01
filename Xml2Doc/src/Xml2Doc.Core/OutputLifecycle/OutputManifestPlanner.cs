using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Xml2Doc.Core.OutputLifecycle
{
    /// <summary>
    /// Creates deterministic output lifecycle plans from current outputs and prior ownership.
    /// </summary>
    public static class OutputManifestPlanner
    {
        /// <summary>
        /// Creates an output lifecycle plan for the current invocation.
        /// </summary>
        /// <param name="outputRoot">The root directory containing the generated output.</param>
        /// <param name="plannedOutputs">The output files planned by the current invocation.</param>
        /// <param name="previousManifest">
        /// The prior ownership manifest, or <see langword="null"/> when no prior ownership exists.
        /// </param>
        /// <returns>
        /// A deterministic plan containing the files to write and stale owned files to delete.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The current output root or planned outputs are invalid.
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// The previous manifest is unsupported, inconsistent, or unsafe.
        /// </exception>
        public static OutputLifecyclePlan CreatePlan(
            string outputRoot,
            IReadOnlyList<string> plannedOutputs,
            OutputManifest? previousManifest)
        {
            if (string.IsNullOrWhiteSpace(outputRoot))
            {
                throw new ArgumentException(
                    "The output root must be specified.",
                    nameof(outputRoot));
            }

            if (plannedOutputs is null)
            {
                throw new ArgumentNullException(nameof(plannedOutputs));
            }

            var canonicalOutputRoot = NormalizeRootPath(outputRoot);

            var filesToWrite = ValidateAndNormalizeEntries(
                canonicalOutputRoot,
                plannedOutputs,
                entry => new ArgumentException(
                    $"The planned output path '{entry}' is invalid.",
                    nameof(plannedOutputs)),
                duplicate => new ArgumentException(
                    $"The planned output path '{duplicate}' is duplicated.",
                    nameof(plannedOutputs)));

            if (previousManifest is null)
            {
                return new OutputLifecyclePlan(
                    filesToWrite,
                    Array.Empty<string>());
            }

            ValidateManifest(canonicalOutputRoot, previousManifest);

            var previouslyOwnedFiles = ValidateAndNormalizeEntries(
                canonicalOutputRoot,
                previousManifest.Files,
                entry => new InvalidDataException(
                    $"The manifest file entry '{entry}' is invalid."),
                duplicate => new InvalidDataException(
                    $"The manifest file entry '{duplicate}' is duplicated."));

            var currentFiles = new HashSet<string>(
                filesToWrite,
                GetPathComparer());

            var filesToDelete = previouslyOwnedFiles
                .Where(file => !currentFiles.Contains(file))
                .OrderBy(file => file, StringComparer.Ordinal)
                .ToArray();

            return new OutputLifecyclePlan(
                filesToWrite,
                filesToDelete);
        }

        private static void ValidateManifest(
            string canonicalOutputRoot,
            OutputManifest manifest)
        {
            if (manifest.SchemaVersion != OutputManifest.CurrentSchemaVersion)
            {
                throw new InvalidDataException(
                    $"Manifest schema version {manifest.SchemaVersion} is not supported.");
            }

            if (string.IsNullOrWhiteSpace(manifest.OutputRoot))
            {
                throw new InvalidDataException(
                    "The manifest output root is missing.");
            }

            string manifestOutputRoot;

            try
            {
                manifestOutputRoot = NormalizeRootPath(manifest.OutputRoot);
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is NotSupportedException ||
                exception is PathTooLongException)
            {
                throw new InvalidDataException(
                    "The manifest output root is invalid.",
                    exception);
            }

            if (!string.Equals(
                    canonicalOutputRoot,
                    manifestOutputRoot,
                    GetPathComparison()))
            {
                throw new InvalidDataException(
                    "The manifest output root does not match the current output root.");
            }

            if (manifest.Files is null)
            {
                throw new InvalidDataException(
                    "The manifest file list is missing.");
            }
        }

        private static IReadOnlyList<string> ValidateAndNormalizeEntries(
            string canonicalOutputRoot,
            IReadOnlyList<string> entries,
            Func<string, Exception> invalidEntryException,
            Func<string, Exception> duplicateEntryException)
        {
            var normalizedEntries = new List<string>(entries.Count);
            var uniqueEntries = new HashSet<string>(GetPathComparer());

            foreach (var entry in entries)
            {
                var normalizedEntry = NormalizeEntry(
                    canonicalOutputRoot,
                    entry,
                    invalidEntryException);

                if (!uniqueEntries.Add(normalizedEntry))
                {
                    throw duplicateEntryException(normalizedEntry);
                }

                normalizedEntries.Add(normalizedEntry);
            }

            normalizedEntries.Sort(StringComparer.Ordinal);
            return normalizedEntries;
        }

        private static string NormalizeRootPath(string path)
        {
            var fullPath = Path.GetFullPath(path);
            var pathRoot = Path.GetPathRoot(fullPath);

            while (fullPath.Length > pathRoot.Length &&
                   (fullPath[fullPath.Length - 1] == Path.DirectorySeparatorChar ||
                    fullPath[fullPath.Length - 1] == Path.AltDirectorySeparatorChar))
            {
                fullPath = fullPath.Substring(0, fullPath.Length - 1);
            }

            return fullPath;
        }

        private static string NormalizeEntry(
            string canonicalOutputRoot,
            string entry,
            Func<string, Exception> invalidEntryException)
        {
            if (string.IsNullOrWhiteSpace(entry) || Path.IsPathRooted(entry))
            {
                throw invalidEntryException(entry);
            }

            string fullPath;

            try
            {
                fullPath = Path.GetFullPath(
                    Path.Combine(canonicalOutputRoot, entry));
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is NotSupportedException ||
                exception is PathTooLongException)
            {
                throw invalidEntryException(entry);
            }

            var comparison = GetPathComparison();
            var rootWithSeparator = EnsureTrailingDirectorySeparator(
                canonicalOutputRoot);

            if (string.Equals(fullPath, canonicalOutputRoot, comparison) ||
                !fullPath.StartsWith(rootWithSeparator, comparison))
            {
                throw invalidEntryException(entry);
            }

            return fullPath.Substring(rootWithSeparator.Length);
        }

        private static string EnsureTrailingDirectorySeparator(string path)
        {
            if (path.EndsWith(
                    Path.DirectorySeparatorChar.ToString(),
                    StringComparison.Ordinal) ||
                path.EndsWith(
                    Path.AltDirectorySeparatorChar.ToString(),
                    StringComparison.Ordinal))
            {
                return path;
            }

            return path + Path.DirectorySeparatorChar;
        }

        private static StringComparer GetPathComparer() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;

        private static StringComparison GetPathComparison() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
    }
}