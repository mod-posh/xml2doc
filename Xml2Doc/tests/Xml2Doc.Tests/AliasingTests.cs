using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xml2Doc.Core;
using Xml2Doc.Core.Aliasing;
using Xml2Doc.Core.Anchoring;
using Xml2Doc.Core.Templates;
using Xml2Doc.Core.AutoLinking;
using Xml2Doc.Core.Linking;
using Xml2Doc.Core.Signatures;
using Xml2Doc.Core.Diagnostics;
using Xml2Doc.Sample;
using Xunit;

public class AliasingTests
{
    // Resolve project directory from the test's bin folder
    private static string ProjectDir =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

    [Fact]
    public void DefaultProvider_AppliesOnlyCompleteTypeTokens()
    {
        DefaultAliasProvider.Instance
            .ApplyAliases("System.String StringComparer Int32 System.UInt32")
            .ShouldBe("string StringComparer int uint");
    }

    [Fact]
    public void RendererOptions_PreservesPreviousConstructorSignature()
    {
        var parameterTypes = new[]
        {
            typeof(FileNameMode),
            typeof(string),
            typeof(string),
            typeof(bool),
            typeof(AnchorAlgorithm),
            typeof(string),
            typeof(string),
            typeof(bool),
            typeof(string),
            typeof(string),
            typeof(bool),
            typeof(bool),
            typeof(bool),
            typeof(int?),
            typeof(bool),
            typeof(bool),
            typeof(string),
            typeof(LineEndingStyle),
            typeof(Action<string>)
        };

        typeof(RendererOptions).GetConstructor(parameterTypes).ShouldNotBeNull();
        var aliasProviderSignature =
            parameterTypes.Concat(new[] { typeof(IAliasProvider) }).ToArray();
        typeof(RendererOptions).GetConstructor(aliasProviderSignature).ShouldNotBeNull();

        var anchorGeneratorSignature =
            aliasProviderSignature.Concat(new[] { typeof(IAnchorGenerator) }).ToArray();
        typeof(RendererOptions).GetConstructor(anchorGeneratorSignature).ShouldNotBeNull();

        typeof(RendererOptions)
            .GetConstructor(
                anchorGeneratorSignature
                    .Concat(new[] { typeof(ITemplateRenderer) })
                    .ToArray())
            .ShouldNotBeNull();

        typeof(RendererOptions)
            .GetConstructor(
                anchorGeneratorSignature
                    .Concat(new[]
                    {
                        typeof(ITemplateRenderer),
                        typeof(Func<
                            TemplateRenderContext,
                            IReadOnlyDictionary<string, object?>>),
                        typeof(IAutoLinker),
                        typeof(LinkPolicy),
                        typeof(IExternalSymbolResolver),
                        typeof(SignatureStyle),
                        typeof(ISignatureRenderer),
                        typeof(IDiagnosticSink)
                    })
                    .ToArray())
            .ShouldNotBeNull();

        typeof(RendererOptions)
            .GetConstructor(
                anchorGeneratorSignature
                    .Concat(new[]
                    {
                        typeof(ITemplateRenderer),
                        typeof(Func<
                            TemplateRenderContext,
                            IReadOnlyDictionary<string, object?>>),
                        typeof(IAutoLinker),
                        typeof(LinkPolicy),
                        typeof(IExternalSymbolResolver),
                        typeof(SignatureStyle),
                        typeof(ISignatureRenderer)
                    })
                    .ToArray())
            .ShouldNotBeNull();

        typeof(RendererOptions)
            .GetConstructor(
                anchorGeneratorSignature
                    .Concat(new[]
                    {
                        typeof(ITemplateRenderer),
                        typeof(Func<
                            TemplateRenderContext,
                            IReadOnlyDictionary<string, object?>>),
                        typeof(IAutoLinker),
                        typeof(LinkPolicy),
                        typeof(IExternalSymbolResolver)
                    })
                    .ToArray())
            .ShouldNotBeNull();

        typeof(RendererOptions)
            .GetConstructor(
                anchorGeneratorSignature
                    .Concat(new[]
                    {
                        typeof(ITemplateRenderer),
                        typeof(Func<
                            TemplateRenderContext,
                            IReadOnlyDictionary<string, object?>>),
                        typeof(IAutoLinker)
                    })
                    .ToArray())
            .ShouldNotBeNull();

        typeof(RendererOptions)
            .GetConstructor(
                anchorGeneratorSignature
                    .Concat(new[]
                    {
                        typeof(ITemplateRenderer),
                        typeof(Func<
                            TemplateRenderContext,
                            IReadOnlyDictionary<string, object?>>)
                    })
                    .ToArray())
            .ShouldNotBeNull();
    }

    [Fact]
    public void CustomProvider_IsUsedForSignaturesLinksAndAnchors()
    {
        var xml = """
            <doc><members>
              <member name="T:Temp.Widget">
                <summary>Calls <see cref="M:Temp.Widget.Run(System.Int32)"/>.</summary>
              </member>
              <member name="M:Temp.Widget.Run(System.Int32)">
                <summary>Runs the widget.</summary>
                <param name="value">Input value.</param>
              </member>
            </members></doc>
            """;
        var xmlPath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(xmlPath, xml);
            var model = Xml2Doc.Core.Models.Xml2Doc.Load(xmlPath);
            var renderer = new MarkdownRenderer(
                model,
                new RendererOptions(AliasProvider: new IntegerAliasProvider()));

            var markdown = renderer.RenderToString();

            markdown.ShouldContain("[Run(integer)](Temp.Widget.md#temp.widget.run(integer))");
            markdown.ShouldContain("## Method: Run(integer)");
            markdown.ShouldContain("<a id=\"temp.widget.run(integer)\"></a>");
        }
        finally
        {
            File.Delete(xmlPath);
        }
    }

    [Fact]
    public async Task TokenAwareAliasing_DoesNotCorruptIdentifiers()
    {
        // Build sample XML
        var xml = Path.ChangeExtension(typeof(AliasingPlayground).Assembly.Location, ".xml");
        File.Exists(xml).ShouldBeTrue($"Missing XML: {xml}");

        var model = Xml2Doc.Core.Models.Xml2Doc.Load(xml);
        var options = new RendererOptions(
            FileNameMode: FileNameMode.CleanGenerics,
            RootNamespaceToTrim: "Xml2Doc.Sample",
            CodeBlockLanguage: "csharp"
        );
        var renderer = new MarkdownRenderer(model, options);

        // Render per-type and read the AliasingPlayground page
        var outDir = Path.Combine(Path.GetTempPath(), "Xml2Doc.Tests", Path.GetRandomFileName());
        Directory.CreateDirectory(outDir);
        renderer.RenderToDirectory(outDir);

        var mdPath = Path.Combine(outDir, "Xml2Doc.Sample.AliasingPlayground.md");
        File.Exists(mdPath).ShouldBeTrue($"Missing generated page: {mdPath}");

        var md = await File.ReadAllTextAsync(mdPath);
        md = md.Replace("\r\n", "\n");

        // Sanity: page header
        md.ShouldContain("# AliasingPlayground");

        // Remove explicit anchors from consideration to avoid false-positives on lowercased ids
        var mdNoAnchors = Regex.Replace(md, "<a id=\"[^\"]+\"></a>\\s*", "", RegexOptions.IgnoreCase);

        // 1) Ensure we did NOT corrupt identifiers containing "String" in visible text
        // Expect the method header to show the un-aliased BCL identifier: StringComparer
        Regex.IsMatch(mdNoAnchors, @"(?im)^##\s+Method:\s*UseComparer\s*\(\s*StringComparer\s*\)")
            .ShouldBeTrue("Expected visible header 'Method: UseComparer(StringComparer)'.");

        // Ensure the lowercase variant does NOT appear (case-sensitive check)
        mdNoAnchors.IndexOf("stringComparer", System.StringComparison.Ordinal).ShouldBe(-1);
        mdNoAnchors.IndexOf("System.stringComparer", System.StringComparison.Ordinal).ShouldBe(-1);

        // 2) Ensure true tokens were aliased as expected in the Mix signature
        // Expect "Method: Mix(string, int, uint)" somewhere (header or bullet)
        var pattern = @"(?im)Method:\s*Mix\s*\(\s*string\s*,\s*int\s*,\s*uint\s*\)";
        Regex.IsMatch(mdNoAnchors, pattern).ShouldBeTrue(
            "Expected Mix(string, int, uint) signature (header or bullet) with aliases applied."
        );
    }

    private sealed class IntegerAliasProvider : IAliasProvider
    {
        public string ApplyAliases(string value) =>
            Regex.Replace(
                value,
                @"(?<![A-Za-z0-9_])(?:System\.Int32|Int32)(?![A-Za-z0-9_])",
                "integer");
    }
}
