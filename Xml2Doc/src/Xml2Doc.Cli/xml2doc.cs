using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xml2Doc.Core;
using Xml2Doc.Core.Linking;
using Xml2Doc.Core.Pipeline;

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
    /// Option precedence (highest first): CLI args → JSON <c>--config</c> → built‑in defaults.
    /// Extended options:
    /// <list type="bullet">
    ///   <item><description><c>--file-names</c>: <c>verbatim</c> | <c>clean</c> (generic arity removal).</description></item>
    ///   <item><description><c>--rootns</c> / <c>--trim-rootns-filenames</c>: trim namespace from headings and optionally file names.</description></item>
    ///   <item><description><c>--basename-only</c>: drop all namespace segments (applied after trimming / mode transforms).</description></item>
    ///   <item><description><c>--lang</c>: fenced code block language.</description></item>
    ///   <item><description><c>--anchor-algorithm</c>: <c>default|github|kramdown|gfm</c> → maps to <see cref="AnchorAlgorithm"/> enum.</description></item>
    ///   <item><description><c>--template</c>, <c>--front-matter</c>: inject outer template and optional front matter.</description></item>
    ///   <item><description><c>--auto-link</c>, <c>--alias-map</c>, <c>--external-docs</c>: link &amp; alias behavior.</description></item>
    ///   <item><description><c>--toc</c>: per‑type member TOC (multi‑file only).</description></item>
    ///   <item><description><c>--namespace-index</c>: emit namespace index + per‑namespace pages.</description></item>
    ///   <item><description><c>--parallel &lt;N&gt;</c>: cap generation concurrency.</description></item>
    ///   <item><description><c>--prune-stale</c> / <c>--manifest-id</c>: remove only stale outputs owned by the same invocation.</description></item>
    ///   <item><description><c>--report</c>: write JSON execution report.</description></item>
    ///   <item><description><c>--dry-run</c>: plan (no writes); report includes <c>wouldWrite</c>/<c>wouldDelete</c>.</description></item>
    ///   <item><description><c>--diff</c>: compare generated Markdown with existing output without modifying it.</description></item>
    /// </list>
    /// Dry run report fields:
    /// <list type="bullet">
    ///   <item><description><c>plannedFiles</c>: deterministic absolute output paths.</description></item>
    ///   <item><description><c>writtenFiles</c>, <c>skippedFiles</c>, and <c>prunedFiles</c>: actual execution results; empty during dry run.</description></item>
    ///   <item><description><c>wouldWrite</c>: full absolute paths planned.</description></item>
    ///   <item><description><c>wouldDelete</c>: stale files owned by the selected manifest identity; empty when pruning is disabled.</description></item>
    ///   <item><description><c>timings</c>: runner planning, rendering, lifecycle, and total durations.</description></item>
    /// </list>
    /// Processing pipeline:
    /// <list type="number">
    ///   <item><description>Parse CLI args.</description></item>
    ///   <item><description>Overlay JSON config values for unspecified options.</description></item>
    ///   <item><description>Map <c>--anchor-algorithm</c> token to <see cref="AnchorAlgorithm"/> enum.</description></item>
    ///   <item><description>Instantiate <see cref="RendererOptions"/> and <see cref="MarkdownRenderer"/>.</description></item>
    ///   <item><description>Use <see cref="RendererRunner"/> to plan and execute the invocation.</description></item>
    ///   <item><description>Optionally emit JSON report with planned vs actual sets.</description></item>
    /// </list>
    /// Exit codes: 0 success/no differences; 1 invalid arguments; 2 diagnostic or runtime error; 3 differences found.
    /// </remarks>
    internal static class Program
    {
        /// <summary>
        /// Application entry point for the Xml2Doc CLI.
        /// </summary>
        /// <param name="args">Command‑line arguments (use <c>--help</c> / <c>-h</c> for usage).</param>
        /// <returns>0 success/no differences; 1 validation failure; 2 diagnostic or runtime error; 3 differences found.</returns>
        public static int Main(string[] args)
        {
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
            bool generateIndex = true;
            int? parallel = null;
            bool? basenameOnly = false;
            string? configPath = null;
            bool pruneStaleFiles = false;
            string? manifestIdentity = null;
            string lineEndings = "lf";
            bool lineEndingsSpecified = false;

            // Parse CLI
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
                    case "--no-index": generateIndex = false; break;
                    case "--basename-only": basenameOnly = true; break;
                    case "--parallel" when i + 1 < args.Length:
                        if (int.TryParse(args[++i], out var p)) parallel = p;
                        break;
                    case "--config" when i + 1 < args.Length: configPath = args[++i]; break;
                    case "--prune-stale": pruneStaleFiles = true; break;
                    case "--manifest-id" when i + 1 < args.Length: manifestIdentity = args[++i]; break;
                    case "--line-endings" when i + 1 < args.Length:
                        lineEndings = args[++i];
                        lineEndingsSpecified = true;
                        break;
                    case "--help":
                    case "-h":
                        PrintHelp();
                        return 0;
                }
            }

            // Merge config (CLI wins)
            if (!string.IsNullOrWhiteSpace(configPath) && File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var cfg = JsonSerializer.Deserialize<CliConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                xml ??= cfg?.Xml;
                outArg ??= cfg?.Out;
                if (cfg?.Single is bool s) single = s;

                var cfgNames = cfg?.FileNames;
                if (!string.IsNullOrWhiteSpace(cfgNames))
                    fileNameMode = cfgNames.Equals("clean", StringComparison.OrdinalIgnoreCase)
                        ? FileNameMode.CleanGenerics : FileNameMode.Verbatim;

                rootns ??= cfg?.RootNamespace;
                if (cfg?.TrimRootNamespaceInFileNames is bool tr) trimRootNsInFileNames = tr || trimRootNsInFileNames;
                if (!string.IsNullOrWhiteSpace(cfg?.CodeLanguage)) codeLang = cfg.CodeLanguage!;
                reportPath ??= cfg?.Report;
                if (cfg?.DryRun is bool dr) dryRun = dr || dryRun;
                if (!string.IsNullOrWhiteSpace(cfg?.AnchorAlgorithm)) anchorAlgorithm = cfg.AnchorAlgorithm!;
                if (!string.IsNullOrWhiteSpace(cfg?.Template)) templatePath = templatePath ?? cfg.Template!;
                if (!string.IsNullOrWhiteSpace(cfg?.FrontMatter)) frontMatterPath = frontMatterPath ?? cfg.FrontMatter!;
                if (cfg?.AutoLink is bool al) autoLink = al || autoLink;
                if (!string.IsNullOrWhiteSpace(cfg?.AliasMap)) aliasMapPath = aliasMapPath ?? cfg.AliasMap!;
                if (!string.IsNullOrWhiteSpace(cfg?.ExternalDocs)) externalDocs = externalDocs ?? cfg.ExternalDocs!;
                if (cfg?.Toc is bool tc) toc = tc || toc;
                if (cfg?.NamespaceIndex is bool ni) namespaceIndex = ni || namespaceIndex;
                if (cfg?.GenerateIndex is bool gi && generateIndex) generateIndex = gi;
                if (cfg?.BasenameOnly is bool bo) basenameOnly = basenameOnly ?? bo;
                if (cfg?.Parallel is int pi && parallel is null) parallel = pi;
                if (cfg?.Diff is bool df) diff = df || diff;
                if (cfg?.PruneStaleFiles is bool ps) pruneStaleFiles = ps || pruneStaleFiles;
                if (!string.IsNullOrWhiteSpace(cfg?.ManifestIdentity))
                    manifestIdentity ??= cfg.ManifestIdentity;
                if (!lineEndingsSpecified &&
                    !string.IsNullOrWhiteSpace(cfg?.LineEndings))
                {
                    lineEndings = cfg.LineEndings!;
                }
            }

            if (string.IsNullOrWhiteSpace(xml) || string.IsNullOrWhiteSpace(outArg))
            {
                Console.Error.WriteLine("Missing --xml or --out");
                PrintHelp();
                return 1;
            }

            if (pruneStaleFiles && single)
            {
                Console.Error.WriteLine("--prune-stale is only supported for directory output.");
                return 1;
            }

            if (pruneStaleFiles && string.IsNullOrWhiteSpace(manifestIdentity))
            {
                Console.Error.WriteLine("--manifest-id is required when --prune-stale is enabled.");
                return 1;
            }

            LineEndingStyle lineEndingStyle;

            switch (lineEndings.ToLowerInvariant())
            {
                case "lf":
                    lineEndingStyle = LineEndingStyle.Lf;
                    break;
                case "crlf":
                    lineEndingStyle = LineEndingStyle.CrLf;
                    break;
                case "native":
                    lineEndingStyle = LineEndingStyle.Native;
                    break;
                default:
                    Console.Error.WriteLine(
                        "--line-endings must be one of: lf, crlf, native.");
                    return 1;
            }

            // Anchor algorithm token → enum
            var anchorAlgEnum = (anchorAlgorithm ?? "default").ToLowerInvariant() switch
            {
                "github" => Xml2Doc.Core.AnchorAlgorithm.Github,
                "gfm" => Xml2Doc.Core.AnchorAlgorithm.Gfm,
                "kramdown" => Xml2Doc.Core.AnchorAlgorithm.Kramdown,
                _ => Xml2Doc.Core.AnchorAlgorithm.Default
            };

            var diagnosticSink = new CliDiagnosticSink(Console.Error);

            try
            {
                // Build options & renderer
                var model = Xml2Doc.Core.Models.Xml2Doc.Load(
                    xml,
                    diagnosticSink);
                var options = new RendererOptions(
                    FileNameMode: fileNameMode,
                    RootNamespaceToTrim: string.IsNullOrWhiteSpace(rootns) ? null : rootns,
                    CodeBlockLanguage: codeLang,
                    TrimRootNamespaceInFileNames: trimRootNsInFileNames,
                    AnchorAlgorithm: anchorAlgEnum,
                    TemplatePath: templatePath,
                    FrontMatterPath: frontMatterPath,
                    AutoLink: autoLink,
                    AliasMapPath: aliasMapPath,
                    ExternalDocs: externalDocs,
                    LinkPolicy: string.IsNullOrWhiteSpace(externalDocs)
                        ? LinkPolicy.InternalOnly
                        : LinkPolicy.PreferExternalForUnknown,
                    EmitToc: toc,
                    EmitNamespaceIndex: namespaceIndex,
                    BasenameOnly: basenameOnly ?? false,
                    ParallelDegree: parallel,
                    GenerateIndex: generateIndex,
                    PruneStaleFiles: pruneStaleFiles,
                    ManifestIdentity: manifestIdentity,
                    LineEndings: lineEndingStyle,
                    DiagnosticSink: diagnosticSink
                );

                var mode = single
                    ? RendererRunMode.SingleFile
                    : RendererRunMode.PerType;
                CliDiffResult? diffResult = null;
                RendererRunResult runResult;

                if (diff)
                {
                    diffResult = RunDiff(model, options, outArg!, mode);
                    runResult = diffResult.RunResult;
                }
                else
                {
                    var renderer = new MarkdownRenderer(model, options);
                    var runner = new RendererRunner(renderer);
                    runResult = runner.Run(new RendererRunRequest(
                        outArg!,
                        mode,
                        DryRun: dryRun));
                }

                if (diffResult is not null)
                {
                    Console.WriteLine(
                        $"[diff] added {diffResult.AddedFiles.Count}, " +
                        $"changed {diffResult.ChangedFiles.Count}, " +
                        $"unchanged {diffResult.UnchangedFiles.Count}, " +
                        $"removed {diffResult.RemovedFiles.Count}");
                }
                else if (dryRun)
                {
                    var where = single ? Path.GetDirectoryName(Path.GetFullPath(outArg!))! : Path.GetFullPath(outArg!);
                    Console.WriteLine($"[dry-run] would write {runResult.PlannedFiles.Count} files under {where}");
                }
                else if (single)
                {
                    Console.WriteLine(
                        $"Processed single-file Markdown at {Path.GetFullPath(outArg!)} " +
                        $"(written {runResult.WrittenFiles.Count}, skipped {runResult.SkippedFiles.Count})");
                }
                else
                {
                    Console.WriteLine(
                        $"Processed Markdown files in {Path.GetFullPath(outArg!)} " +
                        $"(written {runResult.WrittenFiles.Count}, skipped {runResult.SkippedFiles.Count}, " +
                        $"pruned {runResult.PrunedFiles.Count})");
                }

                // Report (optional)
                if (!string.IsNullOrWhiteSpace(reportPath))
                {
                    var report = new
                    {
                        xml = Path.GetFullPath(xml),
                        single,
                        outputFile = single ? Path.GetFullPath(outArg!) : null,
                        outputDir = single ? null : Path.GetFullPath(outArg!),
                        files = dryRun || diff
                            ? Array.Empty<string>()
                            : runResult.PlannedFiles,
                        plannedFiles = runResult.PlannedFiles,
                        writtenFiles = runResult.WrittenFiles,
                        skippedFiles = runResult.SkippedFiles,
                        prunedFiles = runResult.PrunedFiles,
                        wouldWrite = diffResult is not null
                            ? diffResult.AddedFiles
                                .Concat(diffResult.ChangedFiles)
                                .ToArray()
                            : dryRun ? runResult.PlannedFiles : null,
                        wouldDelete = diffResult is not null
                            ? diffResult.RemovedFiles
                            : dryRun ? runResult.WouldPruneFiles : null,
                        differences = diffResult is null ? null : new
                        {
                            addedFiles = diffResult.AddedFiles,
                            changedFiles = diffResult.ChangedFiles,
                            unchangedFiles = diffResult.UnchangedFiles,
                            removedFiles = diffResult.RemovedFiles,
                            hasDifferences = diffResult.HasDifferences
                        },
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
                            generateIndex,
                            basenameOnly = options.BasenameOnly,
                            parallel,
                            pruneStaleFiles,
                            manifestIdentity,
                            lineEndings = lineEndingStyle.ToString()
                        },
                        dryRun,
                        diffRequested = diff,
                        timings = new
                        {
                            totalMilliseconds = runResult.Elapsed.TotalMilliseconds,
                            planningMilliseconds = runResult.PlanningElapsed.TotalMilliseconds,
                            renderingMilliseconds = runResult.RenderingElapsed.TotalMilliseconds,
                            lifecycleMilliseconds = runResult.LifecycleElapsed.TotalMilliseconds
                        }
                    };

                    var repFull = Path.GetFullPath(reportPath!);
                    var repDir = Path.GetDirectoryName(repFull);
                    if (!string.IsNullOrEmpty(repDir) && !Directory.Exists(repDir))
                        Directory.CreateDirectory(repDir);

                    File.WriteAllText(repFull, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
                    Console.WriteLine($"Report written to {repFull}");
                }

                if (diagnosticSink.HasErrors)
                    return 2;

                return diffResult?.HasDifferences == true ? 3 : 0;
            }
            catch (Exception ex)
            {
                if (!diagnosticSink.HasErrors)
                    Console.Error.WriteLine(ex.ToString());
                return 2;
            }
        }

        private static CliDiffResult RunDiff(
            Xml2Doc.Core.Models.Xml2Doc model,
            RendererOptions options,
            string outputPath,
            RendererRunMode mode)
        {
            var comparisonResult = new RendererRunner(
                new MarkdownRenderer(model, options)).Run(
                    new RendererRunRequest(
                        outputPath,
                        mode,
                        DryRun: true));
            var temporaryDirectoryName = Path.GetFileName(
                "xml2doc-diff-" + Guid.NewGuid().ToString("N"));
            var temporaryRoot = Path.Combine(
                Path.GetTempPath(),
                temporaryDirectoryName);
            var temporaryFileName = Path.GetFileName("output.md");
            var temporaryOutputDirectoryName = Path.GetFileName("output");
            var temporaryOutput = mode == RendererRunMode.SingleFile
                ? Path.Combine(temporaryRoot, temporaryFileName)
                : Path.Combine(
                    temporaryRoot,
                    temporaryOutputDirectoryName);

            try
            {
                var previewOptions = options with
                {
                    PruneStaleFiles = false,
                    ManifestIdentity = null
                };
                var previewResult = new RendererRunner(
                    new MarkdownRenderer(model, previewOptions)).Run(
                        new RendererRunRequest(temporaryOutput, mode));

                if (previewResult.PlannedFiles.Count !=
                    comparisonResult.PlannedFiles.Count)
                {
                    throw new InvalidOperationException(
                        "Diff preview did not match the deterministic output plan.");
                }

                var addedFiles = new List<string>();
                var changedFiles = new List<string>();
                var unchangedFiles = new List<string>();

                for (var index = 0;
                    index < comparisonResult.PlannedFiles.Count;
                    index++)
                {
                    var destination = comparisonResult.PlannedFiles[index];
                    var preview = previewResult.PlannedFiles[index];

                    if (!File.Exists(destination))
                        addedFiles.Add(destination);
                    else if (File.ReadAllBytes(destination)
                        .SequenceEqual(File.ReadAllBytes(preview)))
                        unchangedFiles.Add(destination);
                    else
                        changedFiles.Add(destination);
                }

                return new CliDiffResult(
                    comparisonResult,
                    addedFiles,
                    changedFiles,
                    unchangedFiles,
                    comparisonResult.WouldPruneFiles);
            }
            finally
            {
                if (Directory.Exists(temporaryRoot))
                    Directory.Delete(temporaryRoot, recursive: true);
            }
        }

        private sealed record CliDiffResult(
            RendererRunResult RunResult,
            IReadOnlyList<string> AddedFiles,
            IReadOnlyList<string> ChangedFiles,
            IReadOnlyList<string> UnchangedFiles,
            IReadOnlyList<string> RemovedFiles)
        {
            public bool HasDifferences =>
                AddedFiles.Count > 0 ||
                ChangedFiles.Count > 0 ||
                RemovedFiles.Count > 0;
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
            Console.WriteLine("                   [--external-docs <base-url>]");
            Console.WriteLine("                   [--toc]");
            Console.WriteLine("                   [--namespace-index]");
            Console.WriteLine("                   [--no-index]");
            Console.WriteLine("                   [--basename-only]");
            Console.WriteLine("                   [--parallel <N>]");
            Console.WriteLine("                   [--prune-stale --manifest-id <identity>]");
            Console.WriteLine("                   [--line-endings <lf|crlf|native>]");
            Console.WriteLine("                   [--config <file>]");
            Console.WriteLine();
        }
    }
}
