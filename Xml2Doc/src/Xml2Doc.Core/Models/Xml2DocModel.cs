using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Xml2Doc.Core.Models
{
    /// <summary>
    /// Represents an in-memory model of a .NET XML documentation file.
    /// </summary>
    public sealed class Xml2Doc
    {
        /// <summary>
        /// Gets the collection of documented members keyed by their XML documentation <c>name</c> attribute.
        /// </summary>
        /// <remarks>
        /// Keys are case-sensitive and compared using <see cref="StringComparer.Ordinal"/>.
        /// Examples: <c>T:MyNamespace.MyType</c>, <c>M:MyNamespace.MyType.MyMethod(System.String)</c>.
        /// </remarks>
        public Dictionary<string, XMember> Members { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Loads an XML documentation file and builds the <see cref="Xml2Doc"/> model.
        /// </summary>
        /// <param name="xmlPath">The path to the XML documentation file.</param>
        /// <returns>An <see cref="Xml2Doc"/> instance containing parsed members.</returns>
        /// <remarks>
        /// Exceptions thrown are those of <see cref="XDocument.Load(string, LoadOptions)"/> and file I/O operations
        /// (e.g., file not found, access denied, malformed XML).
        /// </remarks>
        public static Xml2Doc Load(string xmlPath)
        {
            var doc = XDocument.Load(xmlPath, LoadOptions.PreserveWhitespace);
            var model = new Xml2Doc();

            foreach (var m in doc.Descendants("member"))
            {
                var name = (string?)m.Attribute("name");
                if (string.IsNullOrWhiteSpace(name)) continue;

                model.Members[name!] = new XMember(name!, m);
            }

            return model;
        }
    }

    /// <summary>
    /// Represents a single <c>&lt;member&gt;</c> element from an XML documentation file.
    /// </summary>
    /// <param name="Name">The full documentation ID (e.g., <c>M:Namespace.Type.Method(System.String)</c>).</param>
    /// <param name="Element">The underlying XML element for this member.</param>
    public sealed record XMember(string Name, XElement Element)
    {
        /// <summary>
        /// Gets the kind prefix of the documentation ID before the colon.
        /// </summary>
        /// <remarks>
        /// Common values: <c>T</c> (type), <c>M</c> (method), <c>P</c> (property), <c>F</c> (field), <c>E</c> (event), <c>N</c> (namespace).
        /// </remarks>
        public string Kind
        {
            get
            {
                var i = Name.IndexOf(':');
                return i >= 0 ? Name.Substring(0, i) : string.Empty;
            }
        }

        /// <summary>
        /// Gets the identifier portion of the documentation ID after the colon.
        /// </summary>
        /// <example>
        /// For <c>M:MyNamespace.MyType.MyMethod(System.String)</c>, the ID is <c>MyNamespace.MyType.MyMethod(System.String)</c>.
        /// </example>
        public string Id
        {
            get
            {
                var i = Name.IndexOf(':');
                return i >= 0 ? Name.Substring(i + 1) : Name;
            }
        }
    }
}
