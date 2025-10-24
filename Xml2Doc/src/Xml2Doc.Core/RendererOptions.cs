using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xml2Doc.Core
{
    /// <summary>
    /// Controls how output file names are generated for documented types.
    /// </summary>
    public enum FileNameMode
    {
        /// <summary>
        /// Keep the original documentation IDs verbatim in file names (including generic arity like <c>`1</c>).
        /// </summary>
        Verbatim,

        /// <summary>
        /// Strip generic arity (e.g., <c>`1</c>) and format generic arguments in a prettier way for file names.
        /// </summary>
        CleanGenerics
    }

    /// <summary>
    /// Options that control how XML documentation is rendered to Markdown.
    /// </summary>
    /// <param name="FileNameMode">How type IDs should be transformed when generating Markdown file names.</param>
    /// <param name="RootNamespaceToTrim">
    /// An optional namespace prefix to remove from displayed type names (e.g., <c>"MyCompany.MyProduct"</c>).
    /// </param>
    /// <param name="CodeBlockLanguage">
    /// The language identifier to use for fenced code blocks (e.g., <c>"csharp"</c>).
    /// </param>
    public sealed record RendererOptions(
        FileNameMode FileNameMode = FileNameMode.Verbatim,
        string? RootNamespaceToTrim = null,    // e.g. "MyCompany.MyProduct"
        string CodeBlockLanguage = "csharp"    // language for fenced code blocks
    );
}
