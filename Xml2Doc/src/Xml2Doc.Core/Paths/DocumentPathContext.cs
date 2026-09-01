namespace Xml2Doc.Core.Paths;

/// <summary>Provides deterministic inputs to a document path resolver.</summary>
/// <param name="Document">The logical document being placed.</param>
/// <param name="DefaultPath">The backward-compatible flat logical path.</param>
/// <param name="FileName">The deterministic filename for the document.</param>
public sealed record DocumentPathContext(
    DocumentDescriptor Document,
    string DefaultPath,
    string FileName);
