using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Xml2Doc.Core.Diagnostics;

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
        /// Gets documented members loaded from reference XML files. These members are
        /// available for inheritance lookup but are not rendered as output pages.
        /// </summary>
        public Dictionary<string, XMember> ReferenceMembers { get; } =
            new(StringComparer.Ordinal);

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
            => Load(xmlPath, diagnosticSink: null);

        /// <summary>
        /// Loads an XML documentation file and reports malformed XML through a diagnostic sink.
        /// </summary>
        /// <param name="xmlPath">The path to the XML documentation file.</param>
        /// <param name="diagnosticSink">Optional receiver for structured diagnostics.</param>
        /// <returns>An <see cref="Xml2Doc"/> instance containing parsed members.</returns>
        public static Xml2Doc Load(
            string xmlPath,
            IDiagnosticSink? diagnosticSink)
        {
            var doc = LoadDocument(xmlPath, diagnosticSink);
            var model = new Xml2Doc();

            AddMembers(model, doc);
            return model;
        }

        /// <summary>
        /// Loads and deterministically merges multiple XML documentation files.
        /// </summary>
        /// <param name="xmlPaths">Paths to participating XML documentation files.</param>
        /// <returns>An aggregate model containing members from every input.</returns>
        /// <exception cref="ArgumentException">No XML documentation paths were supplied.</exception>
        /// <exception cref="InvalidDataException">
        /// Multiple inputs define the same documentation member identifier.
        /// </exception>
        public static Xml2Doc LoadAggregate(IEnumerable<string> xmlPaths)
            => LoadAggregate(xmlPaths, diagnosticSink: null);

        /// <summary>
        /// Loads and deterministically merges multiple XML documentation files,
        /// reporting malformed inputs and conflicting member ownership.
        /// </summary>
        /// <param name="xmlPaths">Paths to participating XML documentation files.</param>
        /// <param name="diagnosticSink">Optional receiver for structured diagnostics.</param>
        /// <returns>An aggregate model containing members from every input.</returns>
        public static Xml2Doc LoadAggregate(
            IEnumerable<string> xmlPaths,
            IDiagnosticSink? diagnosticSink)
        {
            if (xmlPaths is null)
                throw new ArgumentNullException(nameof(xmlPaths));

            var pathComparer = Path.DirectorySeparatorChar == '\\'
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;
            var paths = xmlPaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(Path.GetFullPath)
                .Distinct(pathComparer)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            if (paths.Length == 0)
            {
                throw new ArgumentException(
                    "At least one XML documentation path must be specified.",
                    nameof(xmlPaths));
            }

            var model = new Xml2Doc();
            var memberOwners = new Dictionary<string, string>(
                StringComparer.Ordinal);

            foreach (var path in paths)
            {
                var document = LoadDocument(path, diagnosticSink);
                foreach (var element in document.Descendants("member"))
                {
                    var name = (string?)element.Attribute("name");
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    if (memberOwners.TryGetValue(name!, out var owner))
                    {
                        var message =
                            $"XML documentation member '{name}' is defined by " +
                            $"both '{owner}' and '{path}'.";
                        diagnosticSink?.Report(new Xml2DocDiagnostic(
                            DiagnosticIds.DuplicateInputMember,
                            DiagnosticSeverity.Error,
                            message,
                            MemberId: name,
                            SourcePath: path));
                        throw new InvalidDataException(message);
                    }

                    memberOwners.Add(name!, path);
                    model.Members.Add(name!, new XMember(name!, element));
                }
            }

            return model;
        }

        private static void AddMembers(Xml2Doc model, XDocument document)
        {
            foreach (var m in document.Descendants("member"))
            {
                var name = (string?)m.Attribute("name");
                if (string.IsNullOrWhiteSpace(name)) continue;

                model.Members[name!] = new XMember(name!, m);
            }
        }

        /// <summary>
        /// Loads additional XML documentation for inheritance lookup without adding
        /// referenced types to the set of rendered output pages.
        /// </summary>
        /// <param name="xmlPaths">Reference XML documentation paths.</param>
        public void LoadReferences(IEnumerable<string> xmlPaths)
            => LoadReferences(xmlPaths, diagnosticSink: null);

        /// <summary>
        /// Loads reference XML and reports malformed inputs through a diagnostic sink.
        /// </summary>
        /// <param name="xmlPaths">Reference XML documentation paths.</param>
        /// <param name="diagnosticSink">Optional receiver for structured diagnostics.</param>
        public void LoadReferences(
            IEnumerable<string> xmlPaths,
            IDiagnosticSink? diagnosticSink)
        {
            foreach (var doc in xmlPaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(path => LoadDocument(path, diagnosticSink)))
            {
                foreach (var element in doc.Descendants("member"))
                {
                    var name = (string?)element.Attribute("name");
                    if (string.IsNullOrWhiteSpace(name) || Members.ContainsKey(name!))
                        continue;

                    ReferenceMembers[name!] = new XMember(name!, element);
                }
            }
        }

        private static XDocument LoadDocument(
            string xmlPath,
            IDiagnosticSink? diagnosticSink)
        {
            try
            {
                return XDocument.Load(xmlPath, LoadOptions.PreserveWhitespace);
            }
            catch (XmlException exception)
            {
                diagnosticSink?.Report(new Xml2DocDiagnostic(
                    DiagnosticIds.MalformedXml,
                    DiagnosticSeverity.Error,
                    $"Unable to parse XML documentation '{xmlPath}': {exception.Message}",
                    SourcePath: xmlPath,
                    LineNumber: exception.LineNumber,
                    LinePosition: exception.LinePosition));
                throw;
            }
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
