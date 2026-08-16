using System;
using System.IO;
using Xml2Doc.Core.Diagnostics;

namespace Xml2Doc.Cli;

internal sealed class CliDiagnosticSink : IDiagnosticSink
{
    private readonly TextWriter _writer;
    private readonly object _gate = new();
    private bool _hasErrors;

    public CliDiagnosticSink(TextWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public bool HasErrors
    {
        get
        {
            lock (_gate)
                return _hasErrors;
        }
    }

    public void Report(Xml2DocDiagnostic diagnostic)
    {
        if (diagnostic is null)
            throw new ArgumentNullException(nameof(diagnostic));

        lock (_gate)
        {
            if (diagnostic.Severity == DiagnosticSeverity.Error)
                _hasErrors = true;

            _writer.WriteLine(Format(diagnostic));
        }
    }

    private static string Format(Xml2DocDiagnostic diagnostic)
    {
        var location = FormatLocation(diagnostic);
        var member = string.IsNullOrWhiteSpace(diagnostic.MemberId)
            ? string.Empty
            : $" [member: {diagnostic.MemberId}]";
        return $"{location}xml2doc {diagnostic.Severity.ToString().ToLowerInvariant()} " +
            $"{diagnostic.Code}: {diagnostic.Message}{member}";
    }

    private static string FormatLocation(Xml2DocDiagnostic diagnostic)
    {
        if (string.IsNullOrWhiteSpace(diagnostic.SourcePath))
            return string.Empty;

        if (diagnostic.LineNumber is null)
            return diagnostic.SourcePath + ": ";

        var position = diagnostic.LinePosition is null
            ? diagnostic.LineNumber.Value.ToString()
            : $"{diagnostic.LineNumber.Value},{diagnostic.LinePosition.Value}";
        return $"{diagnostic.SourcePath}({position}): ";
    }
}
