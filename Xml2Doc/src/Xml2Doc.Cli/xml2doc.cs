using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xml2Doc.Core;

namespace Xml2Doc.Cli
{
    /// <summary>
    /// Command‑line entry point for converting C# XML documentation into Markdown using Xml2Doc.
    /// </summary>
    /// <remarks>
    /// Output modes:
    /// <list type="bullet">
    ///   <item><description><b>Per‑type</b> (default): one <c>.md</c> per documented type plus an <c>index.md</c> (pass a directory to <c>--out</c>).</description></item>
    ///   <item><description><b>Single file</b>: consolidated index + all types (pass a file path to <c>--out</c> with <c>--single</c>).</description></item>
    /// </list>
    /// Option precedence (highest first):
    /// <list type="number">
    ///   <item><description>Command‑line arguments</description></item>
    ///   <item><description>JSON configuration file (<c>--config</c>) values (only where not overridden)</description></item>
    ///   <item><description>Built‑in defaults</description></item>
    /// </list>
    /// Extended options:
    /// <list type="bullet">
    ///   <item><description><c>--file-names</c>: <c>verbatim</c> | <c>clean</c> (generic arity removal).</description></item>
    ///   <item><description><c>--rootns</c> + <c>--trim-rootns-filenames</c>: trim namespace from headings and (optionally) file names.</description></item>
    ///   <item><description><c>--lang</c>: fenced code block language.</description></item>
    ///   <item><description><c>--anchor-algorithm</c>: heading slug style (<c>default</c>|<c>github</c>|<c>kramdown</c>|<c>gfm</c>).</description></item>
    ///   <item><description><c>--template</c>, <c>--front-matter</c>: layout / front matter customization.</description></item>
    ///   <item><description><c>--auto-link</c>, <c>--alias-map</c>, <c>--external-docs</c>: linking / alias behavior.</description></item>
    ///   <item><description><c>--toc</c>: per‑type member TOC (multi‑file only).</description></item>
    ///   <item><description><c>--namespace-index</c>: emit namespace index pages.</description></item>
    ///   <item><description><c>--basename-only</c>: file names reduced to final identifier (drops namespace portion).</description></item>
    ///   <item><description><c>--parallel &lt;N&gt;</c>: limit generation concurrency.</description></item>
    ///   <item><description><c>--report</c>: JSON execution report (includes real writes or planned set on dry‑run).</description></item>
    ///   <item><description><c>--dry-run</c>: compute planned output list without writing.</description></item>
    ///   <item><description><c>--diff</c>: reserved (no effect yet).</description></item>
    /// </list>
    /// Dry run behavior:
    /// <para>
    /// When <c>--dry-run</c> is supplied the report includes:
    /// <list type="bullet">
    ///   <item><description><c>files</c>: empty (no writes).</description></item>
    ///   <item><description><c>wouldWrite</c>: all paths that would be generated.</description></item>
    ///   <item><description><c>wouldDelete</c>: existing <c>.md</c> files (including namespace pages) not in <c>wouldWrite</c>.</description></item>
    /// </list>
    /// </para>
    /// Exit codes: 0 = success (including dry‑run), 1 = invalid/missing required arguments, 2 = runtime error.
    /// </remarks>
    internal static class Program
    {
        /// <summary>
        /// Application entry point for the Xml2Doc CLI.
        /// </summary>
        /// <param name="args">Command‑line arguments (use <c>--help</c> / <c>-h</c> for usage).</param>
        /// <returns>0 on success; 1 on argument validation failure; 2 on unhandled exception.</returns>
        public static int Main(string[] args)
        {
            // help
            if (args.Length == 0 || Array.IndexOf(args, "--help") >= 0 || Array.IndexOf(args, "-h") >= 0)
            {
                PrintHelp();
                return 0;
            }

            string? xml = null;
            string? outArg = null;
            bool single = false;
            FileNameMode fileNameMode = FileNameMode.Verbatim;
            string? rootns = null;
            bool trimRootNsInFileNames = false;
            string codeLang = "csharp";
            string? reportPath = null;
            bool dryRun = false;
            bool diff = false;
            string anchorAlgorithm = "default";
            string? templatePath = null;
            string? frontMatterPath = null;
            bool autoLink = false;
            string? aliasMapPath = null;
            string? externalDocs = null;
            bool toc = false;
            bool namespaceIndex = false;
            int? parallel = null;
            bool? basenameOnly = false;
            string? configPath = null;

            // 1) parse CLI
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--xml" when i + 1 < args.Length: xml = args[++i]; break;
                    case "--out" when i + 1 < args.Length: outArg = args[++i]; break;
                    case "--single": single = true; break;
                    case "--file-names" when i + 1 < args.Length:
                        fileNameMode = args[++i].Equals("clean", StringComparison.OrdinalIgnoreCase)
                            ? FileNameMode.CleanGenerics : FileNameMode.Verbatim;
                        break;
                    case "--rootns" when i + 1 < args.Length: rootns = args[++i]; break;
                    case "--trim-rootns-filenames": trimRootNsInFileNames = true; break;
                    case "--lang" when i + 1 < args.Length: codeLang = args[++i]; break;
                    case "--report" when i + 1 < args.Length: reportPath = args[++i]; break;
                    case "--dry-run": dryRun = true; break;
                    case "--diff": diff = true; break;
                    case "--anchor-algorithm" when i + 1 < args.Length: anchorAlgorithm = args[++i]; break;
                    case "--template" when i + 1 < args.Length: templatePath = args[++i]; break;
                    case "--front-matter" when i + 1 < args.Length: frontMatterPath = args[++i]; break;
                    case "--auto-link": autoLink = true; break;
                    case "--alias-map" when i + 1 < args.Length: aliasMapPath = args[++i]; break;
                    case "--external-docs" when i + 1 < args.Length: externalDocs = args[++i]; break;
                    case "--toc": toc = true; break;
                    case "--namespace-index": namespaceIndex = true; break;
                    case "--basename-only": basenameOnly = true; break;
                    case "--parallel" when i + 1 < args.Length:
                        if (int.TryParse(args[++i], out var p)) parallel = p;
                        break;
                    case "--config" when i + 1 < args.Length: configPath = args[++i]; break;
                    case "--help":
                    case "-h":
                        PrintHelp();
                        return 0;
                }
            }

            // 2) load JSON config (if supplied), but CLI args win
            if (!string.IsNullOrWhiteSpace(configPath) && File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var cfg = JsonSerializer.Deserialize<CliConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                xml ??= cfg?.Xml;
                outArg ??= cfg?.Out;
                if (cfg?.Single is bool s) single = s;

                var cfgNames = cfg?.FileNames;
                if (!string.IsNullOrWhiteSpace(cfgNames))
                    fileNameMode = cfgNames!.Equals("clean", StringComparison.OrdinalIgnoreCase)
                        ? FileNameMode.CleanGenerics : FileNameMode.Verbatim;

                rootns ??= cfg?.RootNamespace;
                if (cfg?.TrimRootNamespaceInFileNames is bool tr) trimRootNsInFileNames = tr || trimRootNsInFileNames;
                if (!string.IsNullOrWhiteSpace(cfg?.CodeLanguage)) codeLang = cfg!.CodeLanguage!;
                reportPath ??= cfg?.Report;
                if (cfg?.DryRun is bool dr) dryRun = dr || dryRun;
                if (!string.IsNullOrWhiteSpace(cfg?.AnchorAlgorithm)) anchorAlgorithm = cfg!.AnchorAlgorithm!;
                if (!string.IsNullOrWhiteSpace(cfg?.Template)) templatePath = templatePath ?? cfg!.Template!;
                if (!string.IsNullOrWhiteSpace(cfg?.FrontMatter)) frontMatterPath = frontMatterPath ?? cfg!.FrontMatter!;
                if (cfg?.AutoLink is bool al) autoLink = al || autoLink;
                if (!string.IsNullOrWhiteSpace(cfg?.AliasMap)) aliasMapPath = aliasMapPath ?? cfg!.AliasMap!;
                if (!string.IsNullOrWhiteSpace(cfg?.ExternalDocs)) externalDocs = externalDocs ?? cfg!.ExternalDocs!;
                if (cfg?.Toc is bool tc) toc = tc || toc;
                if (cfg?.NamespaceIndex is bool ni) namespaceIndex = ni || namespaceIndex;
                if (cfg?.BasenameOnly is bool bo) basenameOnly = basenameOnly ?? bo;
                if (cfg?.Parallel is int pi && parallel is null) parallel = pi;
                if (cfg?.Diff is bool df) diff = df || diff;
            }

            if (string.IsNullOrWhiteSpace(xml) || string.IsNullOrWhiteSpace(outArg))
            {
                Console.Error.WriteLine("Missing --xml or --out");
                PrintHelp();
                return 1;
            }

            try
            {
                // 3) load model & options
                var model = Xml2Doc.Core.Models.Xml2Doc.Load(xml);
                var options = new RendererOptions(
                    FileNameMode: fileNameMode,
                    RootNamespaceToTrim: string.IsNullOrWhiteSpace(rootns) ? null : rootns,
                    CodeBlockLanguage: codeLang,
                    TrimRootNamespaceInFileNames: trimRootNsInFileNames,
                    AnchorAlgorithm: anchorAlgorithm,
                    TemplatePath: templatePath,
                    FrontMatterPath: frontMatterPath,
                    AutoLink: autoLink,
                    AliasMapPath: aliasMapPath,
                    ExternalDocs: externalDocs,
                    EmitToc: toc,
                    EmitNamespaceIndex: namespaceIndex,
                    BasenameOnly: basenameOnly ?? false,
                    ParallelDegree: parallel
                );

                var renderer = new MarkdownRenderer(model, options);

                // Plan outputs (dry-run uses this list; normal run relies on it for reporting)
                var plannedFiles = single
                    ? renderer.PlanOutputs(outDir: "", singleFilePath: outArg)
                    : renderer.PlanOutputs(outDir: outArg!, singleFilePath: null);

                // 4) render or dry-run
                List<string> produced = new();

                if (dryRun)
                {
                    var where = single ? Path.GetDirectoryName(Path.GetFullPath(outArg!))! : Path.GetFullPath(outArg!);
                    Console.WriteLine($"[dry-run] would write {plannedFiles.Count} files under {where}");
                }
                else
                {
                    if (single)
                    {
                        var full = Path.GetFullPath(outArg!);
                        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                        renderer.RenderToSingleFile(full);
                        Console.WriteLine($"Wrote single-file Markdown to {full}");
                    }
                    else
                    {
                        var dir = Path.GetFullPath(outArg!);
                        Directory.CreateDirectory(dir);
                        renderer.RenderToDirectory(dir);
                        Console.WriteLine($"Wrote Markdown files to {dir}");
                    }
                    produced.AddRange(plannedFiles);
                }

                // 5) optional JSON report
                if (!string.IsNullOrWhiteSpace(reportPath))
                {
                    var report = new
                    {
                        xml = Path.GetFullPath(xml),
                        single,
                        outputFile = single ? Path.GetFullPath(outArg!) : null,
                        outputDir = single ? null : Path.GetFullPath(outArg!),
                        files = produced.ToArray(),                                // actual writes
                        wouldWrite = dryRun ? plannedFiles.ToArray() : null,        // dry-run preview
                        wouldDelete = dryRun && !single
                            ? ComputeWouldDelete(Path.GetFullPath(outArg!), plannedFiles)
                            : null,                                               // predicted obsolete files
                        options = new
                        {
                            fileNameMode = fileNameMode.ToString(),
                            rootNs = options.RootNamespaceToTrim,
                            trimRootNsInFileNames = trimRootNsInFileNames,
                            lang = options.CodeBlockLanguage,
                            anchorAlgorithm,
                            templatePath,
                            frontMatterPath,
                            autoLink,
                            aliasMapPath,
                            externalDocs,
                            toc,
                            namespaceIndex,
                            basenameOnly = options.BasenameOnly,
                            parallel
                        },
                        dryRun,
                        diffRequested = diff,
                        timestamp = DateTimeOffset.Now
                    };

                    var repFull = Path.GetFullPath(reportPath!);
                    var repDir = Path.GetDirectoryName(repFull);
                    if (!string.IsNullOrEmpty(repDir) && !Directory.Exists(repDir))
                        Directory.CreateDirectory(repDir);

                    File.WriteAllText(repFull, JsonSerializer.Serialize(report, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
                    Console.WriteLine($"Report written to {repFull}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 2;
            }
        }

        /// <summary>
        /// Computes the set of existing Markdown files in the output directory (including namespace pages)
        /// that are <b>not</b> part of the currently planned output list. Used only for dry-run reporting.
        /// </summary>
        /// <param name="outDir">The target output directory.</param>
        /// <param name="plannedFiles">Files that would be produced this run.</param>
        /// <returns>Array of full paths that would become obsolete (empty if directory missing or on failure).</returns>
        private static string[] ComputeWouldDelete(string outDir, IReadOnlyList<string> plannedFiles)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(outDir) || !Directory.Exists(outDir))
                    return Array.Empty<string>();

                var planned = new HashSet<string>(
                    plannedFiles.Select(Path.GetFullPath),
                    StringComparer.OrdinalIgnoreCase);

                IEnumerable<string> existing = Directory.GetFiles(outDir, "*.md", SearchOption.TopDirectoryOnly)
                                                        .Select(Path.GetFullPath);

                var nsDir = Path.Combine(outDir, "namespaces");
                if (Directory.Exists(nsDir))
                    existing = existing.Concat(Directory.GetFiles(nsDir, "*.md", SearchOption.TopDirectoryOnly)
                                                        .Select(Path.GetFullPath));

                return existing.Where(p => !planned.Contains(p)).ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Writes command‑line usage instructions to standard output.
        /// </summary>
        private static void PrintHelp()
        {
            Console.WriteLine("Xml2Doc :: Convert C# XML doc comments to Markdown");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  Xml2Doc.Cli.exe --xml <path> --out <dir-or-file>");
            Console.WriteLine("                   [--single]");
            Console.WriteLine("                   [--file-names <verbatim|clean>]");
            Console.WriteLine("                   [--rootns <ns>]");
            Console.WriteLine("                   [--trim-rootns-filenames]");
            Console.WriteLine("                   [--lang <id>]");
            Console.WriteLine("                   [--report <file>]");
            Console.WriteLine("                   [--dry-run]");
            Console.WriteLine("                   [--diff]");
            Console.WriteLine("                   [--anchor-algorithm <default|github|kramdown|gfm>]");
            Console.WriteLine("                   [--template <file>]");
            Console.WriteLine("                   [--front-matter <file>]");
            Console.WriteLine("                   [--auto-link]");
            Console.WriteLine("                   [--alias-map <file>]");
            Console.WriteLine("                   [--external-docs <url|mapfile>]");
            Console.WriteLine("                   [--toc]");
            Console.WriteLine("                   [--namespace-index]");
            Console.WriteLine("                   [--basename-only]");
            Console.WriteLine("                   [--parallel <N>]");
            Console.WriteLine("                   [--config <file>]");
            Console.WriteLine();
        }
    }
}
