using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xml2Doc.Core;
using Xml2Doc.Core.Linking;
using Xml2Doc.Core.Paths;
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
    ///   <item><description><c>--xml</c>: may be repeated to aggregate multiple XML documentation inputs.</description></item>
    ///   <item><description><c>--file-names</c>: <c>verbatim</c> | <c>clean</c> (generic arity removal).</description></item>
    ///   <item><description><c>--rootns</c> / <c>--trim-rootns-filenames</c>: trim namespace from headings and optionally file names.</description></item>
    ///   <item><description><c>--basename-only</c>: drop all namespace segments (applied after trimming / mode transforms).</description></item>
    ///   <item><description><c>--lang</c>: fenced code block language.</description></item>
    ///   <item><description><c>--anchor-algorithm</c>: <c>default|github|kramdown|gfm</c> → maps to <see cref="AnchorAlgorithm"/> enum.</description></item>
    ///   <item><description><c>--template</c>, <c>--front-matter</c>: inject outer template and optional front matter.</description></item>
    ///   <item><description><c>--auto-link</c>, <c>--alias-map</c>, <c>--external-docs</c>: link &amp; alias behavior.</description></item>
    ///   <item><description><c>--toc</c>: per‑type member TOC (multi‑file only).</description></item>
    ///   <item><description><c>--namespace-index</c>: emit namespace index + per‑namespace pages.</description></item>
    ///   <item><description><c>--layout</c>: <c>flat</c> | <c>namespace-folders</c> multi-document output.</description></item>
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
    ///   <item><description>Load one XML input with the compatible single-input path, or aggregate multiple inputs deterministically.</description></item>
    ///   <item><description>Map <c>--anchor-algorithm</c> token to <see cref="AnchorAlgorithm"/> enum.</description></item>
    ///   <item><description>Instantiate <see cref="RendererOptions"/> and <see cref="MarkdownRenderer"/>.</description></item>
    ///   <item><description>Use <see cref="RendererRunner"/> to plan and execute the invocation.</description></item>
    ///   <item><description>Optionally emit JSON report with planned vs actual sets.</description></item>
    /// </list>
    /// Exit codes: 0 success/no differences; 1 invalid arguments; 2 diagnostic or runtime error; 3 differences found.
    /// </remarks>
    internal static class Program
    {
        private static readonly HashSet<string> OptionsWithValues = new(
            StringComparer.Ordinal)
        {
            "--xml",
            "--out",
            "--file-names",
            "--rootns",
            "--lang",
            "--report",
            "--anchor-algorithm",
            "--template",
            "--front-matter",
            "--metadata",
            "--alias-map",
            "--external-docs",
            "--parallel",
            "--config",
            "--manifest-id",
            "--line-endings",
            "--layout"
        };

        private static readonly HashSet<string> FlagOptions = new(
            StringComparer.Ordinal)
        {
            "--single",
            "--trim-rootns-filenames",
            "--dry-run",
            "--diff",
            "--auto-link",
            "--toc",
            "--namespace-index",
            "--no-index",
            "--basename-only",
            "--prune-stale",
            "--help",
            "-h"
        };

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

            var argumentError = ValidateArgumentSyntax(args);
            if (argumentError is not null)
            {
                Console.Error.WriteLine(argumentError);
                return 1;
            }

            var xmlInputs = new List<string>();
            string? outArg = null;
            bool single = false;
            bool singleSpecified = false;
            FileNameMode fileNameMode = FileNameMode.Verbatim;
            bool fileNameModeSpecified = false;
            string? rootns = null;
            bool trimRootNsInFileNames = false;
            string codeLang = "csharp";
            bool codeLangSpecified = false;
            string? reportPath = null;
            bool dryRun = false;
            bool diff = false;
            string anchorAlgorithm = "default";
            bool anchorAlgorithmSpecified = false;
            string? templatePath = null;
            string? frontMatterPath = null;
            var metadataValues = new Dictionary<string, object?>(StringComparer.Ordinal);
            bool autoLink = false;
            string? aliasMapPath = null;
            string? externalDocs = null;
            bool toc = false;
            bool namespaceIndex = false;
            bool generateIndex = true;
            int? parallel = null;
            bool basenameOnly = false;
            string? configPath = null;
            bool pruneStaleFiles = false;
            string? manifestIdentity = null;
            string lineEndings = "lf";
            bool lineEndingsSpecified = false;
            string layout = "flat";
            bool layoutSpecified = false;

            // Parse CLI
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--xml" when i + 1 < args.Length:
                        xmlInputs.Add(args[++i]);
                        break;
                    case "--out" when i + 1 < args.Length: outArg = args[++i]; break;
                    case "--single":
                        single = true;
                        singleSpecified = true;
                        break;
                    case "--file-names" when i + 1 < args.Length:
                        fileNameMode = args[++i].Equals("clean", StringComparison.OrdinalIgnoreCase)
                            ? FileNameMode.CleanGenerics : FileNameMode.Verbatim;
                        fileNameModeSpecified = true;
                        break;
                    case "--rootns" when i + 1 < args.Length: rootns = args[++i]; break;
                    case "--trim-rootns-filenames": trimRootNsInFileNames = true; break;
                    case "--lang" when i + 1 < args.Length:
                        codeLang = args[++i];
                        codeLangSpecified = true;
                        break;
                    case "--report" when i + 1 < args.Length: reportPath = args[++i]; break;
                    case "--dry-run": dryRun = true; break;
                    case "--diff": diff = true; break;
                    case "--anchor-algorithm" when i + 1 < args.Length:
                        anchorAlgorithm = args[++i];
                        anchorAlgorithmSpecified = true;
                        break;
                    case "--template" when i + 1 < args.Length: templatePath = args[++i]; break;
                    case "--front-matter" when i + 1 < args.Length: frontMatterPath = args[++i]; break;
                    case "--metadata" when i + 1 < args.Length:
                        var metadataArgument = args[++i];
                        var separator = metadataArgument.IndexOf('=');
                        if (separator <= 0)
                        {
                            Console.Error.WriteLine(
                                "--metadata values must use key=value syntax with a non-empty key.");
                            return 1;
                        }
                        metadataValues[metadataArgument.Substring(0, separator)] =
                            metadataArgument.Substring(separator + 1);
                        break;
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
                    case "--layout" when i + 1 < args.Length:
                        layout = args[++i];
                        layoutSpecified = true;
                        break;
                    case "--help":
                    case "-h":
                        PrintHelp();
                        return 0;
                }
            }

            // Merge config (CLI wins)
            if (!string.IsNullOrWhiteSpace(configPath))
            {
                if (!File.Exists(configPath))
                {
                    Console.Error.WriteLine(
                        $"Configuration file was not found: {configPath}");
                    return 1;
                }

                CliConfig? cfg;
                try
                {
                    var json = File.ReadAllText(configPath);
                    cfg = JsonSerializer.Deserialize<CliConfig>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            UnmappedMemberHandling = System.Text.Json.Serialization
                                .JsonUnmappedMemberHandling.Disallow
                        });
                }
                catch (Exception ex) when (
                    ex is JsonException ||
                    ex is IOException ||
                    ex is UnauthorizedAccessException)
                {
                    Console.Error.WriteLine(
                        $"Invalid configuration file '{configPath}': " +
                        ex.Message);
                    return 1;
                }

                if (cfg is null)
                {
                    Console.Error.WriteLine(
                        $"Configuration file must contain a JSON object: {configPath}");
                    return 1;
                }

                var config = cfg!;
                if (xmlInputs.Count == 0)
                {
                    if (config.XmlInputs is { Length: > 0 })
                    {
                        xmlInputs.AddRange(config.XmlInputs.Where(
                            path => !string.IsNullOrWhiteSpace(path)));
                    }
                    else if (!string.IsNullOrWhiteSpace(config.Xml))
                    {
                        xmlInputs.Add(config.Xml);
                    }
                }
                outArg ??= config.Out;
                if (!singleSpecified && config.Single is bool s) single = s;

                var cfgNames = config.FileNames;
                if (!fileNameModeSpecified &&
                    !string.IsNullOrWhiteSpace(cfgNames))
                {
                    if (!IsFileNameMode(cfgNames))
                    {
                        Console.Error.WriteLine(
                            "FileNames must be one of: verbatim, clean.");
                        return 1;
                    }
                    fileNameMode = cfgNames.Equals("clean", StringComparison.OrdinalIgnoreCase)
                        ? FileNameMode.CleanGenerics : FileNameMode.Verbatim;
                }

                rootns ??= config.RootNamespace;
                if (config.TrimRootNamespaceInFileNames is bool tr) trimRootNsInFileNames = tr || trimRootNsInFileNames;
                if (!codeLangSpecified &&
                    !string.IsNullOrWhiteSpace(config.CodeLanguage))
                {
                    codeLang = config.CodeLanguage;
                }
                reportPath ??= config.Report;
                if (config.DryRun is bool dr) dryRun = dr || dryRun;
                if (!anchorAlgorithmSpecified &&
                    !string.IsNullOrWhiteSpace(config.AnchorAlgorithm))
                {
                    anchorAlgorithm = config.AnchorAlgorithm;
                }
                if (!string.IsNullOrWhiteSpace(config.Template)) templatePath ??= config.Template;
                if (!string.IsNullOrWhiteSpace(config.FrontMatter)) frontMatterPath ??= config.FrontMatter;
                if (config.Metadata is not null)
                {
                    foreach (var pair in config.Metadata.Where(
                                 pair => !metadataValues.ContainsKey(pair.Key)))
                        metadataValues.Add(pair.Key, pair.Value);
                }
                if (config.AutoLink is bool al) autoLink = al || autoLink;
                if (!string.IsNullOrWhiteSpace(config.AliasMap)) aliasMapPath ??= config.AliasMap;
                if (!string.IsNullOrWhiteSpace(config.ExternalDocs)) externalDocs ??= config.ExternalDocs;
                if (config.Toc is bool tc) toc = tc || toc;
                if (config.NamespaceIndex is bool ni) namespaceIndex = ni || namespaceIndex;
                if (config.GenerateIndex is bool gi && generateIndex) generateIndex = gi;
                if (config.BasenameOnly is bool bo)
                    basenameOnly = bo || basenameOnly;
                if (config.Parallel is int pi && parallel is null) parallel = pi;
                if (config.Diff is bool df) diff = df || diff;
                if (config.PruneStaleFiles is bool ps) pruneStaleFiles = ps || pruneStaleFiles;
                if (!string.IsNullOrWhiteSpace(config.ManifestIdentity))
                    manifestIdentity ??= config.ManifestIdentity;
                if (!lineEndingsSpecified &&
                    !string.IsNullOrWhiteSpace(config.LineEndings))
                {
                    lineEndings = config.LineEndings;
                }
                if (!layoutSpecified &&
                    !string.IsNullOrWhiteSpace(config.Layout))
                {
                    layout = config.Layout;
                }
            }

            if (xmlInputs.Count == 0 ||
                xmlInputs.Any(string.IsNullOrWhiteSpace) ||
                string.IsNullOrWhiteSpace(outArg))
            {
                Console.Error.WriteLine("Missing --xml or --out");
                PrintHelp();
                return 1;
            }

            if (!IsAnchorAlgorithm(anchorAlgorithm))
            {
                Console.Error.WriteLine(
                    "--anchor-algorithm must be one of: default, github, gfm, kramdown.");
                return 1;
            }

            if (!IsLayout(layout))
            {
                Console.Error.WriteLine(
                    "--layout must be one of: flat, namespace-folders.");
                return 1;
            }

            if (parallel is <= 0)
            {
                Console.Error.WriteLine("--parallel must be an integer greater than zero.");
                return 1;
            }

            if (single && toc)
            {
                Console.Error.WriteLine("--toc is only supported for directory output.");
                return 1;
            }

            if (single && namespaceIndex)
            {
                Console.Error.WriteLine(
                    "--namespace-index is only supported for directory output.");
                return 1;
            }

            if (dryRun && diff)
            {
                Console.Error.WriteLine(
                    "--dry-run and --diff cannot be used together; --diff already performs a non-mutating comparison.");
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
            var layoutEnum = layout.Equals(
                    "namespace-folders",
                    StringComparison.OrdinalIgnoreCase)
                ? DocumentLayout.NamespaceFolders
                : DocumentLayout.Flat;

            var diagnosticSink = new CliDiagnosticSink(Console.Error);

            MetadataCollection? metadata = null;
            if (metadataValues.Count > 0)
            {
                try
                {
                    metadata = new MetadataCollection(metadataValues);
                }
                catch (ArgumentException exception)
                {
                    Console.Error.WriteLine("Invalid metadata: " + exception.Message);
                    return 1;
                }
            }

            if (metadata is not null &&
                !string.IsNullOrWhiteSpace(frontMatterPath))
            {
                Console.Error.WriteLine(
                    "--metadata cannot be combined with --front-matter.");
                return 1;
            }

            try
            {
                // Build options & renderer
                var model = xmlInputs.Count == 1
                    ? Xml2Doc.Core.Models.Xml2Doc.Load(
                        xmlInputs[0],
                        diagnosticSink)
                    : Xml2Doc.Core.Models.Xml2Doc.LoadAggregate(
                        xmlInputs,
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
                    BasenameOnly: basenameOnly,
                    ParallelDegree: parallel,
                    GenerateIndex: generateIndex,
                    PruneStaleFiles: pruneStaleFiles,
                    ManifestIdentity: manifestIdentity,
                    LineEndings: lineEndingStyle,
                    DiagnosticSink: diagnosticSink,
                    Metadata: metadata,
                    Layout: layoutEnum
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
                    var pathComparer = Path.DirectorySeparatorChar == '\\'
                        ? StringComparer.OrdinalIgnoreCase
                        : StringComparer.Ordinal;
                    var reportXmlInputs = xmlInputs
                        .Where(path => !string.IsNullOrWhiteSpace(path))
                        .Select(Path.GetFullPath)
                        .OrderBy(path => path, pathComparer)
                        .ThenBy(path => path, StringComparer.Ordinal)
                        .Distinct(pathComparer)
                        .ToArray();
                    var report = new
                    {
                        xml = reportXmlInputs[0],
                        xmlInputs = reportXmlInputs,
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
                            lineEndings = lineEndingStyle.ToString(),
                            layout = layoutEnum.ToString()
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

        private static string? ValidateArgumentSyntax(string[] args)
        {
            for (var index = 0; index < args.Length; index++)
            {
                var option = args[index];
                if (FlagOptions.Contains(option))
                    continue;

                if (!OptionsWithValues.Contains(option))
                    return $"Unknown option: {option}";

                if (index + 1 >= args.Length ||
                    args[index + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    return $"Option {option} requires a value.";
                }

                var value = args[++index];
                if (option == "--file-names" && !IsFileNameMode(value))
                    return "--file-names must be one of: verbatim, clean.";

                if (option == "--parallel" &&
                    (!int.TryParse(value, out var parallel) || parallel <= 0))
                {
                    return "--parallel must be an integer greater than zero.";
                }

                if (option == "--layout" && !IsLayout(value))
                    return "--layout must be one of: flat, namespace-folders.";
            }

            return null;
        }

        private static bool IsFileNameMode(string value) =>
            value.Equals("verbatim", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("clean", StringComparison.OrdinalIgnoreCase);

        private static bool IsAnchorAlgorithm(string value) =>
            value.Equals("default", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("github", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("gfm", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("kramdown", StringComparison.OrdinalIgnoreCase);

        private static bool IsLayout(string value) =>
            value.Equals("flat", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("namespace-folders", StringComparison.OrdinalIgnoreCase);

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
            var temporaryRoot = Path.Join(
                Path.GetTempPath(),
                temporaryDirectoryName);
            var temporaryFileName = Path.GetFileName("output.md");
            var temporaryOutputDirectoryName = Path.GetFileName("output");
            var temporaryOutput = mode == RendererRunMode.SingleFile
                ? Path.Join(temporaryRoot, temporaryFileName)
                : Path.Join(
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
                    else if (FilesHaveSameContent(destination, preview))
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
                DeleteDirectoryBestEffort(temporaryRoot);
            }
        }

        private static bool FilesHaveSameContent(
            string firstPath,
            string secondPath)
        {
            using var first = File.OpenRead(firstPath);
            using var second = File.OpenRead(secondPath);

            if (first.Length != second.Length)
                return false;

            const int bufferSize = 81920;
            var firstBuffer = new byte[bufferSize];
            var secondBuffer = new byte[bufferSize];

            while (true)
            {
                var firstCount = ReadBlock(first, firstBuffer);
                var secondCount = ReadBlock(second, secondBuffer);

                if (firstCount != secondCount)
                    return false;
                if (firstCount == 0)
                    return true;
                if (!firstBuffer.AsSpan(0, firstCount)
                    .SequenceEqual(secondBuffer.AsSpan(0, secondCount)))
                    return false;
            }
        }

        private static int ReadBlock(Stream stream, byte[] buffer)
        {
            var totalRead = 0;
            while (totalRead < buffer.Length)
            {
                var read = stream.Read(
                    buffer,
                    totalRead,
                    buffer.Length - totalRead);
                if (read == 0)
                    break;
                totalRead += read;
            }

            return totalRead;
        }

        private static void DeleteDirectoryBestEffort(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch (IOException)
            {
                // Temporary cleanup must not replace a successful diff result.
            }
            catch (UnauthorizedAccessException)
            {
                // Antivirus or indexing can briefly retain Windows file handles.
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
            Console.WriteLine("  Xml2Doc.Cli.exe --xml <path> [--xml <path> ...] --out <dir-or-file>");
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
            Console.WriteLine("                   [--metadata <key=value>] ...");
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
            Console.WriteLine("                   [--layout <flat|namespace-folders>]");
            Console.WriteLine("                   [--config <file>]");
            Console.WriteLine();
        }
    }
}
