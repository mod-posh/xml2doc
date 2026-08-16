namespace Xml2Doc.Core.Diagnostics;

/// <summary>Describes one structured Xml2Doc diagnostic.</summary>
/// <param name="Code">Stable diagnostic identifier.</param>
/// <param name="Severity">Diagnostic severity.</param>
/// <param name="Message">Human-readable diagnostic message.</param>
/// <param name="MemberId">Optional XML documentation member identifier.</param>
/// <param name="SourcePath">Optional source XML path.</param>
/// <param name="LineNumber">Optional one-based source line.</param>
/// <param name="LinePosition">Optional one-based source column.</param>
public sealed record Xml2DocDiagnostic(
    string Code,
    DiagnosticSeverity Severity,
    string Message,
    string? MemberId = null,
    string? SourcePath = null,
    int? LineNumber = null,
    int? LinePosition = null);
