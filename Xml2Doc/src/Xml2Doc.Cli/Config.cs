using System;

namespace Xml2Doc.Cli
{
    /// <summary>
    /// Command‑line configuration options for the Xml2Doc CLI.
    /// </summary>
    /// <remarks>
    /// All properties are nullable so callers can distinguish between:
    /// <list type="bullet">
    ///   <item><description><c>null</c>: option not provided on the command line (use defaults).</description></item>
    ///   <item><description>Non‑null: explicit user intent (even if empty for paths).</description></item>
    /// </list>
    /// Mapped flags:
    /// <c>--xml</c>, <c>--out</c>, <c>--single</c>, <c>--file-names</c>, <c>--rootns</c>, <c>--lang</c>,
    /// <c>--trim-rootns-filenames</c>, <c>--report</c>, <c>--dry-run</c>, <c>--diff</c>.
    /// </remarks>
    public sealed class CliConfig
    {
        /// <summary>
        /// Path to the input XML documentation file (e.g. compiler output). Maps to <c>--xml</c>.
        /// </summary>
        public string? Xml { get; set; }

        /// <summary>
        /// Output directory or single-file target directory depending on mode. Maps to <c>--out</c>.
        /// </summary>
        public string? Out { get; set; }

        /// <summary>
        /// When <see langword="true"/>, generate a single consolidated Markdown file; otherwise per-type files. Maps to <c>--single</c>.
        /// </summary>
        public bool? Single { get; set; }

        /// <summary>
        /// Filename mode: expected values <c>verbatim</c> or <c>clean</c>. Maps to <c>--file-names</c>.
        /// </summary>
        public string? FileNames { get; set; }           // "verbatim" | "clean"

        /// <summary>
        /// Namespace prefix trimmed from displayed type names (e.g. <c>MyCompany.MyProduct</c>). Maps to <c>--rootns</c>.
        /// </summary>
        public string? RootNamespace { get; set; }       // e.g., "MyCompany.MyProduct"

        /// <summary>
        /// Language identifier for fenced code blocks (default typically <c>csharp</c>). Maps to <c>--lang</c>.
        /// </summary>
        public string? CodeLanguage { get; set; }        // e.g., "csharp"

        /// <summary>
        /// When <see langword="true"/>, also trims the root namespace from generated file names. Maps to <c>--trim-rootns-filenames</c>.
        /// </summary>
        public bool? TrimRootNamespaceInFileNames { get; set; }

        /// <summary>
        /// Path to a JSON execution report capturing options, outputs, and fingerprints. Maps to <c>--report</c>.
        /// </summary>
        public string? Report { get; set; }              // path to JSON report

        /// <summary>
        /// Dry-run: compute planned outputs without writing Markdown. Maps to <c>--dry-run</c>.
        /// </summary>
        public bool? DryRun { get; set; }

        /// <summary>
        /// Reserved for future diff/changes analysis; currently no effect. Maps to <c>--diff</c>.
        /// </summary>
        public bool? Diff { get; set; }
        // add to CliConfig
        public string? AnchorAlgorithm { get; set; }   // "default"|"github"|"kramdown"|"gfm"
        public string? Template { get; set; }          // path to template file
        public string? FrontMatter { get; set; }       // path to YAML/JSON front-matter
        public bool? AutoLink { get; set; }
        public string? AliasMap { get; set; }          // path to JSON alias map
        public string? ExternalDocs { get; set; }      // base URL or path to map
        public bool? Toc { get; set; }
        public bool? NamespaceIndex { get; set; }
        public int? Parallel { get; set; }             // 0/1 disables; >1 enables
    }
}
