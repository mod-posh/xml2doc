namespace Xml2Doc.Core.Diagnostics;

/// <summary>Identifies the impact of an Xml2Doc diagnostic.</summary>
public enum DiagnosticSeverity
{
    /// <summary>Informational context that does not require action.</summary>
    Info,

    /// <summary>A non-fatal condition that may require attention.</summary>
    Warning,

    /// <summary>A condition that prevents successful processing.</summary>
    Error
}
