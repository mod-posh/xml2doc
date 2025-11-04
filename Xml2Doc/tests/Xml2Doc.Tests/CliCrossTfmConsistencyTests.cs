using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Shouldly;

namespace Xml2Doc.Tests
{
    static class TestPaths
    {
        public static string RepoRoot
        {
            get
            {
                // Walk up until we find the *solution* folder (the one that directly contains Xml2Doc.sln).
                var d = new DirectoryInfo(AppContext.BaseDirectory);
                for (int i = 0; i < 12 && d != null; i++, d = d.Parent)
                {
                    var sln = Path.Combine(d.FullName, "Xml2Doc.sln");
                    var src = Path.Combine(d.FullName, "src");
                    var tests = Path.Combine(d.FullName, "tests");
                    if (File.Exists(sln) && Directory.Exists(src) && Directory.Exists(tests))
                        return d.FullName;
                }
                throw new DirectoryNotFoundException("Could not locate repo root (folder that contains Xml2Doc.sln, src/, tests/).");
            }
        }

        // IMPORTANT: RepoRoot is exactly the folder that contains Xml2Doc.sln (…\Xml2Doc)
        // Do NOT add another "Xml2Doc" segment here.
        public static string Solution => Path.Combine(RepoRoot, "Xml2Doc.sln");
        public static string SampleProject => Path.Combine(RepoRoot, "tests", "Xml2Doc.Sample", "Xml2Doc.Sample.csproj");
        public static string CliProject => Path.Combine(RepoRoot, "src", "Xml2Doc.Cli", "Xml2Doc.Cli.csproj");

        public static string SampleXml(string cfg, string tfm) =>
            Path.Combine(RepoRoot, "tests", "Xml2Doc.Sample", "bin", cfg, tfm, "Xml2Doc.Sample.xml");

        public static string CliDll(string cfg, string tfm) =>
            Path.Combine(RepoRoot, "src", "Xml2Doc.Cli", "bin", cfg, tfm, "Xml2Doc.Cli.dll");

        public static string TempOut(string suffix) =>
            Path.Combine(Path.GetTempPath(), $"xml2doc-test-{suffix}-{Guid.NewGuid():N}");
    }

    public class CliCrossTfmConsistencyTests
    {
        private static readonly string[] ExpectedFiles =
        {
        "index.md",
        "Xml2Doc.Sample.GenericPlayground.md",
        "Xml2Doc.Sample.Mathx.md",
        "Xml2Doc.Sample.XItem.md",
        "Xml2Doc.Sample.AliasingPlayground.md",
    };

        private static bool _built;

        [Fact]
        public async Task Cli_net80_and_net90_produce_identical_output()
        {
            const string cfg = "Release";

            // Build the entire solution once (this path is known-good in your repo)
            EnsureSolutionBuilt(cfg);

            var xml = TestPaths.SampleXml(cfg, "net9.0");
            if (!File.Exists(xml))
                Assert.True(File.Exists(xml), $"Expected sample XML at {xml}. The solution must be built (Release) before running this test.");

            var cli8 = TestPaths.CliDll(cfg, "net8.0");
            var cli9 = TestPaths.CliDll(cfg, "net9.0");
            if (!File.Exists(cli8) || !File.Exists(cli9))
                Assert.True(File.Exists(cli8), $"Missing CLI artifact at {cli8}");
                Assert.True(File.Exists(cli9), $"Missing CLI artifact at {cli9}");

            var out8 = TestPaths.TempOut("net8");
            var out9 = TestPaths.TempOut("net9");
            Directory.CreateDirectory(out8);
            Directory.CreateDirectory(out9);

            try
            {
                await RunCliAsync(cli8, xml, out8);
                await RunCliAsync(cli9, xml, out9);

                foreach (var rel in ExpectedFiles)
                {
                    var p8 = Path.Combine(out8, rel);
                    var p9 = Path.Combine(out9, rel);
                    Assert.True(File.Exists(p8), $"Missing {p8}");
                    Assert.True(File.Exists(p9), $"Missing {p9}");

                    var a = Normalize(File.ReadAllText(p8));
                    var b = Normalize(File.ReadAllText(p9));
                    Assert.Equal(a, b);
                }
            }
            finally
            {
                SafeDelete(out8);
                SafeDelete(out9);
            }
        }

        // ---- helpers ----

        private static void EnsureSolutionBuilt(string cfg)
        {
            if (_built) return;

            // Build once, single node to avoid file-handle noise
            Run("dotnet", $"build \"{TestPaths.Solution}\" -c {cfg} -m:1 -nr:false -v minimal", TestPaths.RepoRoot);
            _built = true;
        }

        private static string Normalize(string s)
        {
            var lines = s.Replace("\r\n", "\n").Replace("\r", "\n")
                         .Split('\n').Select(l => l.TrimEnd());
            return string.Join("\n", lines).TrimEnd();
        }

        private static async Task RunCliAsync(string cliDll, string xml, string outDir)
        {
            using var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"\"{cliDll}\" --xml \"{xml}\" --out \"{outDir}\"",
                    WorkingDirectory = TestPaths.RepoRoot,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            p.Start();
            await p.WaitForExitAsync();
            if (p.ExitCode != 0)
            {
                var so = await p.StandardOutput.ReadToEndAsync();
                var se = await p.StandardError.ReadToEndAsync();
                throw new Exception($"dotnet {p.StartInfo.Arguments}\n{so}\n{se}");
            }
        }

        private static void Run(string file, string args, string cwd)
        {
            using var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args,
                    WorkingDirectory = cwd,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            p.Start();
            p.WaitForExit();
            if (p.ExitCode != 0)
                throw new Exception($"{file} {args}\n{p.StandardOutput.ReadToEnd()}\n{p.StandardError.ReadToEnd()}");
        }

        private static void SafeDelete(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { /* ignore */ }
        }
    }
}
