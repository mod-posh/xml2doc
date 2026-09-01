using Shouldly;
using Xml2Doc.Core;
using Xunit;

namespace Xml2Doc.Tests;

public class MetadataCollectionTests
{
    [Fact]
    public void Constructor_CopiesAndOrdersCallerValues()
    {
        var tags = new List<string> { "api", "stable" };
        var source = new Dictionary<string, object?>
        {
            ["version"] = "2.4.0",
            ["tags"] = tags
        };

        var metadata = new MetadataCollection(source);
        source["version"] = "changed";
        tags.Add("changed");

        metadata.Keys.ShouldBe(new[] { "tags", "version" });
        metadata["version"].ShouldBe("2.4.0");
        ((IEnumerable<object?>)metadata["tags"]!).ShouldBe(
            new object?[] { "api", "stable" });
    }

    [Fact]
    public void ParseJson_AcceptsDeterministicScalarAndListValues()
    {
        var metadata = MetadataCollection.ParseJson(
            """
            {
              "published": true,
              "retries": 3,
              "tags": ["api", "stable"],
              "version": "2.4.0"
            }
            """);

        metadata.Keys.ShouldBe(new[] { "published", "retries", "tags", "version" });
        metadata["published"].ShouldBe(true);
        metadata["retries"].ShouldBe(3L);
        metadata.ShouldBe(MetadataCollection.ParseJson(
            """{"version":"2.4.0","tags":["api","stable"],"retries":3,"published":true}"""));
    }

    [Fact]
    public void ParseJson_RejectsObjectValues()
    {
        var exception = Should.Throw<ArgumentException>(() =>
            MetadataCollection.ParseJson("""{"nested":{"value":1}}"""));

        exception.Message.ShouldContain("Object");
    }

    [Fact]
    public void Constructor_RejectsUnsupportedValues()
    {
        var exception = Should.Throw<ArgumentException>(() =>
            new MetadataCollection(new Dictionary<string, object?>
            {
                ["unsupported"] = new object()
            }));

        exception.Message.ShouldContain("Unsupported metadata value type");
    }
}
