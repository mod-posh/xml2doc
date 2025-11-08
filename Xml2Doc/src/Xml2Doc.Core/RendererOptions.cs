using System;

namespace Xml2Doc.Core
{
    /// <summary>
    /// Controls how output file names are generated for documented types.
    /// </summary>
    public enum FileNameMode
    {
        /// <summary>
        /// Use the original documentation identifier verbatim (e.g., <c>MyLib.Widget`1</c> → <c>MyLib.Widget`1.md</c>).
        /// Generic arity markers (<c>`1</c>, <c>`2</c>, …) and braces remain unchanged.
        /// </summary>
        Verbatim,

        /// <summary>
        /// Remove generic arity markers (e.g., <c>MyLib.Widget`1</c> → <c>MyLib.Widget.md</c>) and normalize generic
        /// braces used in XML doc IDs so file names are cleaner and more stable across refactors.
        /// </summary>
        CleanGenerics
    }

    /// <summary>
    /// Options that control how XML documentation is rendered to Markdown.
    /// </summary>
    /// <param name="FileNameMode">
    /// Strategy for transforming type documentation IDs into Markdown file names (see <see cref="Core.FileNameMode"/>).
    /// </param>
    /// <param name="RootNamespaceToTrim">
    /// Optional namespace prefix to remove from displayed type headings and link labels
    /// (e.g., trimming <c>MyCompany.MyProduct</c> from <c>MyCompany.MyProduct.Feature.Widget</c> yields <c>Feature.Widget</c>).
    /// Does <strong>not</strong> affect underlying documentation IDs—only visible text (unless filename trimming is enabled).
    /// </param>
    /// <param name="CodeBlockLanguage">
    /// Language identifier used for fenced code blocks (e.g., <c>csharp</c>, <c>xml</c>). Defaults to <c>csharp</c>.
    /// </param>
    /// <param name="TrimRootNamespaceInFileNames">
    /// When <see langword="true"/>, also applies <paramref name="RootNamespaceToTrim"/> to generated Markdown file names
    /// (in addition to headings). Ignored when <paramref name="RootNamespaceToTrim"/> is <see langword="null"/>.
    /// </param>
    /// <param name="AnchorAlgorithm">
    /// Named algorithm for generating HTML anchors / fragment identifiers (e.g., <c>"default"</c>, <c>"github"</c>, <c>"strict"</c>).
    /// Allows future extension for slug styles without changing persisted links.
    /// </param>
    /// <param name="TemplatePath">
    /// Optional path to a user-supplied template (e.g., Razor / simple token template) applied around rendered content.
    /// When <see langword="null"/>, the built‑in layout is used.
    /// </param>
    /// <param name="FrontMatterPath">
    /// Optional path to a front‑matter snippet (YAML / TOML / JSON) prepended verbatim to each generated file (if present).
    /// Useful for static site generators (e.g., Jekyll / Hugo).
    /// </param>
    /// <param name="AutoLink">
    /// When <see langword="true"/>, heuristically converts plain type/member mentions in free text to links (best‑effort).
    /// Disabled by default to avoid accidental false positives.
    /// </param>
    /// <param name="AliasMapPath">
    /// Path to a JSON or text map defining additional type/namespace aliases (e.g., to rewrite or shorten certain names).
    /// If <see langword="null"/>, only built‑in C# aliases are applied.
    /// </param>
    /// <param name="ExternalDocs">
    /// Optional base URL for external documentation used when creating outbound links for unresolved cref targets
    /// (e.g., pointing to framework or third‑party API docs).
    /// </param>
    /// <param name="EmitToc">
    /// When <see langword="true"/>, emits an in‑document table of contents (e.g., at the top of single‑file output or per file).
    /// </param>
    /// <param name="EmitNamespaceIndex">
    /// When <see langword="true"/>, produces an additional namespace index (grouping types by namespace) alongside the standard index.
    /// </param>
    /// <param name="ParallelDegree">
    /// Optional maximum degree of parallelism for generation. <see langword="null"/> or values &lt;= 0 use a default heuristic;
    /// positive values cap concurrency (e.g., set to <c>Environment.ProcessorCount</c> for deterministic load).
    /// </param>
    /// <remarks>
    /// Example:
    /// <code><![CDATA[
    /// var opts = new RendererOptions(
    ///     FileNameMode: FileNameMode.CleanGenerics,
    ///     RootNamespaceToTrim: "MyCompany.MyProduct",
    ///     CodeBlockLanguage: "csharp",
    ///     TrimRootNamespaceInFileNames: true,
    ///     AnchorAlgorithm: "github",
    ///     TemplatePath: "templates/type.md.tpl",
    ///     FrontMatterPath: "templates/frontmatter.yml",
    ///     AutoLink: true,
    ///     AliasMapPath: "config/aliases.json",
    ///     ExternalDocs: "https://learn.microsoft.com/dotnet/api/",
    ///     EmitToc: true,
    ///     EmitNamespaceIndex: true,
    ///     ParallelDegree: Environment.ProcessorCount
    /// );
    /// ]]></code>
    /// </remarks>
    public sealed record RendererOptions(
        FileNameMode FileNameMode = FileNameMode.Verbatim,
        string? RootNamespaceToTrim = null,
        string CodeBlockLanguage = "csharp",
        bool TrimRootNamespaceInFileNames = false,
        string AnchorAlgorithm = "default",
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
