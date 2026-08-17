using Shouldly;
using Xml2Doc.Core;
using Xml2Doc.Core.Diagnostics;
using Xunit;

namespace Xml2Doc.Tests;

public class MultiProjectAggregationTests
{
    [Fact]
    public void Load_MultipleInputsRendersOneCanonicalIndex()
    {
        using var workspace = TemporaryWorkspace.Create();
        var zebra = workspace.WriteXml("Zebra.xml", "T:Project.Zebra");
        var alpha = workspace.WriteXml("Alpha.xml", "T:Project.Alpha");
        var output = workspace.FullPath("docs");

        var model = Xml2Doc.Core.Models.Xml2Doc.LoadAggregate(
            new[] { zebra, alpha });
        new MarkdownRenderer(model).RenderToDirectory(output);

        model.Members.Keys.OrderBy(key => key, StringComparer.Ordinal)
            .ShouldBe(new[] { "T:Project.Alpha", "T:Project.Zebra" });
        var index = File.ReadAllText(Path.Join(output, "index.md"));
        index.ShouldContain("Project.Alpha");
        index.ShouldContain("Project.Zebra");
        index.IndexOf("Project.Alpha", StringComparison.Ordinal)
            .ShouldBeLessThan(index.IndexOf(
                "Project.Zebra",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Load_InputOrderDoesNotChangeGeneratedOutput()
    {
        using var workspace = TemporaryWorkspace.Create();
        var zebra = workspace.WriteXml("Zebra.xml", "T:Project.Zebra");
        var alpha = workspace.WriteXml("Alpha.xml", "T:Project.Alpha");
        var forwardOutput = workspace.FullPath("forward");
        var reverseOutput = workspace.FullPath("reverse");

        new MarkdownRenderer(Xml2Doc.Core.Models.Xml2Doc.LoadAggregate(
            new[] { alpha, zebra })).RenderToDirectory(forwardOutput);
        new MarkdownRenderer(Xml2Doc.Core.Models.Xml2Doc.LoadAggregate(
            new[] { zebra, alpha })).RenderToDirectory(reverseOutput);

        var forwardFiles = Directory.GetFiles(forwardOutput)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();
        var reverseFiles = Directory.GetFiles(reverseOutput)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();

        reverseFiles.Select(Path.GetFileName)
            .ShouldBe(forwardFiles.Select(Path.GetFileName));
        for (var index = 0; index < forwardFiles.Length; index++)
        {
            File.ReadAllBytes(reverseFiles[index])
                .ShouldBe(File.ReadAllBytes(forwardFiles[index]));
        }
    }

    [Fact]
    public void Load_DuplicateMemberOwnershipReportsErrorAndFails()
    {
        using var workspace = TemporaryWorkspace.Create();
        var first = workspace.WriteXml("First.xml", "T:Shared.Widget");
        var second = workspace.WriteXml("Second.xml", "T:Shared.Widget");
        var sink = new RecordingDiagnosticSink();

        var exception = Should.Throw<InvalidDataException>(() =>
            Xml2Doc.Core.Models.Xml2Doc.LoadAggregate(
                new[] { second, first },
                sink));

        exception.Message.ShouldContain(first);
        exception.Message.ShouldContain(second);
        var diagnostic = sink.Diagnostics.Single();
        diagnostic.Code.ShouldBe(DiagnosticIds.DuplicateInputMember);
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
        diagnostic.MemberId.ShouldBe("T:Shared.Widget");
        diagnostic.SourcePath.ShouldBe(second);
    }

    [Fact]
    public void Load_DuplicateMemberWithinOneInputReportsClearErrorAndFails()
    {
        using var workspace = TemporaryWorkspace.Create();
        var input = workspace.WriteXml(
            "Duplicate.xml",
            "T:Shared.Widget",
            "T:Shared.Widget");
        var sink = new RecordingDiagnosticSink();

        var exception = Should.Throw<InvalidDataException>(() =>
            Xml2Doc.Core.Models.Xml2Doc.LoadAggregate(
                new[] { input },
                sink));

        exception.Message.ShouldBe(
            $"XML documentation member 'T:Shared.Widget' is defined more than once in '{input}'.");
        var diagnostic = sink.Diagnostics.Single();
        diagnostic.Code.ShouldBe(DiagnosticIds.DuplicateInputMember);
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
        diagnostic.MemberId.ShouldBe("T:Shared.Widget");
        diagnostic.SourcePath.ShouldBe(input);
    }

    private sealed class RecordingDiagnosticSink : IDiagnosticSink
    {
        public List<Xml2DocDiagnostic> Diagnostics { get; } = new();

        public void Report(Xml2DocDiagnostic diagnostic) =>
            Diagnostics.Add(diagnostic);
    }

    private sealed class TemporaryWorkspace : IDisposable
    {
        private TemporaryWorkspace(string path) => Path = path;

        public string Path { get; }

        public static TemporaryWorkspace Create() =>
            new(System.IO.Path.Join(
                System.IO.Path.GetTempPath(),
                "Xml2Doc.Tests",
                Guid.NewGuid().ToString("N")));

        public string FullPath(string relativePath) =>
            System.IO.Path.Join(Path, relativePath);

        public string WriteXml(string fileName, params string[] memberIds)
        {
            Directory.CreateDirectory(Path);
            var path = FullPath(fileName);
            var members = string.Concat(memberIds.Select(memberId =>
                $"<member name=\"{memberId}\">" +
                "<summary>Documented type.</summary>" +
                "</member>"));
            File.WriteAllText(
                path,
                $"<doc><members>{members}</members></doc>");
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
