using Shouldly;
using System.Xml;
using Xml2Doc.Core;
using Xml2Doc.Core.Anchoring;
using Xml2Doc.Core.Diagnostics;
using Xml2Doc.Core.Linking;
using Xunit;

namespace Xml2Doc.Tests;

public class StructuredDiagnosticTests
{
    [Fact]
    public void Render_ReportsUnresolvedCrefOnce()
    {
        var sink = new RecordingDiagnosticSink();
        var renderer = CreateRenderer(
            """
            <doc><members>
              <member name="T:Temp.Consumer">
                <summary>
                  See <see cref="T:Missing.Widget"/> and
                  <see cref="T:Missing.Widget"/>.
                </summary>
              </member>
            </members></doc>
            """,
            new RendererOptions(DiagnosticSink: sink));

        renderer.RenderToString();

        var diagnostic = sink.Diagnostics.Single();
        diagnostic.Code.ShouldBe(DiagnosticIds.UnresolvedCref);
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);
        diagnostic.MemberId.ShouldBe("T:Missing.Widget");
    }

    [Fact]
    public void Render_DoesNotReportCrefResolvedExternally()
    {
        var sink = new RecordingDiagnosticSink();
        var renderer = CreateRenderer(
            """
            <doc><members>
              <member name="T:Temp.Consumer">
                <summary>See <see cref="T:Missing.Widget"/>.</summary>
              </member>
            </members></doc>
            """,
            new RendererOptions(
                LinkPolicy: LinkPolicy.PreferExternalForUnknown,
                ExternalSymbolResolver: new AcceptingExternalResolver(),
                DiagnosticSink: sink));

        renderer.RenderToString();

        sink.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void Render_ReportsMissingTypeAndMemberSummaries()
    {
        var sink = new RecordingDiagnosticSink();
        var renderer = CreateRenderer(
            """
            <doc><members>
              <member name="T:Temp.Widget" />
              <member name="M:Temp.Widget.Run" />
            </members></doc>
            """,
            new RendererOptions(DiagnosticSink: sink));

        renderer.RenderToString();

        sink.Diagnostics
            .Where(diagnostic => diagnostic.Code == DiagnosticIds.MissingSummary)
            .Select(diagnostic => diagnostic.MemberId!)
            .OrderBy(memberId => memberId, StringComparer.Ordinal)
            .ShouldBe(new[] { "M:Temp.Widget.Run", "T:Temp.Widget" });
    }

    [Fact]
    public void Render_ReportsDuplicateMemberAnchors()
    {
        var sink = new RecordingDiagnosticSink();
        var renderer = CreateRenderer(
            """
            <doc><members>
              <member name="T:Temp.Widget"><summary>A widget.</summary></member>
              <member name="M:Temp.Widget.First"><summary>First.</summary></member>
              <member name="M:Temp.Widget.Second"><summary>Second.</summary></member>
            </members></doc>
            """,
            new RendererOptions(
                AnchorGenerator: new ConstantMemberAnchorGenerator(),
                DiagnosticSink: sink));

        renderer.RenderToString();

        var diagnostic = sink.Diagnostics
            .Single(item => item.Code == DiagnosticIds.DuplicateAnchor);
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);
        diagnostic.Message.ShouldContain("M:Temp.Widget.First");
        diagnostic.Message.ShouldContain("M:Temp.Widget.Second");
    }

    [Fact]
    public void Load_WhenXmlIsMalformed_ReportsErrorAndRethrows()
    {
        var path = Path.GetTempFileName();
        var sink = new RecordingDiagnosticSink();
        File.WriteAllText(path, "<doc><members></doc>");

        try
        {
            Should.Throw<XmlException>(() =>
                Xml2Doc.Core.Models.Xml2Doc.Load(path, sink));

            var diagnostic = sink.Diagnostics.Single();
            diagnostic.Code.ShouldBe(DiagnosticIds.MalformedXml);
            diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
            diagnostic.SourcePath.ShouldBe(path);
            diagnostic.LineNumber.ShouldNotBeNull();
            diagnostic.LinePosition.ShouldNotBeNull();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void UnresolvedInheritDoc_ReportsStructuredAndLegacyWarnings()
    {
        var sink = new RecordingDiagnosticSink();
        var warnings = new List<string>();
        var renderer = CreateRenderer(
            """
            <doc><members>
              <member name="T:Temp.Widget"><summary>A widget.</summary></member>
              <member name="M:Temp.Widget.Run">
                <inheritdoc cref="M:Missing.Widget.Run" />
              </member>
            </members></doc>
            """,
            new RendererOptions(
                WarningSink: warnings.Add,
                DiagnosticSink: sink));

        renderer.RenderToString();

        sink.Diagnostics.ShouldContain(diagnostic =>
            diagnostic.Code == DiagnosticIds.UnresolvedInheritDoc &&
            diagnostic.MemberId == "M:Temp.Widget.Run");
        warnings.Single()
            .ShouldContain("M:Temp.Widget.Run");
    }

    private static MarkdownRenderer CreateRenderer(
        string xml,
        RendererOptions options)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, xml);

        try
        {
            return new MarkdownRenderer(
                Xml2Doc.Core.Models.Xml2Doc.Load(path),
                options);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class RecordingDiagnosticSink : IDiagnosticSink
    {
        public List<Xml2DocDiagnostic> Diagnostics { get; } = new();

        public void Report(Xml2DocDiagnostic diagnostic) =>
            Diagnostics.Add(diagnostic);
    }

    private sealed class ConstantMemberAnchorGenerator : IAnchorGenerator
    {
        public string GenerateHeadingAnchor(string heading) =>
            "heading-" + heading.ToLowerInvariant();

        public string GenerateMemberAnchor(string memberId) => "duplicate";
    }

    private sealed class AcceptingExternalResolver : IExternalSymbolResolver
    {
        public bool TryResolve(string cref, out string? href)
        {
            href = "https://docs.example/" + cref.Substring(2);
            return true;
        }
    }
}
