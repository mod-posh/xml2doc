using Xml2Doc.Core;

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine("Xml2Doc :: Convert C# XML doc comments to Markdown");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  xml2doc --xml <path-to-xml> --out <out-dir>");
    Console.WriteLine();
    return 0;
}

string? xml = null;
string? outDir = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--xml" && i + 1 < args.Length) xml = args[++i];
    else if (args[i] == "--out" && i + 1 < args.Length) outDir = args[++i];
}

if (string.IsNullOrWhiteSpace(xml) || string.IsNullOrWhiteSpace(outDir))
{
    Console.Error.WriteLine("Missing --xml or --out");
    return 1;
}

var model = Xml2Doc.Core.Models.Xml2Doc.Load(xml!);
var renderer = new MarkdownRenderer(model);
renderer.RenderToDirectory(outDir!);
Console.WriteLine($"Wrote Markdown to {outDir}");
return 0;
