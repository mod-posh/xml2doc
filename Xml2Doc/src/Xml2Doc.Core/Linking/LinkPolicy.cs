namespace Xml2Doc.Core.Linking;

/// <summary>Controls how unresolved XML documentation links are routed.</summary>
public enum LinkPolicy
{
    /// <summary>Preserve the existing internal-link behavior for every cref.</summary>
    InternalOnly,

    /// <summary>
    /// Prefer a configured external resolver when the cref is not part of the
    /// rendered documentation model.
    /// </summary>
    PreferExternalForUnknown
}
