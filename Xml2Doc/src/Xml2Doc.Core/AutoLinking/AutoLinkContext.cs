namespace Xml2Doc.Core.AutoLinking;

/// <summary>Provides the known, mode-specific symbols available for auto-linking.</summary>
/// <param name="Targets">Unambiguous link targets ordered by label specificity.</param>
public sealed record AutoLinkContext(IReadOnlyList<AutoLinkTarget> Targets);
