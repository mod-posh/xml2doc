using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Xml2Doc.Core.OutputLifecycle
{
    /// <summary>
    /// Loads and atomically persists invocation-scoped output manifests.
    /// </summary>
    public static class OutputManifestStore
    {
        private static readonly Encoding Utf8WithoutBom =
            new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false,
                throwOnInvalidBytes: true);

        /// <summary>
        /// Loads the requested manifest when it exists.
        /// </summary>
        /// <param name="location">The invocation-scoped manifest location.</param>
        /// <returns>
        /// The validated manifest, or <see langword="null"/> when no manifest exists.
        /// </returns>
        public static OutputManifest? Load(OutputManifestLocation location)
        {
            if (location is null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (!File.Exists(location.ManifestPath))
            {
                return null;
            }

            string json;

            try
            {
                json = File.ReadAllText(
                    location.ManifestPath,
                    Utf8WithoutBom);
            }
            catch (DecoderFallbackException exception)
            {
                throw new InvalidDataException(
                    "The output manifest is not valid UTF-8.",
                    exception);
            }

            return OutputManifestSerializer.Deserialize(json, location);
        }

        /// <summary>
        /// Validates and atomically persists the current invocation's ownership manifest.
        /// </summary>
        /// <param name="location">The invocation-scoped manifest location.</param>
        /// <param name="ownedFiles">The output-root-relative files owned by the invocation.</param>
        public static void Save(
            OutputManifestLocation location,
            IReadOnlyList<string> ownedFiles)
        {
            if (location is null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (ownedFiles is null)
            {
                throw new ArgumentNullException(nameof(ownedFiles));
            }

            var json = OutputManifestSerializer.Serialize(
                location,
                ownedFiles);
            var manifestDirectory = Path.GetDirectoryName(
                location.ManifestPath);

            if (string.IsNullOrEmpty(manifestDirectory))
            {
                throw new InvalidOperationException(
                    "The manifest path does not contain a directory.");
            }

            Directory.CreateDirectory(manifestDirectory);

            var manifestFileName = Path.GetFileName(location.ManifestPath);

            if (string.IsNullOrEmpty(manifestFileName))
            {
                throw new InvalidOperationException(
                    "The manifest path does not contain a file name.");
            }

            var temporaryFileName =
                "." + manifestFileName +
                "." + Guid.NewGuid().ToString("N") + ".tmp";

            if (Path.IsPathRooted(temporaryFileName))
            {
                throw new InvalidOperationException(
                    "The temporary manifest file name must be relative.");
            }

            var temporaryPath =
                manifestDirectory + Path.DirectorySeparatorChar +
                temporaryFileName;

            try
            {
                File.WriteAllText(
                    temporaryPath,
                    json,
                    Utf8WithoutBom);

                if (File.Exists(location.ManifestPath))
                {
                    File.Replace(
                        temporaryPath,
                        location.ManifestPath,
                        destinationBackupFileName: null);
                }
                else
                {
                    File.Move(
                        temporaryPath,
                        location.ManifestPath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
    }
}
