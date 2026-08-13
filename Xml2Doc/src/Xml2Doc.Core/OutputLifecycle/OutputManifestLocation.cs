using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Xml2Doc.Core.OutputLifecycle
{
    /// <summary>
    /// Identifies the deterministic storage location for one invocation-scoped output manifest.
    /// </summary>
    public sealed record OutputManifestLocation
    {
        private const string MetadataDirectoryName = ".xml2doc";
        private const string ManifestDirectoryName = "manifests";
        private static readonly Encoding StrictUtf8Encoding =
            new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false,
                throwOnInvalidBytes: true);

        private OutputManifestLocation(
            string outputRoot,
            string identity,
            string identityHash,
            string manifestPath)
        {
            OutputRoot = outputRoot;
            Identity = identity;
            IdentityHash = identityHash;
            ManifestPath = manifestPath;
        }

        /// <summary>
        /// Gets the canonical absolute root containing the generated output.
        /// </summary>
        public string OutputRoot { get; }

        /// <summary>
        /// Gets the exact opaque identity supplied by the caller.
        /// </summary>
        public string Identity { get; }

        /// <summary>
        /// Gets the lowercase SHA-256 hexadecimal hash of the identity's exact UTF-8 bytes.
        /// </summary>
        public string IdentityHash { get; }

        /// <summary>
        /// Gets the canonical absolute path at which the invocation manifest is stored.
        /// </summary>
        public string ManifestPath { get; }

        /// <summary>
        /// Creates an invocation-scoped manifest location.
        /// </summary>
        /// <param name="outputRoot">The root directory containing generated output.</param>
        /// <param name="identity">The explicit, stable identity for the invocation.</param>
        /// <returns>The validated deterministic manifest location.</returns>
        /// <exception cref="ArgumentException">
        /// The output root or manifest identity is missing or invalid.
        /// </exception>
        public static OutputManifestLocation Create(
            string outputRoot,
            string identity)
        {
            if (string.IsNullOrWhiteSpace(outputRoot))
            {
                throw new ArgumentException(
                    "The output root must be specified.",
                    nameof(outputRoot));
            }

            if (string.IsNullOrWhiteSpace(identity))
            {
                throw new ArgumentException(
                    "The manifest identity must be specified.",
                    nameof(identity));
            }

            string canonicalOutputRoot;

            try
            {
                canonicalOutputRoot = NormalizeRootPath(outputRoot);
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is NotSupportedException ||
                exception is PathTooLongException)
            {
                throw new ArgumentException(
                    "The output root is invalid.",
                    nameof(outputRoot),
                    exception);
            }

            var identityHash = ComputeIdentityHash(identity);
            var manifestFileName = identityHash + ".json";
            var manifestPath = CombineRelativeSegments(
                canonicalOutputRoot,
                MetadataDirectoryName,
                ManifestDirectoryName,
                manifestFileName);

            return new OutputManifestLocation(
                canonicalOutputRoot,
                identity,
                identityHash,
                manifestPath);
        }

        private static string ComputeIdentityHash(string identity)
        {
            byte[] identityBytes;

            try
            {
                identityBytes = StrictUtf8Encoding.GetBytes(identity);
            }
            catch (EncoderFallbackException exception)
            {
                throw new ArgumentException(
                    "The manifest identity contains invalid Unicode text.",
                    nameof(identity),
                    exception);
            }

            byte[] hash;

            using (var sha256 = SHA256.Create())
            {
                hash = sha256.ComputeHash(identityBytes);
            }

            var hexadecimalHash = new StringBuilder(hash.Length * 2);

            foreach (var value in hash)
            {
                hexadecimalHash.Append(value.ToString("x2"));
            }

            return hexadecimalHash.ToString();
        }

        private static string CombineRelativeSegments(
            string root,
            params string[] segments)
        {
            var path = root;

            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment) ||
                    Path.IsPathRooted(segment))
                {
                    throw new ArgumentException(
                        "Manifest path segments must be non-empty relative paths.",
                        nameof(segments));
                }

                path = EnsureTrailingDirectorySeparator(path) + segment;
            }

            return path;
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

        private static string NormalizeRootPath(string path)
        {
            var fullPath = Path.GetFullPath(path);
            var pathRoot = Path.GetPathRoot(fullPath);

            if (string.IsNullOrEmpty(pathRoot))
            {
                throw new ArgumentException(
                    "The path does not contain a valid filesystem root.",
                    nameof(path));
            }

            while (fullPath.Length > pathRoot.Length &&
                   (fullPath[fullPath.Length - 1] == Path.DirectorySeparatorChar ||
                    fullPath[fullPath.Length - 1] == Path.AltDirectorySeparatorChar))
            {
                fullPath = fullPath.Substring(0, fullPath.Length - 1);
            }

            return fullPath;
        }
    }
}
