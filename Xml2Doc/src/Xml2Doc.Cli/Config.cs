using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xml2Doc.Cli
{
    /// <summary>
    /// Represents command-line configuration for the Xml2Doc tool.
    /// </summary>
    /// <remarks>
    /// Properties are nullable to distinguish between unspecified values and explicit settings.
    /// Typical mappings:
    /// - <see cref="Xml"/> ↔ <c>--xml</c>
    /// - <see cref="Out"/> ↔ <c>--out</c>
    /// - <see cref="Single"/> ↔ <c>--single</c>
    /// - <see cref="FileNames"/> ↔ <c>--file-names</c>
    /// - <see cref="RootNamespace"/> (optional future CLI) ↔ trims display names
    /// - <see cref="CodeLanguage"/> (optional future CLI) ↔ fenced code block language
    /// </remarks>
    public sealed class CliConfig
    {
        /// <summary>
        /// Path to the compiler-generated XML documentation file.
        /// </summary>
        /// <remarks>Maps to the <c>--xml</c> option.</remarks>
        public string? Xml { get; set; }

        /// <summary>
        /// Output location: a directory for per-type files, or a file path when single-file mode is enabled.
        /// </summary>
        /// <remarks>Maps to the <c>--out</c> option.</remarks>
        public string? Out { get; set; }

        /// <summary>
        /// When <see langword="true"/>, emit a single Markdown file; when <see langword="false"/>, emit per-type files.
        /// </summary>
        /// <remarks>
        /// <see langword="null"/> indicates the option was not specified on the command line.
        /// Maps to the <c>--single</c> option.
        /// </remarks>
        public bool? Single { get; set; }

        /// <summary>
        /// Filename generation mode for Markdown outputs.
        /// </summary>
        /// <remarks>
        /// Accepted values: <c>verbatim</c> (default) or <c>clean</c>.
        /// Maps to the <c>--file-names</c> option and corresponds to <see cref="Core.FileNameMode"/>.
        /// </remarks>
        public string? FileNames { get; set; }            // "verbatim" | "clean"

        /// <summary>
        /// Optional root namespace prefix to trim from displayed type names (e.g., <c>MyCompany.MyProduct</c>).
        /// </summary>
        public string? RootNamespace { get; set; }        // e.g., "MyCompany.MyProduct"

        /// <summary>
        /// Language identifier used for fenced code blocks in generated Markdown (e.g., <c>csharp</c>).
        /// </summary>
        public string? CodeLanguage { get; set; }         // e.g., "csharp"
    }
}
