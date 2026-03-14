using System;

namespace Xml2Doc.Core
{
    /// <summary>
    /// Controls how output file names are generated for documented types.
    /// </summary>
    public enum FileNameMode
    {
        /// <summary>
        /// Verbatim: preserve the documentation identifier exactly (e.g. <c>MyLib.Widget`1</c> → <c>MyLib.Widget`1.md</c>).
        /// Generic arity tokens (<c>`N</c>) and XML‑doc generic braces remain unchanged.
        /// </summary>
        Verbatim,

        /// <summary>
        /// Clean: remove generic arity tokens (e.g. <c>MyLib.Widget`1</c> → <c>MyLib.Widget.md</c>) and normalize XML‑doc
        /// generic braces (<c>{}</c> → <c>&lt;&gt;</c>) producing shorter, more stable file names across refactors.
        /// </summary>
        CleanGenerics
    }

    /// <summary>
    /// Controls how heading anchors (slugs) are generated for types and members.
    /// </summary>
    public enum AnchorAlgorithm
    {
        /// <summary>
        /// Default: lowercase, collapse whitespace → single dash, strip non <c>[a-z0-9-]</c>, collapse multi‑dash runs, trim leading/trailing dashes.
        /// </summary>
        Default = 0,

        /// <summary>
        /// GitHub (GFM) style: Unicode normalize, remove diacritics, lowercase, drop punctuation (except space / dash),
        /// whitespace → dash, collapse multi‑dash runs, trim dashes.
        /// </summary>
        Github = 1,

        /// <summary>
        /// Kramdown/Jekyll style: similar to GitHub but preserves underscores (<c>_</c>) in the slug.
        /// </summary>
        Kramdown = 2,

        /// <summary>
        /// Alias of GitHub style (kept for explicit <c>gfm</c> selection in CLI/config).
        /// </summary>
        Gfm = 3
    }

    /// <summary>
    /// Rendering options applied when converting XML documentation to Markdown.
    /// </summary>
    /// <param name="FileNameMode">
    /// File naming strategy (see <see cref="Xml2Doc.Core.FileNameMode"/>). Applied before namespace trimming and basename stripping.
    /// </param>
    /// <param name="RootNamespaceToTrim">
    /// Optional namespace prefix removed from visible type headings and link labels (e.g. trimming <c>MyCompany.MyProduct</c>
    /// from <c>MyCompany.MyProduct.Feature.Widget</c> yields <c>Feature.Widget</c>). Does not alter underlying IDs.
    /// </param>
    /// <param name="CodeBlockLanguage">
    /// Default fenced code block language (e.g. <c>csharp</c>, <c>xml</c>) used when no language is specified in source XML.
    /// </param>
    /// <param name="TrimRootNamespaceInFileNames">
    /// When true, also trims <paramref name="RootNamespaceToTrim"/> from generated file names after <paramref name="FileNameMode"/> normalization.
    /// Ignored if <paramref name="RootNamespaceToTrim"/> is <see langword="null"/> / empty.
    /// </param>
    /// <param name="AnchorAlgorithm">
    /// Slug algorithm for headings (see <see cref="Xml2Doc.Core.AnchorAlgorithm"/>). Changing this after publication alters fragment IDs.
    /// </param>
    /// <param name="TemplatePath">
    /// Optional path to a wrapping template (e.g. Razor / token) applied around rendered body content; null = built‑in minimal layout.
    /// </param>
    /// <param name="FrontMatterPath">
    /// Optional path to front‑matter (YAML / TOML / JSON) prepended verbatim to each output file (for SSG integration).
    /// </param>
    /// <param name="AutoLink">
    /// When true, heuristically links unadorned type/member mentions in prose. Off by default to reduce false positives.
    /// </param>
    /// <param name="AliasMapPath">
    /// Path to a JSON/text alias map adding custom type/namespace replacements beyond built‑in C# keyword aliases.
    /// </param>
    /// <param name="ExternalDocs">
    /// Base URL (or map) for external documentation used for unresolved cref targets (e.g. framework APIs).
    /// </param>
    /// <param name="EmitToc">
    /// When true, emits a member table of contents per type in multi‑file mode (suppressed in single‑file mode).
    /// </param>
    /// <param name="EmitNamespaceIndex">
    /// When true, generates a <c>namespaces.md</c> overview plus one page per namespace (multi‑file mode only).
    /// </param>
    /// <param name="BasenameOnly">
    /// When true, file names drop namespace segments (after trimming if enabled), keeping only the final identifier.
    /// </param>
    /// <param name="ParallelDegree">
    /// Max parallelism for rendering; <see langword="null"/> or &lt;= 0 selects a heuristic (typically <c>Environment.ProcessorCount</c>).
    /// </param>
    /// <remarks>
    /// Example:
    /// <code><![CDATA[
    /// var opts = new RendererOptions(
    ///     FileNameMode: FileNameMode.CleanGenerics,
    ///     RootNamespaceToTrim: "MyCompany.MyProduct",
    ///     CodeBlockLanguage: "csharp",
    ///     TrimRootNamespaceInFileNames: true,
    ///     AnchorAlgorithm: AnchorAlgorithm.Github,
    ///     TemplatePath: "templates/type.md.tpl",
    ///     FrontMatterPath: "templates/frontmatter.yml",
    ///     AutoLink: true,
    ///     AliasMapPath: "config/aliases.json",
    ///     ExternalDocs: "https://learn.microsoft.com/dotnet/api/",
    ///     EmitToc: true,
    ///     EmitNamespaceIndex: true,
    ///     BasenameOnly: false,
    ///     ParallelDegree: Environment.ProcessorCount
    /// );
    /// ]]></code>
    /// Ordering:
    /// <list type="bullet">
    ///   <item><description><see cref="FileNameMode"/> normalization → root namespace trimming → basename stripping.</description></item>
    ///   <item><description>Slug generation uses <see cref="AnchorAlgorithm"/> and does not depend on file naming.</description></item>
    ///   <item><description>Changing <see cref="AnchorAlgorithm"/> after publishing may invalidate inbound links.</description></item>
    /// </list>
    /// </remarks>
    public sealed record RendererOptions(
        FileNameMode FileNameMode = FileNameMode.Verbatim,
        string? RootNamespaceToTrim = null,
        string CodeBlockLanguage = "csharp",
        bool TrimRootNamespaceInFileNames = false,
        AnchorAlgorithm AnchorAlgorithm = AnchorAlgorithm.Default,
        string? TemplatePath = null,
        string? FrontMatterPath = null,
        bool AutoLink = false,
        string? AliasMapPath = null,
        string? ExternalDocs = null,
        bool EmitToc = false,
        bool EmitNamespaceIndex = false,
        bool BasenameOnly = false,
        int? ParallelDegree = null
    );
}
