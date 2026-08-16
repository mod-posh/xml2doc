namespace Xml2Doc.Core.Aliasing
{
    /// <summary>
    /// Applies display aliases to type tokens used in signatures, labels, and anchors.
    /// </summary>
    public interface IAliasProvider
    {
        /// <summary>
        /// Applies aliases to complete type tokens without modifying partial identifiers.
        /// </summary>
        /// <param name="value">The signature, label, or documentation identifier to transform.</param>
        /// <returns>The transformed value.</returns>
        string ApplyAliases(string value);
    }
}
