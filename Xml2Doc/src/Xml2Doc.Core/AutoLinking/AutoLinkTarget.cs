namespace Xml2Doc.Core.AutoLinking;

/// <summary>Describes one unambiguous label and its Markdown destination.</summary>
/// <param name="Label">Visible identifier text to recognize.</param>
/// <param name="Href">Markdown link destination.</param>
public sealed record AutoLinkTarget(string Label, string Href);
