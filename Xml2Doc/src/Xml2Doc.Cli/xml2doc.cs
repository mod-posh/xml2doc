using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xml2Doc.Core;

namespace Xml2Doc.Cli
{
    /// <summary>
    /// Command-line entry point for converting C# XML documentation to Markdown.
    /// </summary>
    /// <remarks>
    /// Usage:
    ///   Xml2Doc.Cli.exe --xml &lt;path&gt; --out &lt;dir-or-file&gt; [--single] [--file-names &lt;verbatim|clean&gt;] [--rootns &lt;ns&gt;] [--lang &lt;id&gt;] [--config &lt;file&gt;]
    /// Options:
    ///   --xml &lt;path&gt;            Path to XML documentation file produced by the compiler.
    ///   --out &lt;dir-or-file&gt;     Output directory (default) or file path when --single is used.
    ///   --single                 Emit a single Markdown file instead of per-type files.
    ///   --file-names &lt;mode&gt;     Filename mode: 'verbatim' (default) or 'clean'.
    ///   --rootns &lt;ns&gt;           Optional root namespace to trim from displayed type names.
    ///   --lang &lt;id&gt;             Code-fence language for Markdown (default: csharp).
    ///   --config &lt;file&gt;         Optional JSON config file (see CliConfig) to supply defaults.
    /// Exit codes:
    ///   0 on success; 1 on failure (e.g., missing required arguments).
    /// </remarks>
    /// <seealso cref="MarkdownRenderer"/>
    /// <seealso cref="RendererOptions"/>
    /// <seealso cref="FileNameMode"/>
    internal static class Program
    {
        /// <summary>
        /// Application entry point.
        /// </summary>
        /// <param name="args">Command-line arguments. See remarks for supported options.</param>
        /// <returns>0 on success; 1 if required arguments are missing.</returns>
        public static int Main(string[] args)
        {
            // Show help early
            if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
            {
                PrintHelp();
                return 0;
            }

            string? xml = null;
            string? outArg = null;
            bool single = false;
            FileNameMode fileNameMode = FileNameMode.Verbatim;
            string? rootns = null;
            string codeLang = "csharp";
            string? configPath = null;

            // 1) parse args
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--xml" when i + 1 < args.Length: xml = args[++i]; break;
                    case "--out" when i + 1 < args.Length: outArg = args[++i]; break;
                    case "--single": single = true; break;
                    case "--file-names" when i + 1 < args.Length:
                        fileNameMode = args[++i].ToLowerInvariant() == "clean" ? FileNameMode.CleanGenerics : FileNameMode.Verbatim;
                        break;
                    case "--rootns" when i + 1 < args.Length: rootns = args[++i]; break;
                    case "--lang" when i + 1 < args.Length: codeLang = args[++i]; break;
                    case "--config" when i + 1 < args.Length: configPath = args[++i]; break;
                    case "--help":
                    case "-h":
                        PrintHelp();
                        return 0;
                }
            }

            // 2) load config (if provided)
            if (!string.IsNullOrWhiteSpace(configPath) && File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var cfg = JsonSerializer.Deserialize<CliConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                xml ??= cfg?.Xml;
                outArg ??= cfg?.Out;
                if (cfg?.Single is bool s) single = s;
                if (!string.IsNullOrWhiteSpace(cfg?.FileNames))
                    fileNameMode = cfg!.FileNames!.Equals("clean", StringComparison.OrdinalIgnoreCase) ? FileNameMode.CleanGenerics : FileNameMode.Verbatim;
                rootns ??= cfg?.RootNamespace;
                codeLang = string.IsNullOrWhiteSpace(cfg?.CodeLanguage) ? codeLang : cfg!.CodeLanguage!;
            }

            if (string.IsNullOrWhiteSpace(xml) || string.IsNullOrWhiteSpace(outArg))
            {
                Console.Error.WriteLine("Missing --xml or --out");
                PrintHelp();
                return 1;
            }

            // 3) run
            var model = Xml2Doc.Core.Models.Xml2Doc.Load(xml!);
            var options = new RendererOptions(
                FileNameMode: fileNameMode,
                RootNamespaceToTrim: string.IsNullOrWhiteSpace(rootns) ? null : rootns,
                CodeBlockLanguage: codeLang
            );
            var renderer = new MarkdownRenderer(model, options);

            if (single)
            {
                renderer.RenderToSingleFile(outArg!);
                Console.WriteLine($"Wrote single-file Markdown to {outArg}");
            }
            else
            {
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
            Console.WriteLine("  Xml2Doc.Cli.exe --xml <path> --out <dir-or-file> [--single] [--file-names <verbatim|clean>] [--rootns <ns>] [--lang <id>] [--config <file>]");
            Console.WriteLine();
        }
    }
}
