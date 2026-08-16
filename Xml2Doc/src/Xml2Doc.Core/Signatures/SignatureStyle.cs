namespace Xml2Doc.Core.Signatures;

/// <summary>Controls optional detail in rendered member signatures.</summary>
/// <param name="IncludeParamNames">Include documented parameter names.</param>
/// <param name="IncludeConstraints">Include available generic parameter constraints.</param>
/// <param name="IncludeDefaultValues">Include available parameter default values.</param>
public sealed record SignatureStyle(
    bool IncludeParamNames = false,
    bool IncludeConstraints = false,
    bool IncludeDefaultValues = false)
{
    /// <summary>Compatibility style matching Xml2Doc's existing output.</summary>
    public static SignatureStyle Default { get; } = new();
}
