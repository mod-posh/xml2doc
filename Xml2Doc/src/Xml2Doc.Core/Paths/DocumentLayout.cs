namespace Xml2Doc.Core.Paths;

/// <summary>Selects a built-in multi-document output layout.</summary>
public enum DocumentLayout
{
    /// <summary>Preserves the existing flat type-file layout.</summary>
    Flat = 0,

    /// <summary>Places type and namespace documents in hierarchical namespace directories.</summary>
    NamespaceFolders = 1
}
