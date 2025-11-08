using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xml2Doc.Core;
using static System.Net.Mime.MediaTypeNames;

namespace Xml2Doc.Cli
{
    /// <summary>
    /// Command‑line entry point for converting C# XML documentation to Markdown.
    /// </summary>
    /// <remarks>
    /// Supports two modes:
    /// <list type="bullet">
    ///   <item><description>Per‑type (default): one <c>.md</c> per documented type plus an <c>index.md</c> (specify output directory).</description></item>
    ///   <item><description>Single file: consolidated index + all types (use <c>--single</c> with an output file path).</description></item>
    /// </list>
    /// Option precedence:
    /// <list type="number">
    ///   <item><description>Command‑line arguments.</description></item>
    ///   <item><description>JSON config file (<c>--config</c>) values (only where not overridden).</description></item>
    ///   <item><description>Internal defaults.</description></item>
    /// </list>
    /// Exit codes:
    /// <list type="bullet">
    ///   <item><description><c>0</c> – success (including dry run).</description></item>
    ///   <item><description><c>1</c> – invalid or missing required arguments.</description></item>
    ///   <item><description><c>2</c> – unhandled runtime error.</description></item>
    /// </list>
    /// JSON report (when <c>--report</c> is used) contains: input XML path, mode, output file/dir, list of produced (or would produce) files, selected options, dry‑run flag, diff flag, timestamp.
    /// </remarks>
    internal static class Program
    {
        /// <summary>
        /// Application entry point.
        /// </summary>
        /// <param name="args">
        /// Command‑line arguments. See <see cref="PrintHelp"/> for supported switches.
        /// </param>
        /// <returns>Process exit code (0 success, 1 invalid args, 2 runtime failure).</returns>
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
                    ParallelDegree: parallel
                );

                var renderer = new MarkdownRenderer(model, options);

                // 4) render or dry-run
                List<string> produced = new();

                if (dryRun)
                {
                    if (single)
                    {
                        produced.Add(Path.GetFullPath(outArg));
                        Console.WriteLine($"[dry-run] would write single-file to {outArg}");
                    }
                    else
                    {
                        var outDir = Path.GetFullPath(outArg);
                        // Compute filenames the same way the renderer does
                        foreach (var t in model.Members.Values.Where(m => m.Kind == "T").OrderBy(t => t.Id))
                        {
                            var fileName = ComputeFileName(t.Id, fileNameMode, options);
                            produced.Add(Path.Combine(outDir, fileName));
                        }
                        produced.Add(Path.Combine(outDir, "index.md"));
                        Console.WriteLine($"[dry-run] would write {produced.Count} files under {outDir}");
                    }
                }
                else
                {
                    if (single)
                    {
                        var full = Path.GetFullPath(outArg);
                        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                        renderer.RenderToSingleFile(full);
                        produced.Add(full);
                        Console.WriteLine($"Wrote single-file Markdown to {full}");
                    }
                    else
                    {
                        var dir = Path.GetFullPath(outArg);
                        Directory.CreateDirectory(dir);
                        renderer.RenderToDirectory(dir);
                        // Enumerate for the report
                        produced.AddRange(Directory.GetFiles(dir, "*.md", SearchOption.TopDirectoryOnly));
                        Console.WriteLine($"Wrote Markdown files to {dir}");
                    }
                }

                // 5) optional JSON report
                if (!string.IsNullOrWhiteSpace(reportPath))
                {
                    var report = new
                    {
                        xml = Path.GetFullPath(xml),
                        single,
                        outputFile = single ? Path.GetFullPath(outArg) : null,
                        outputDir = single ? null : Path.GetFullPath(outArg),
                        files = produced.ToArray(),
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
                            parallel
                        },
                        dryRun = dryRun,
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
            Console.WriteLine("                   [--anchor-algorithm <default | github | kramdown | gfm>]");
            Console.WriteLine("                   [--template <file>]");
            Console.WriteLine("                   [--front-matter<file>]");
            Console.WriteLine("                   [--auto-link]");
            Console.WriteLine("                   [--alias-map<file>]");
            Console.WriteLine("                   [--external-docs <url | mapfile>]");
            Console.WriteLine("                   [--toc]");
            Console.WriteLine("                   [--namespace-index]");
            Console.WriteLine("                   [--parallel <N>]");
            Console.WriteLine("                   [--config <file>]");
            Console.WriteLine();
        }

        /// <summary>
        /// Computes the Markdown file name for a documented type, mirroring renderer logic (including generic clean‑up and root namespace trimming).
        /// </summary>
        /// <param name="typeId">Type documentation ID without the <c>T:</c> prefix.</param>
        /// <param name="mode">Filename transformation mode (<see cref="FileNameMode"/>).</param>
        /// <param name="opt">Renderer options (root namespace trimming and filename trimming flags).</param>
        /// <returns>File name including <c>.md</c> extension.</returns>
        private static string ComputeFileName(string typeId, FileNameMode mode, RendererOptions opt)
        {
            // typeId is like "Namespace.Type`1" (no "T:" prefix here)
            var name = typeId;

            if (mode == FileNameMode.CleanGenerics)
            {
                name = System.Text.RegularExpressions.Regex.Replace(name, @"`\d+", "");
                name = name.Replace('{', '<').Replace('}', '>');
            }

            // Apply trim AFTER filename-mode logic
            if (opt.TrimRootNamespaceInFileNames && !string.IsNullOrWhiteSpace(opt.RootNamespaceToTrim))
            {
                var prefix = opt.RootNamespaceToTrim + ".";
                if (name.StartsWith(prefix, StringComparison.Ordinal))
                    name = name.Substring(prefix.Length);
            }

            name = name.Replace('<', '[').Replace('>', ']');
            return name + ".md";
        }
    }
}
