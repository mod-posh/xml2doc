using Shouldly;
using System.Xml.Linq;
using Xml2Doc.Core;
using Xml2Doc.Core.Models;
using Xml2Doc.Core.Signatures;
using Xunit;

namespace Xml2Doc.Tests;

public class SignatureRendererTests
{
    [Fact]
    public void DefaultStyle_PreservesNestedGenericSignature()
    {
        var member = Member(
            "M:Temp.Nested.Transform``2(System.Collections.Generic.List{System.Collections.Generic.Dictionary{``0,System.Collections.Generic.List{``1}}})");

        var result = new DefaultSignatureRenderer()
            .RenderMemberHeader(member, SignatureStyle.Default);

        result.ShouldBe(
            "Method: Transform<T1,T2>(List<Dictionary<T1, List<T2>>>)");
    }

    [Fact]
    public void DetailedStyle_RendersNamesDefaultsModifiersAndConstraints()
    {
        var member = Member(
            "M:Temp.Factory.Create``1(System.Int32,System.String[],``0@)",
            new XElement("typeparam",
                new XAttribute("name", "T"),
                new XAttribute("constraint", "class, new()")),
            new XElement("param",
                new XAttribute("name", "count"),
                new XAttribute("default", "42")),
            new XElement("param",
                new XAttribute("name", "values"),
                new XAttribute("modifier", "params")),
            new XElement("param",
                new XAttribute("name", "result")));
        var style = new SignatureStyle(
            IncludeParamNames: true,
            IncludeConstraints: true,
            IncludeDefaultValues: true);

        var result = new DefaultSignatureRenderer()
            .RenderMemberHeader(member, style);

        result.ShouldBe(
            "Method: Create<T>(int count = 42, params string[] values, ref T result) where T : class, new()");
    }

    [Fact]
    public void ParameterNames_UseCSharpIndexerSyntax()
    {
        var member = Member(
            "P:Temp.Collection.Item(System.Int32)",
            new XElement("param", new XAttribute("name", "index")));

        var result = new DefaultSignatureRenderer().RenderMemberHeader(
            member,
            new SignatureStyle(IncludeParamNames: true));

        result.ShouldBe("Property: this[int index]");
    }

    [Fact]
    public void DefaultStyle_PreservesIndexerCompatibility()
    {
        var member = Member(
            "P:Temp.Collection.Item(System.Int32)",
            new XElement("param", new XAttribute("name", "index")));

        new DefaultSignatureRenderer()
            .RenderMemberHeader(member, SignatureStyle.Default)
            .ShouldBe("Property: Item(int)");
    }

    [Fact]
    public void Renderer_UsesCustomSignatureService()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, """
            <doc><members>
              <member name="T:Temp.Widget">
                <summary>See <see cref="T:Temp.Other"/>.</summary>
              </member>
              <member name="M:Temp.Widget.Run(System.String)">
                <summary>Runs.</summary>
              </member>
            </members></doc>
            """);

        try
        {
            var model = Xml2Doc.Core.Models.Xml2Doc.Load(path);
            var markdown = new MarkdownRenderer(
                model,
                new RendererOptions(SignatureRenderer: new PrefixSignatureRenderer()))
                .RenderToString();

            markdown.ShouldContain("# type:Temp.Widget");
            markdown.ShouldContain("## member:M:Temp.Widget.Run(System.String)");
            markdown.ShouldContain("[cref:T:Temp.Other]");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Renderer_AppliesConfiguredSignatureStyle()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, """
            <doc><members>
              <member name="T:Temp.Collection"><summary>A collection.</summary></member>
              <member name="P:Temp.Collection.Item(System.Int32)">
                <summary>Gets an item.</summary>
                <param name="index">The index.</param>
              </member>
            </members></doc>
            """);

        try
        {
            var model = Xml2Doc.Core.Models.Xml2Doc.Load(path);
            var markdown = new MarkdownRenderer(
                model,
                new RendererOptions(
                    SignatureStyle: new SignatureStyle(
                        IncludeParamNames: true)))
                .RenderToString();

            markdown.ShouldContain("## Property: this[int index]");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static XMember Member(string name, params XElement[] children)
    {
        var element = new XElement("member", new XAttribute("name", name));
        element.Add(children);
        return new XMember(name, element);
    }

    private sealed class PrefixSignatureRenderer : ISignatureRenderer
    {
        public string RenderTypeName(string typeId) => "type:" + typeId;

        public string RenderMemberHeader(XMember member, SignatureStyle style) =>
            "member:" + member.Name;

        public string RenderCrefLabel(string cref) => "cref:" + cref;
    }
}
