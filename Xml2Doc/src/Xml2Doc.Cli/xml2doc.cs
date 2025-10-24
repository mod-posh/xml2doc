using System;
using System.Linq;
using Xml2Doc.Core;

/// <summary>
/// Command-line entry point for converting C# XML documentation to Markdown.
/// </summary>
/// <remarks>
/// <para>Usage:</para>
/// <code>xmldoc2md --xml &lt;path-to-xml&gt; --out &lt;dir-or-file&gt; [--single] [--file-names &lt;verbatim|clean&gt;]</code>
/// <para>Options:</para>
/// <list type="bullet">
///   <item>
///     <description><c>--xml &lt;path&gt;</c> — Path to XML documentation file produced by the compiler.</description>
///   </item>
///   <item>
///     <description><c>--out &lt;dir-or-file&gt;</c> — Output directory (default) or file path when <c>--single</c> is used.</description>
///   </item>
///   <item>
///     <description><c>--single</c> — Emit a single Markdown file instead of per-type files.</description>
///   </item>
///   <item>
///     <description><c>--file-names &lt;mode&gt;</c> — Filename mode: <c>verbatim</c> (default) or <c>clean</c>. Maps to <see cref="FileNameMode"/>.</description>
///   </item>
///   <item>
///     <description><c>-h</c> | <c>--help</c> — Show help.</description>
///   </item>
/// </list>
/// <para>Exit codes: 0 on success; 1 on failure (e.g., missing required arguments).</para>
/// </remarks>
/// <seealso cref="MarkdownRenderer"/>
/// <seealso cref="RendererOptions"/>
/// <seealso cref="FileNameMode"/>
internal static class Program
{
    /// <summary>
    /// Application entry point.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>0 on success; 1 if required arguments are missing.</returns>
    public static int Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            PrintHelp();
            return 0;
        }

        string? xml = null;
        string? outArg = null;
        bool single = false;
        FileNameMode fileNameMode = FileNameMode.Verbatim;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--xml" when i + 1 < args.Length:
                    xml = args[++i];
                    break;
                case "--out" when i + 1 < args.Length:
                    outArg = args[++i];
                    break;
                case "--single":
                    single = true;
                    break;
                case "--file-names" when i + 1 < args.Length:
                    var mode = args[++i].ToLowerInvariant();
                    fileNameMode = mode switch
                    {
                        "clean" => FileNameMode.CleanGenerics,
                        _ => FileNameMode.Verbatim
                    };
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(xml) || string.IsNullOrWhiteSpace(outArg))
        {
            Console.Error.WriteLine("Missing --xml or --out");
            PrintHelp();
            return 1;
        }

        var model = Xml2Doc.Core.Models.Xml2Doc.Load(xml!);
        var options = new RendererOptions(FileNameMode: fileNameMode);
        // (optional later) . with RootNamespaceToTrim / CodeBlockLanguage set here
        var renderer = new MarkdownRenderer(model, options);

        if (single)
        {
            // when --single, --out is treated as a *file path*, e.g. ./docs/api.md
            renderer.RenderToSingleFile(outArg!);
            Console.WriteLine($"Wrote single-file Markdown to {outArg}");
        }
        else
        {
            // per-type output into a directory
            renderer.RenderToDirectory(outArg!);
            Console.WriteLine($"Wrote Markdown files to {outArg}");
        }

        return 0;
    }

    /// <summary>
    /// Writes usage information to standard output.
    /// </summary>
    private static void PrintHelp()
    {
        Console.WriteLine("Xml2Doc :: Convert C# XML doc comments to Markdown");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  xmldoc2md --xml <path-to-xml> --out <dir-or-file> [--single] [--file-names <verbatim|clean>]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --xml <path>                 Path to XML documentation file.");
        Console.WriteLine("  --out <dir-or-file>          Output directory (default) or file path when --single is used.");
        Console.WriteLine("  --single                     Emit a single Markdown file instead of per-type files.");
        Console.WriteLine("  --file-names <mode>          Filename mode: 'verbatim' (default) or 'clean'.");
        Console.WriteLine("  -h | --help                  Show help.");
    }
}
