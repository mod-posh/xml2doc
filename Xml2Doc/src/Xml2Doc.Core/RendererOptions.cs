using System;
using Xml2Doc.Core.Aliasing;
using Xml2Doc.Core.Anchoring;
using Xml2Doc.Core.Templates;
using Xml2Doc.Core.AutoLinking;
using Xml2Doc.Core.Linking;
using Xml2Doc.Core.Signatures;

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
    /// Controls the line-ending sequence used in rendered Markdown.
    /// </summary>
    public enum LineEndingStyle
    {
        /// <summary>Use line feed (<c>\n</c>) on every platform.</summary>
        Lf = 0,

        /// <summary>Use carriage return followed by line feed (<c>\r\n</c>).</summary>
        CrLf = 1,

        /// <summary>Use <see cref="Environment.NewLine"/> for the current host.</summary>
        Native = 2
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
    /// Base URL for external documentation used for unresolved cref targets (e.g. framework APIs).
    /// Requires <see cref="LinkPolicy.PreferExternalForUnknown"/>.
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
    /// <param name="GenerateIndex">
    /// When true, per-type output includes <c>index.md</c>. Disable this when multiple independent
    /// invocations intentionally share one output directory and index ownership is handled separately.
    /// </param>
    /// <param name="PruneStaleFiles">
    /// When true, per-type rendering removes only stale files recorded by the same invocation manifest.
    /// Disabled by default.
    /// </param>
    /// <param name="ManifestIdentity">
    /// Explicit stable invocation identity required when <paramref name="PruneStaleFiles"/> is true.
    /// </param>
    /// <param name="LineEndings">
    /// Line-ending policy for all rendered Markdown. Defaults to deterministic LF on every host.
    /// </param>
    /// <param name="WarningSink">
    /// Optional callback invoked for non-fatal rendering warnings, including unresolved
    /// <c>&lt;inheritdoc /&gt;</c> members.
    /// </param>
    /// <param name="AliasProvider">
    /// Optional alias provider. When omitted, <see cref="DefaultAliasProvider"/> preserves the
    /// built-in C# keyword mappings.
    /// </param>
    /// <param name="AnchorGenerator">
    /// Optional anchor generator. When omitted, the selected <paramref name="AnchorAlgorithm"/>
    /// uses Xml2Doc's built-in implementation.
    /// </param>
    /// <param name="TemplateRenderer">
    /// Optional programmatic template renderer. Cannot be combined with
    /// <paramref name="TemplatePath"/> or <paramref name="FrontMatterPath"/>.
    /// </param>
    /// <param name="FrontMatter">
    /// Optional per-document metadata provider. Returned scalar values are serialized as
    /// deterministic YAML front matter. Cannot be combined with <paramref name="FrontMatterPath"/>.
    /// </param>
    /// <param name="AutoLinker">
    /// Optional free-text linker used when <paramref name="AutoLink"/> is true.
    /// </param>
    /// <param name="LinkPolicy">
    /// Controls whether unresolved cref targets retain the existing internal-link
    /// behavior or are offered to an external resolver.
    /// </param>
    /// <param name="ExternalSymbolResolver">
    /// Optional provider for unresolved cref targets. When omitted and
    /// <paramref name="ExternalDocs"/> is set, a
    /// <see cref="BaseUrlExternalSymbolResolver"/> is used.
    /// </param>
    /// <param name="SignatureStyle">
    /// Optional controls for parameter names, generic constraints, and default values.
    /// Constraint output also uses documented generic parameter names in the signature.
    /// The default preserves existing signature output.
    /// </param>
    /// <param name="SignatureRenderer">
    /// Optional signature and label renderer. When omitted, Xml2Doc uses
    /// <see cref="DefaultSignatureRenderer"/>.
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
        int? ParallelDegree = null,
        bool GenerateIndex = true,
        bool PruneStaleFiles = false,
        string? ManifestIdentity = null,
        LineEndingStyle LineEndings = LineEndingStyle.Lf,
        Action<string>? WarningSink = null,
        IAliasProvider? AliasProvider = null,
        IAnchorGenerator? AnchorGenerator = null,
        ITemplateRenderer? TemplateRenderer = null,
        Func<TemplateRenderContext, IReadOnlyDictionary<string, object?>>? FrontMatter = null,
        IAutoLinker? AutoLinker = null,
        LinkPolicy LinkPolicy = LinkPolicy.InternalOnly,
        IExternalSymbolResolver? ExternalSymbolResolver = null,
        SignatureStyle? SignatureStyle = null,
        ISignatureRenderer? SignatureRenderer = null
    )
    {
        /// <summary>
        /// Preserves the constructor signature published with external cref resolution.
        /// </summary>
        public RendererOptions(
            FileNameMode FileNameMode,
            string? RootNamespaceToTrim,
            string CodeBlockLanguage,
            bool TrimRootNamespaceInFileNames,
            AnchorAlgorithm AnchorAlgorithm,
            string? TemplatePath,
            string? FrontMatterPath,
            bool AutoLink,
            string? AliasMapPath,
            string? ExternalDocs,
            bool EmitToc,
            bool EmitNamespaceIndex,
            bool BasenameOnly,
            int? ParallelDegree,
            bool GenerateIndex,
            bool PruneStaleFiles,
            string? ManifestIdentity,
            LineEndingStyle LineEndings,
            Action<string>? WarningSink,
            IAliasProvider? AliasProvider,
            IAnchorGenerator? AnchorGenerator,
            ITemplateRenderer? TemplateRenderer,
            Func<TemplateRenderContext, IReadOnlyDictionary<string, object?>>? FrontMatter,
            IAutoLinker? AutoLinker,
            LinkPolicy LinkPolicy,
            IExternalSymbolResolver? ExternalSymbolResolver)
            : this(
                FileNameMode,
                RootNamespaceToTrim,
                CodeBlockLanguage,
                TrimRootNamespaceInFileNames,
                AnchorAlgorithm,
                TemplatePath,
                FrontMatterPath,
                AutoLink,
                AliasMapPath,
                ExternalDocs,
                EmitToc,
                EmitNamespaceIndex,
                BasenameOnly,
                ParallelDegree,
                GenerateIndex,
                PruneStaleFiles,
                ManifestIdentity,
                LineEndings,
                WarningSink,
                AliasProvider,
                AnchorGenerator,
                TemplateRenderer,
                FrontMatter,
                AutoLinker,
                LinkPolicy,
                ExternalSymbolResolver,
                SignatureStyle: null,
                SignatureRenderer: null)
        {
        }

        /// <summary>
        /// Preserves the constructor signature published with auto-linker injection.
        /// </summary>
        public RendererOptions(
            FileNameMode FileNameMode,
            string? RootNamespaceToTrim,
            string CodeBlockLanguage,
            bool TrimRootNamespaceInFileNames,
            AnchorAlgorithm AnchorAlgorithm,
            string? TemplatePath,
            string? FrontMatterPath,
            bool AutoLink,
            string? AliasMapPath,
            string? ExternalDocs,
            bool EmitToc,
            bool EmitNamespaceIndex,
            bool BasenameOnly,
            int? ParallelDegree,
            bool GenerateIndex,
            bool PruneStaleFiles,
            string? ManifestIdentity,
            LineEndingStyle LineEndings,
            Action<string>? WarningSink,
            IAliasProvider? AliasProvider,
            IAnchorGenerator? AnchorGenerator,
            ITemplateRenderer? TemplateRenderer,
            Func<TemplateRenderContext, IReadOnlyDictionary<string, object?>>? FrontMatter,
            IAutoLinker? AutoLinker)
            : this(
                FileNameMode,
                RootNamespaceToTrim,
                CodeBlockLanguage,
                TrimRootNamespaceInFileNames,
                AnchorAlgorithm,
                TemplatePath,
                FrontMatterPath,
                AutoLink,
                AliasMapPath,
                ExternalDocs,
                EmitToc,
                EmitNamespaceIndex,
                BasenameOnly,
                ParallelDegree,
                GenerateIndex,
                PruneStaleFiles,
                ManifestIdentity,
                LineEndings,
                WarningSink,
                AliasProvider,
                AnchorGenerator,
                TemplateRenderer,
                FrontMatter,
                AutoLinker,
                LinkPolicy: LinkPolicy.InternalOnly,
                ExternalSymbolResolver: null)
        {
        }

        /// <summary>
        /// Preserves the constructor signature published with front-matter injection.
        /// </summary>
        public RendererOptions(
            FileNameMode FileNameMode,
            string? RootNamespaceToTrim,
            string CodeBlockLanguage,
            bool TrimRootNamespaceInFileNames,
            AnchorAlgorithm AnchorAlgorithm,
            string? TemplatePath,
            string? FrontMatterPath,
            bool AutoLink,
            string? AliasMapPath,
            string? ExternalDocs,
            bool EmitToc,
            bool EmitNamespaceIndex,
            bool BasenameOnly,
            int? ParallelDegree,
            bool GenerateIndex,
            bool PruneStaleFiles,
            string? ManifestIdentity,
            LineEndingStyle LineEndings,
            Action<string>? WarningSink,
            IAliasProvider? AliasProvider,
            IAnchorGenerator? AnchorGenerator,
            ITemplateRenderer? TemplateRenderer,
            Func<TemplateRenderContext, IReadOnlyDictionary<string, object?>>? FrontMatter)
            : this(
                FileNameMode,
                RootNamespaceToTrim,
                CodeBlockLanguage,
                TrimRootNamespaceInFileNames,
                AnchorAlgorithm,
                TemplatePath,
                FrontMatterPath,
                AutoLink,
                AliasMapPath,
                ExternalDocs,
                EmitToc,
                EmitNamespaceIndex,
                BasenameOnly,
                ParallelDegree,
                GenerateIndex,
                PruneStaleFiles,
                ManifestIdentity,
                LineEndings,
                WarningSink,
                AliasProvider,
                AnchorGenerator,
                TemplateRenderer,
                FrontMatter,
                AutoLinker: null)
        {
        }

        /// <summary>
        /// Preserves the constructor signature published with template-renderer injection.
        /// </summary>
        public RendererOptions(
            FileNameMode FileNameMode,
            string? RootNamespaceToTrim,
            string CodeBlockLanguage,
            bool TrimRootNamespaceInFileNames,
            AnchorAlgorithm AnchorAlgorithm,
            string? TemplatePath,
            string? FrontMatterPath,
            bool AutoLink,
            string? AliasMapPath,
            string? ExternalDocs,
            bool EmitToc,
            bool EmitNamespaceIndex,
            bool BasenameOnly,
            int? ParallelDegree,
            bool GenerateIndex,
            bool PruneStaleFiles,
            string? ManifestIdentity,
            LineEndingStyle LineEndings,
            Action<string>? WarningSink,
            IAliasProvider? AliasProvider,
            IAnchorGenerator? AnchorGenerator,
            ITemplateRenderer? TemplateRenderer)
            : this(
                FileNameMode,
                RootNamespaceToTrim,
                CodeBlockLanguage,
                TrimRootNamespaceInFileNames,
                AnchorAlgorithm,
                TemplatePath,
                FrontMatterPath,
                AutoLink,
                AliasMapPath,
                ExternalDocs,
                EmitToc,
                EmitNamespaceIndex,
                BasenameOnly,
                ParallelDegree,
                GenerateIndex,
                PruneStaleFiles,
                ManifestIdentity,
                LineEndings,
                WarningSink,
                AliasProvider,
                AnchorGenerator,
                TemplateRenderer,
                FrontMatter: null,
                AutoLinker: null)
        {
        }

        /// <summary>
        /// Preserves the constructor signature published with anchor-generator injection.
        /// </summary>
        public RendererOptions(
            FileNameMode FileNameMode,
            string? RootNamespaceToTrim,
            string CodeBlockLanguage,
            bool TrimRootNamespaceInFileNames,
            AnchorAlgorithm AnchorAlgorithm,
            string? TemplatePath,
            string? FrontMatterPath,
            bool AutoLink,
            string? AliasMapPath,
            string? ExternalDocs,
            bool EmitToc,
            bool EmitNamespaceIndex,
            bool BasenameOnly,
            int? ParallelDegree,
            bool GenerateIndex,
            bool PruneStaleFiles,
            string? ManifestIdentity,
            LineEndingStyle LineEndings,
            Action<string>? WarningSink,
            IAliasProvider? AliasProvider,
            IAnchorGenerator? AnchorGenerator)
            : this(
                FileNameMode,
                RootNamespaceToTrim,
                CodeBlockLanguage,
                TrimRootNamespaceInFileNames,
                AnchorAlgorithm,
                TemplatePath,
                FrontMatterPath,
                AutoLink,
                AliasMapPath,
                ExternalDocs,
                EmitToc,
                EmitNamespaceIndex,
                BasenameOnly,
                ParallelDegree,
                GenerateIndex,
                PruneStaleFiles,
                ManifestIdentity,
                LineEndings,
                WarningSink,
                AliasProvider,
                AnchorGenerator,
                TemplateRenderer: null,
                FrontMatter: null,
                AutoLinker: null)
        {
        }

        /// <summary>
        /// Preserves the constructor signature published with alias-provider injection.
        /// </summary>
        public RendererOptions(
            FileNameMode FileNameMode,
            string? RootNamespaceToTrim,
            string CodeBlockLanguage,
            bool TrimRootNamespaceInFileNames,
            AnchorAlgorithm AnchorAlgorithm,
            string? TemplatePath,
            string? FrontMatterPath,
            bool AutoLink,
            string? AliasMapPath,
            string? ExternalDocs,
            bool EmitToc,
            bool EmitNamespaceIndex,
            bool BasenameOnly,
            int? ParallelDegree,
            bool GenerateIndex,
            bool PruneStaleFiles,
            string? ManifestIdentity,
            LineEndingStyle LineEndings,
            Action<string>? WarningSink,
            IAliasProvider? AliasProvider)
            : this(
                FileNameMode,
                RootNamespaceToTrim,
                CodeBlockLanguage,
                TrimRootNamespaceInFileNames,
                AnchorAlgorithm,
                TemplatePath,
                FrontMatterPath,
                AutoLink,
                AliasMapPath,
                ExternalDocs,
                EmitToc,
                EmitNamespaceIndex,
                BasenameOnly,
                ParallelDegree,
                GenerateIndex,
                PruneStaleFiles,
                ManifestIdentity,
                LineEndings,
                WarningSink,
                AliasProvider,
                AnchorGenerator: null,
                TemplateRenderer: null,
                FrontMatter: null,
                AutoLinker: null)
        {
        }

        /// <summary>
        /// Preserves the constructor signature published before alias-provider injection was added.
        /// </summary>
        public RendererOptions(
            FileNameMode FileNameMode,
            string? RootNamespaceToTrim,
            string CodeBlockLanguage,
            bool TrimRootNamespaceInFileNames,
            AnchorAlgorithm AnchorAlgorithm,
            string? TemplatePath,
            string? FrontMatterPath,
            bool AutoLink,
            string? AliasMapPath,
            string? ExternalDocs,
            bool EmitToc,
            bool EmitNamespaceIndex,
            bool BasenameOnly,
            int? ParallelDegree,
            bool GenerateIndex,
            bool PruneStaleFiles,
            string? ManifestIdentity,
            LineEndingStyle LineEndings,
            Action<string>? WarningSink)
            : this(
                FileNameMode,
                RootNamespaceToTrim,
                CodeBlockLanguage,
                TrimRootNamespaceInFileNames,
                AnchorAlgorithm,
                TemplatePath,
                FrontMatterPath,
                AutoLink,
                AliasMapPath,
                ExternalDocs,
                EmitToc,
                EmitNamespaceIndex,
                BasenameOnly,
                ParallelDegree,
                GenerateIndex,
                PruneStaleFiles,
                ManifestIdentity,
                LineEndings,
                WarningSink,
                AliasProvider: null,
                AnchorGenerator: null,
                TemplateRenderer: null,
                FrontMatter: null,
                AutoLinker: null)
        {
        }
    }
}
