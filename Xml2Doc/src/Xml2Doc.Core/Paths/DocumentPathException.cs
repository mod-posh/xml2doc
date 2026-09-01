namespace Xml2Doc.Core.Paths;

/// <summary>Represents a deterministic document-path planning failure.</summary>
public sealed class DocumentPathException : InvalidOperationException
{
    /// <summary>Creates a document-path failure with a stable diagnostic code.</summary>
    public DocumentPathException(string diagnosticCode, string message)
        : base(message)
    {
        DiagnosticCode = diagnosticCode;
    }

    /// <summary>Gets the stable diagnostic identifier for this failure.</summary>
    public string DiagnosticCode { get; }
}
