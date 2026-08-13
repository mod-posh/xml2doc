using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Xml2Doc.Core.OutputLifecycle
{
    /// <summary>
    /// Serializes and loads invocation-scoped output manifests without filesystem access.
    /// </summary>
    public static class OutputManifestSerializer
    {
        private static readonly JsonSerializerOptions SerializerOptions =
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

        /// <summary>
        /// Creates a validated manifest for the current invocation and serializes it deterministically.
        /// </summary>
        /// <param name="location">The invocation-scoped manifest location.</param>
        /// <param name="ownedFiles">The output-root-relative files owned by the invocation.</param>
        /// <returns>The deterministic JSON representation of the manifest.</returns>
        public static string Serialize(
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

            var normalizedFiles = OutputManifestPlanner.CreatePlan(
                location.OutputRoot,
                ownedFiles,
                previousManifest: null).FilesToWrite;

            var manifest = new OutputManifest(
                OutputManifest.CurrentSchemaVersion,
                location.Identity,
                location.OutputRoot,
                normalizedFiles);

            return JsonSerializer
                .Serialize(manifest, SerializerOptions)
                .Replace("\r\n", "\n");
        }

        /// <summary>
        /// Loads and validates a manifest for the requested invocation.
        /// </summary>
        /// <param name="json">The JSON manifest content.</param>
        /// <param name="location">The requested invocation-scoped manifest location.</param>
        /// <returns>A validated manifest with normalized, ordinally ordered owned paths.</returns>
        /// <exception cref="InvalidDataException">
        /// The manifest is malformed, unsupported, inconsistent, or unsafe.
        /// </exception>
        public static OutputManifest Deserialize(
            string json,
            OutputManifestLocation location)
        {
            if (json is null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            if (location is null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            OutputManifest? manifest;

            try
            {
                manifest = JsonSerializer.Deserialize<OutputManifest>(
                    json,
                    SerializerOptions);
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException(
                    "The output manifest contains invalid JSON.",
                    exception);
            }
            catch (NotSupportedException exception)
            {
                throw new InvalidDataException(
                    "The output manifest has an unsupported JSON shape.",
                    exception);
            }

            if (manifest is null)
            {
                throw new InvalidDataException(
                    "The output manifest is missing.");
            }

            if (!string.Equals(
                    manifest.Identity,
                    location.Identity,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The manifest identity does not match the requested identity.");
            }

            var validatedPlan = OutputManifestPlanner.CreatePlan(
                location.OutputRoot,
                Array.Empty<string>(),
                manifest);

            return new OutputManifest(
                manifest.SchemaVersion,
                manifest.Identity,
                location.OutputRoot,
                validatedPlan.FilesToDelete);
        }
    }
}
