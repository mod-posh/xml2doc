using System;

namespace Xml2Doc.Cli
{
    /// <summary>
    /// Command-line configuration options for the Xml2Doc CLI.
    /// </summary>
    /// <remarks>
    /// All properties are nullable so the parser can distinguish:
    /// <list type="bullet">
    ///   <item><description><c>null</c>: not supplied (use defaults).</description></item>
    ///   <item><description>Non-null: explicit user intent (even if empty for paths).</description></item>
    /// </list>
    /// Flags mapping:
    /// <c>--xml</c>, <c>--out</c>, <c>--single</c>, <c>--file-names</c>, <c>--rootns</c>, <c>--lang</c>,
    /// <c>--trim-rootns-filenames</c>, <c>--report</c>, <c>--dry-run</c>, <c>--diff</c>,
    /// <c>--anchor-algorithm</c>, <c>--template</c>, <c>--front-matter</c>, <c>--auto-link</c>,
    /// <c>--alias-map</c>, <c>--external-docs</c>, <c>--toc</c>, <c>--namespace-index</c>, <c>--parallel</c>.
    /// </remarks>
    public sealed class CliConfig
    {
        /// <summary>Path to the input XML documentation file. Maps to <c>--xml</c>.</summary>
        public string? Xml { get; set; }

        /// <summary>Output directory (per-type) or single-file path. Maps to <c>--out</c>.</summary>
        public string? Out { get; set; }

        /// <summary>Single consolidated output when true; otherwise per-type. Maps to <c>--single</c>.</summary>
        public bool? Single { get; set; }

        /// <summary>Filename mode (<c>verbatim</c> | <c>clean</c>). Maps to <c>--file-names</c>.</summary>
        public string? FileNames { get; set; }

        /// <summary>Namespace prefix trimmed from displayed type names. Maps to <c>--rootns</c>.</summary>
        public string? RootNamespace { get; set; }

        /// <summary>Language identifier for fenced code blocks (e.g. csharp). Maps to <c>--lang</c>.</summary>
        public string? CodeLanguage { get; set; }

        /// <summary>Also trim root namespace from file names when true. Maps to <c>--trim-rootns-filenames</c>.</summary>
        public bool? TrimRootNamespaceInFileNames { get; set; }

        /// <summary>Path to JSON execution/report file. Maps to <c>--report</c>.</summary>
        public string? Report { get; set; }

        /// <summary>Dry run (no writes) when true. Maps to <c>--dry-run</c>.</summary>
        public bool? DryRun { get; set; }

        /// <summary>Reserved for diff analysis (currently inert). Maps to <c>--diff</c>.</summary>
        public bool? Diff { get; set; }

        /// <summary>Anchor/slug algorithm (<c>default</c>|<c>github</c>|<c>kramdown</c>|<c>gfm</c>). Maps to <c>--anchor-algorithm</c>.</summary>
        public string? AnchorAlgorithm { get; set; }

        /// <summary>Template file applied around rendered content. Maps to <c>--template</c>.</summary>
        public string? Template { get; set; }

        /// <summary>Front-matter file (YAML/JSON/TOML) prepended to outputs. Maps to <c>--front-matter</c>.</summary>
        public string? FrontMatter { get; set; }

        /// <summary>Enable heuristic auto-linking in prose. Maps to <c>--auto-link</c>.</summary>
        public bool? AutoLink { get; set; }

        /// <summary>Alias map file for additional type/namespace substitutions. Maps to <c>--alias-map</c>.</summary>
        public string? AliasMap { get; set; }

        /// <summary>External docs base URL or map file for unresolved references. Maps to <c>--external-docs</c>.</summary>
        public string? ExternalDocs { get; set; }

        /// <summary>Emit table of contents when true. Maps to <c>--toc</c>.</summary>
        public bool? Toc { get; set; }

        /// <summary>Emit namespace index when true. Maps to <c>--namespace-index</c>.</summary>
        public bool? NamespaceIndex { get; set; }

        /// <summary>Max parallelism (<=0 or null uses default heuristic). Maps to <c>--parallel</c>.</summary>
        public int? Parallel { get; set; }
    }
}
