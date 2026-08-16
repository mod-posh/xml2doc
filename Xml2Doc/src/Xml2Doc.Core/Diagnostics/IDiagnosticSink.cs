namespace Xml2Doc.Core.Diagnostics;

/// <summary>Receives structured diagnostics emitted while loading and rendering documentation.</summary>
public interface IDiagnosticSink
{
    /// <summary>Reports one diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic to report.</param>
    void Report(Xml2DocDiagnostic diagnostic);
}
