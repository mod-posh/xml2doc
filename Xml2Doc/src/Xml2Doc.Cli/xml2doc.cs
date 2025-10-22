using System;
using System.Linq;
using Xml2Doc.Core;

/// <summary>
/// Command-line entry point for converting C# XML documentation to Markdown files.
/// </summary>
/// <remarks>
/// Usage:
///   xml2doc --xml &lt;path-to-xml&gt; --out &lt;out-dir&gt;
/// Options:
///   --xml     Path to the XML documentation file produced by the compiler.
///   --out     Output directory where Markdown files will be written.
///   -h, --help  Show usage information.
/// Exit codes:
///   0 on success, 1 on failure (e.g., missing required arguments).
/// </remarks>
internal static class Program
{
    /// <summary>
    /// Application entry point.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>0 on success; 1 if required arguments are missing or an error occurs.</returns>
    public static int Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
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
    }

    /// <summary>
    /// Writes usage information to the console.
    /// </summary>
    private static void PrintUsage()
    {
        Console.WriteLine("Xml2Doc :: Convert C# XML doc comments to Markdown");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  xml2doc --xml <path-to-xml> --out <out-dir>");
        Console.WriteLine();
    }
}
