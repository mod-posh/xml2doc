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
    /// Does <strong>not</strong> affect the underlying anchors or IDs—only visible text.
    /// </param>
    /// <param name="CodeBlockLanguage">
    /// Language identifier used for fenced code blocks (e.g., <c>csharp</c>, <c>xml</c>). Defaults to <c>csharp</c>.
    /// </param>
    /// <param name="TrimRootNamespaceInFileNames">
    /// When <see langword="true"/>, also applies <paramref name="RootNamespaceToTrim"/> to generated Markdown file names
    /// (in addition to headings). Useful if you want shorter file names alongside trimmed display text.
    /// Has no effect when <paramref name="RootNamespaceToTrim"/> is <see langword="null"/>.
    /// </param>
    /// <remarks>
    /// Typical usage:
    /// <code><![CDATA[
    /// var opts = new RendererOptions(
    ///     FileNameMode: FileNameMode.CleanGenerics,
    ///     RootNamespaceToTrim: "MyCompany.MyProduct",
    ///     CodeBlockLanguage: "csharp",
    ///     TrimRootNamespaceInFileNames: true);
    /// ]]></code>
    /// This will render type headings without the root namespace and produce cleaned generic file names without the prefix.
    /// </remarks>
    public sealed record RendererOptions(
        FileNameMode FileNameMode = FileNameMode.Verbatim,
        string? RootNamespaceToTrim = null,
        string CodeBlockLanguage = "csharp",
        bool TrimRootNamespaceInFileNames = false
    );
}
