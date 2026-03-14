using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace Xml2Doc.Tests
{
    public sealed class BuildFixture : IDisposable
    {
        public string RepoRoot { get; }
        public string CliExe { get; }
        public string SampleXml { get; }

        public BuildFixture()
        {
            RepoRoot = FindRepoRoot();
            // Build once (Debug CLI + Release Sample)
            Start("dotnet", $"build \"{Path.Combine(RepoRoot, "src", "Xml2Doc.Cli", "Xml2Doc.Cli.csproj")}\" -c Debug -v:minimal -m:1 -nr:false --nologo --no-restore", RepoRoot);
            Start("dotnet", $"build \"{Path.Combine(RepoRoot, "tests", "Xml2Doc.Sample", "Xml2Doc.Sample.csproj")}\" -c Release -v:minimal -m:1 -nr:false --nologo --no-restore", RepoRoot);

#if WINDOWS
            CliExe = Path.Combine(RepoRoot, "src", "Xml2Doc.Cli", "bin", "Debug", "net9.0", "Xml2Doc.Cli.exe");
#else
            CliExe = Path.Combine(RepoRoot, "src", "Xml2Doc.Cli", "bin", "Debug", "net9.0", "Xml2Doc.Cli");
#endif
            SampleXml = Path.Combine(RepoRoot, "tests", "Xml2Doc.Sample", "bin", "Release", "net9.0", "Xml2Doc.Sample.xml");
            if (!File.Exists(CliExe)) throw new FileNotFoundException("CLI not built", CliExe);
            if (!File.Exists(SampleXml)) throw new FileNotFoundException("Sample XML not built", SampleXml);
        }

        public void Dispose() { /* nothing */ }

        private static string FindRepoRoot()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 12 && d != null; i++, d = d.Parent)
            {
                if (File.Exists(Path.Combine(d.FullName, "Xml2Doc.sln")) &&
                    Directory.Exists(Path.Combine(d.FullName, "src")) &&
                    Directory.Exists(Path.Combine(d.FullName, "tests")))
                    return d.FullName;
            }
            throw new DirectoryNotFoundException("Could not find repo root.");
        }

        /// Robust process runner: async reads + timeout to avoid deadlocks.
        public static void Start(string fileName, string args, string cwd, int timeoutMs = 300_000)
        {
            using var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    WorkingDirectory = cwd,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            var stdout = new System.Text.StringBuilder();
            var stderr = new System.Text.StringBuilder();
            p.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
            p.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

            if (!p.Start())
                throw new Exception($"Failed to start: {fileName} {args}");

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            if (!p.WaitForExit(timeoutMs))
            {
                try { p.Kill(entireProcessTree: true); } catch { }
                throw new TimeoutException($"Timed out: {fileName} {args}\n{stdout}\n{stderr}");
            }

            if (p.ExitCode != 0)
                throw new Exception($"{fileName} {args}\n{stdout}\n{stderr}");
        }
    }
}
