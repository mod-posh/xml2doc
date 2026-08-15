using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xml2Doc.Core;
using Xunit;

public class InheritDocResolverTests
{
    [Fact]
    public async Task Render_UsesReferenceXmlForInheritanceWithoutRenderingReferenceTypes()
    {
        var primaryXml = """
                <?xml version="1.0"?>
                <doc>
                  <members>
                    <member name="T:Consumer.ExampleService"><summary>Implementation.</summary></member>
                    <member name="M:Consumer.ExampleService.Run(Contracts.Request)">
                      <inheritdoc cref="M:Contracts.IExampleService.Run(Contracts.Request)"/>
                    </member>
                    <member name="M:Consumer.ExampleService.Save(Contracts.Request)"><inheritdoc/></member>
                  </members>
                </doc>
                """;
        var referenceXml = """
                <?xml version="1.0"?>
                <doc>
                  <members>
                    <member name="T:Contracts.IExampleService"><summary>Contract.</summary></member>
                    <member name="M:Contracts.IExampleService.Run(Contracts.Request)">
                      <summary>Runs from referenced documentation.</summary>
                    </member>
                    <member name="M:Contracts.IExampleService.Save(Contracts.Request)">
                      <summary>Saves from referenced documentation.</summary>
                    </member>
                  </members>
                </doc>
                """;

        var tmpDir = Path.Join(
            Path.GetTempPath(),
            "Xml2Doc.Tests",
            Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);

        try
        {
            var primaryPath = Path.Join(tmpDir, "Consumer.xml");
            var referencePath = Path.Join(tmpDir, "Contracts.xml");
            await File.WriteAllTextAsync(primaryPath, primaryXml, new UTF8Encoding(false));
            await File.WriteAllTextAsync(referencePath, referenceXml, new UTF8Encoding(false));

            var warnings = new List<string>();
            var model = Xml2Doc.Core.Models.Xml2Doc.Load(primaryPath);
            model.LoadReferences(new[] { referencePath });
            var renderer = new MarkdownRenderer(
                model,
                new RendererOptions(
                    FileNameMode: FileNameMode.CleanGenerics,
                    WarningSink: warnings.Add));
            var outDir = Path.Join(tmpDir, "out");

            renderer.RenderToDirectory(outDir);

            var implementation = await File.ReadAllTextAsync(
                Path.Join(outDir, "Consumer.ExampleService.md"));
            implementation.ShouldContain("Runs from referenced documentation.");
            implementation.ShouldContain("Saves from referenced documentation.");
            File.Exists(Path.Join(outDir, "Contracts.IExampleService.md"))
                .ShouldBeFalse();
            warnings.ShouldBeEmpty();
        }
        finally
        {
            if (Directory.Exists(tmpDir))
                Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public async Task Render_WhenInheritDocCannotBeResolved_EmitsWarning()
    {
        var xml = """
                <?xml version="1.0"?>
                <doc><members>
                  <member name="T:Temp.Service"><summary>Service.</summary></member>
                  <member name="M:Temp.Service.Run"><inheritdoc cref="M:Missing.Contract.Run"/></member>
                </members></doc>
                """;
        var tmpDir = Path.Join(
            Path.GetTempPath(),
            "Xml2Doc.Tests",
            Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);

        try
        {
            var xmlPath = Path.Join(tmpDir, "Temp.xml");
            await File.WriteAllTextAsync(xmlPath, xml, new UTF8Encoding(false));
            var warnings = new List<string>();
            var renderer = new MarkdownRenderer(
                Xml2Doc.Core.Models.Xml2Doc.Load(xmlPath),
                new RendererOptions(WarningSink: warnings.Add));

            renderer.RenderToDirectory(Path.Join(tmpDir, "out"));

            warnings.Count.ShouldBe(1);
            warnings[0].ShouldContain("M:Temp.Service.Run");
        }
        finally
        {
            if (Directory.Exists(tmpDir))
                Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public async Task Render_ResolvesUniqueFullSignatureAndExplicitCref_ButNotAmbiguousMatches()
    {
        var xml = """
                <?xml version="1.0"?>
                <doc>
                  <assembly><name>Temp</name></assembly>
                  <members>
                    <member name="T:Temp.IExampleService"><summary>Interface.</summary></member>
                    <member name="M:Temp.IExampleService.ExecuteAsync(Temp.Request)">
                      <summary>Executes the single-argument request.</summary>
                      <param name="request">The request to execute.</param>
                      <returns>The execution result.</returns>
                      <remarks>Single-argument guidance.</remarks>
                      <exception cref="T:System.InvalidOperationException">The request is invalid.</exception>
                    </member>
                    <member name="M:Temp.IExampleService.ExecuteAsync(Temp.Request,System.Threading.CancellationToken)">
                      <summary>Executes the cancellable request.</summary>
                      <param name="request">The cancellable request.</param>
                      <param name="cancellationToken">The cancellation token.</param>
                      <returns>The cancellable result.</returns>
                    </member>
                    <member name="T:Temp.ExampleService"><summary>Implementation.</summary></member>
                    <member name="M:Temp.ExampleService.ExecuteAsync(Temp.Request)"><inheritdoc/></member>
                    <member name="M:Temp.ExampleService.ExecuteAsync(Temp.Request,System.Threading.CancellationToken)"><inheritdoc/></member>
                    <member name="T:Temp.AnotherExampleService"><summary>Another implementation.</summary></member>
                    <member name="M:Temp.AnotherExampleService.ExecuteAsync(Temp.Request)"><inheritdoc/></member>

                    <member name="T:Temp.ExplicitService"><summary>Explicit implementation.</summary></member>
                    <member name="M:Temp.ExplicitService.Run(Temp.Request)">
                      <inheritdoc cref="M:Temp.IExplicitService.Run(Temp.Request)"/>
                    </member>
                    <member name="M:Temp.IExplicitService.Run(Temp.Request)">
                      <summary>Runs through an explicit cref.</summary>
                    </member>

                    <member name="M:Temp.IOtherService.Remove(Temp.Request)">
                      <summary>Unrelated removal guidance.</summary>
                    </member>
                    <member name="T:Temp.MissingExplicitService"><summary>Missing explicit target.</summary></member>
                    <member name="M:Temp.MissingExplicitService.Remove(Temp.Request)">
                      <inheritdoc cref="M:Temp.IMissingContract.Remove(Temp.Request)"/>
                    </member>

                    <member name="M:Temp.IGenericService.Map``1(``0)">
                      <summary>Maps a generic value.</summary>
                      <typeparam name="T">The value type.</typeparam>
                      <param name="value">The value to map.</param>
                      <returns>The mapped value.</returns>
                    </member>
                    <member name="T:Temp.GenericService"><summary>Generic implementation.</summary></member>
                    <member name="M:Temp.GenericService.Map``1(``0)"><inheritdoc/></member>

                    <member name="P:Temp.IValueSource.Value">
                      <summary>Gets the current value.</summary>
                      <value>The current configured value.</value>
                    </member>
                    <member name="T:Temp.ValueSource"><summary>Value implementation.</summary></member>
                    <member name="P:Temp.ValueSource.Value"><inheritdoc/></member>

                    <member name="M:Temp.IFirst.Save(Temp.Request)"><summary>First save contract.</summary></member>
                    <member name="M:Temp.ISecond.Save(Temp.Request)"><summary>Second save contract.</summary></member>
                    <member name="T:Temp.AmbiguousService"><summary>Ambiguous implementation.</summary></member>
                    <member name="M:Temp.AmbiguousService.Save(Temp.Request)"><inheritdoc/></member>
                  </members>
                </doc>
                """;

        var tmpRoot = Path.Join(Path.GetTempPath(), "Xml2Doc.Tests");
        var tmpDir = Path.Join(tmpRoot, Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);

        try
        {
            var xmlPath = Path.Join(tmpDir, "inheritdoc.xml");
            await File.WriteAllTextAsync(xmlPath, xml, new UTF8Encoding(false));

            var model = Xml2Doc.Core.Models.Xml2Doc.Load(xmlPath);
            var renderer = new MarkdownRenderer(
                model,
                new RendererOptions(FileNameMode: FileNameMode.CleanGenerics));
            var outDir = Path.Join(tmpDir, "out");

            renderer.RenderToDirectory(outDir);

            var implementation = await File.ReadAllTextAsync(
                Path.Join(outDir, "Temp.ExampleService.md"));
            implementation.ShouldContain("Executes the single-argument request.");
            implementation.ShouldContain("The request to execute.");
            implementation.ShouldContain("The execution result.");
            implementation.ShouldContain("Single-argument guidance.");
            implementation.ShouldContain("The request is invalid.");
            implementation.ShouldContain("Executes the cancellable request.");
            implementation.ShouldContain("The cancellation token.");

            var anotherImplementation = await File.ReadAllTextAsync(
                Path.Join(outDir, "Temp.AnotherExampleService.md"));
            anotherImplementation.ShouldContain("Executes the single-argument request.");
            anotherImplementation.ShouldContain("Single-argument guidance.");

            var explicitImplementation = await File.ReadAllTextAsync(
                Path.Join(outDir, "Temp.ExplicitService.md"));
            explicitImplementation.ShouldContain("Runs through an explicit cref.");

            var missingExplicitImplementation = await File.ReadAllTextAsync(
                Path.Join(outDir, "Temp.MissingExplicitService.md"));
            missingExplicitImplementation.ShouldNotContain("Unrelated removal guidance.");

            var genericImplementation = await File.ReadAllTextAsync(
                Path.Join(outDir, "Temp.GenericService.md"));
            genericImplementation.ShouldContain("Maps a generic value.");
            genericImplementation.ShouldContain("**Type parameters**");
            genericImplementation.ShouldContain("- `T` — The value type.");
            genericImplementation.ShouldContain("The value to map.");
            genericImplementation.ShouldContain("The mapped value.");

            var valueImplementation = await File.ReadAllTextAsync(
                Path.Join(outDir, "Temp.ValueSource.md"));
            valueImplementation.ShouldContain("Gets the current value.");
            valueImplementation.ShouldContain("**Value**");
            valueImplementation.ShouldContain("The current configured value.");

            var ambiguousImplementation = await File.ReadAllTextAsync(
                Path.Join(outDir, "Temp.AmbiguousService.md"));
            ambiguousImplementation.ShouldNotContain("First save contract.");
            ambiguousImplementation.ShouldNotContain("Second save contract.");
        }
        finally
        {
            if (Directory.Exists(tmpDir))
                Directory.Delete(tmpDir, recursive: true);
        }
    }
}
