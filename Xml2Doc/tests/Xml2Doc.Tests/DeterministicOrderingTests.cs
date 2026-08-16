using Shouldly;
using System.Globalization;
using System.Xml.Linq;
using Xml2Doc.Core;
using Xml2Doc.Core.Models;
using Xunit;

namespace Xml2Doc.Tests;

public class DeterministicOrderingTests
{
    [Fact]
    public void Render_MemberOrderingDoesNotDependOnCurrentCulture()
    {
        var model = new Xml2Doc.Core.Models.Xml2Doc();
        AddMember(model, "T:Temp.Widget", "Widget.");
        AddMember(model, "M:Temp.Widget.Zebra", "Zebra.");
        AddMember(model, "M:Temp.Widget.Äther", "Aether.");

        var german = RenderWithCulture(model, "de-DE");
        var swedish = RenderWithCulture(model, "sv-SE");

        german.ShouldBe(swedish);
        german.IndexOf("Zebra.", StringComparison.Ordinal)
            .ShouldBeLessThan(
                german.IndexOf("Aether.", StringComparison.Ordinal));
    }

    private static string RenderWithCulture(
        Xml2Doc.Core.Models.Xml2Doc model,
        string cultureName)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            return new MarkdownRenderer(model).RenderToString();
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    private static void AddMember(
        Xml2Doc.Core.Models.Xml2Doc model,
        string documentationId,
        string summary)
    {
        model.Members[documentationId] = new XMember(
            documentationId,
            new XElement(
                "member",
                new XAttribute("name", documentationId),
                new XElement("summary", summary)));
    }
}
