namespace Xml2Doc.Core.Paths;

/// <summary>Associates one logical document with its validated output path.</summary>
/// <param name="Document">Immutable document identity and metadata.</param>
/// <param name="Path">Canonical output-root-relative logical path using <c>/</c>.</param>
public sealed record DocumentPlanEntry(
    DocumentDescriptor Document,
    string Path);
