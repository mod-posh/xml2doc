using Shouldly;
using System.Xml.Linq;
using Xml2Doc.Core.Models;
using Xml2Doc.Core.Pipeline;
using Xunit;

namespace Xml2Doc.Tests;

public class SymbolIndexTests
{
    [Fact]
    public void Build_CreatesOrdinallyOrderedTypeSnapshot()
    {
        var model = CreateModel(
            "T:Temp.Zebra",
            "M:Temp.Alpha.Run",
            "T:Temp.Alpha");

        var index = SymbolIndex.Build(model);

        index.Types.Select(type => type.Name)
            .ShouldBe(new[] { "T:Temp.Alpha", "T:Temp.Zebra" });
        index.Members.Count.ShouldBe(3);
    }

    [Fact]
    public void Build_IsUnaffectedByLaterModelChanges()
    {
        var model = CreateModel("T:Temp.Widget");
        var index = SymbolIndex.Build(model);

        model.Members["T:Temp.Added"] = CreateMember("T:Temp.Added");
        model.Members["T:Temp.Widget"].Element
            .Add(new XElement("summary", "Changed."));

        index.ContainsMember("T:Temp.Added").ShouldBeFalse();
        index.Members["T:Temp.Widget"].Element
            .Element("summary").ShouldBeNull();
    }

    [Fact]
    public void Build_SnapshotsReferenceMembersWithoutRenderingThemAsTypes()
    {
        var model = CreateModel("T:Temp.Widget");
        model.ReferenceMembers["T:External.Base"] =
            CreateMember("T:External.Base");

        var index = SymbolIndex.Build(model);

        index.ReferenceMembers.ContainsKey("T:External.Base")
            .ShouldBeTrue();
        index.Types.Select(type => type.Name)
            .ShouldBe(new[] { "T:Temp.Widget" });
    }

    private static Xml2Doc.Core.Models.Xml2Doc CreateModel(
        params string[] documentationIds)
    {
        var model = new Xml2Doc.Core.Models.Xml2Doc();
        foreach (var documentationId in documentationIds)
            model.Members[documentationId] = CreateMember(documentationId);

        return model;
    }

    private static XMember CreateMember(string documentationId) =>
        new(
            documentationId,
            new XElement(
                "member",
                new XAttribute("name", documentationId)));
}
