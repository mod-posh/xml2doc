using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace Xml2Doc.Tests
{
    public class MsbuildIncrementalTests
    {
        // ----- repo path helpers -----

        private static string RepoRoot
        {
            get
            {
                var d = new DirectoryInfo(AppContext.BaseDirectory);
                for (int i = 0; i < 12 && d != null; i++, d = d.Parent)
                {
                    if (File.Exists(Path.Combine(d.FullName, "Xml2Doc.sln")) &&
                        Directory.Exists(Path.Combine(d.FullName, "src")) &&
                        Directory.Exists(Path.Combine(d.FullName, "tests")))
                        return d.FullName;
                }
                throw new DirectoryNotFoundException("Could not find repo root (folder containing Xml2Doc.sln, src/, tests/).");
            }
        }

        private static string Solution => Path.Combine(RepoRoot, "Xml2Doc.sln");
        private static string SampleProject => Path.Combine(RepoRoot, "tests", "Xml2Doc.Sample", "Xml2Doc.Sample.csproj");

        // Find the xml2doc.stamp that our targets write under obj/<Config>/<TFM>/
        private static string? FindStamp(string projectPath, string configuration)
        {
            var projDir = Path.GetDirectoryName(projectPath)!;
            var objDir = Path.Combine(projDir, "obj", configuration);

            if (!Directory.Exists(objDir)) return null;

            // Search all TFMs (netstandard2.0/net8.0/net9.0)
            var stamps = Directory.GetFiles(objDir, "xml2doc.stamp", SearchOption.AllDirectories);
            return stamps.OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
        }

        // ----- process runner -----

        private static void Run(string fileName, string args, string workingDir)
        {
            using var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            p.Start();
            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();

            if (p.ExitCode != 0)
                throw new Exception($"{fileName} {args}\n{stdout}\n{stderr}");
        }

        // ----- the test -----

        [Fact]
        public void Xml2Doc_task_is_incremental_across_builds()
        {
            const string cfg = "Release";

            // 1) Build the whole solution once (known-good entry point)
            Run("dotnet", $"build \"{Solution}\" -c {cfg} -m:1 -nr:false -v:minimal", RepoRoot);

            // 2) Locate the xml2doc.stamp emitted for the sample project
            var stamp1 = FindStamp(SampleProject, cfg);
            Assert.True(!string.IsNullOrEmpty(stamp1) && File.Exists(stamp1!),
                $"Expected xml2doc.stamp under obj/{cfg}/<TFM>/ for {SampleProject}");
            var t1 = File.GetLastWriteTimeUtc(stamp1!);

            // 3) Build the solution again with NO changes -> target should be up-to-date (stamp time unchanged)
            Run("dotnet", $"build \"{Solution}\" -c {cfg} -m:1 -nr:false -v:minimal", RepoRoot);

            var stamp2 = FindStamp(SampleProject, cfg);
            Assert.True(!string.IsNullOrEmpty(stamp2) && File.Exists(stamp2!),
                $"Expected xml2doc.stamp after second build for {SampleProject}");
            var t2 = File.GetLastWriteTimeUtc(stamp2!);

            Assert.Equal(t1, t2); // no-op build keeps stamp unchanged

            // 4) Change Xml2Doc properties (toggle SingleFile) -> task must re-run (stamp updated)
            // NOTE: Passing MSBuild properties to the solution is reliable here.
            var outFile = Path.Combine(Path.GetDirectoryName(SampleProject)!, "docs", "api.md");
            Run("dotnet",
                $"build \"{Solution}\" -c {cfg} -m:1 -nr:false -v:minimal " +
                $"/p:Xml2Doc_SingleFile=true /p:Xml2Doc_OutputFile=\"{outFile}\"",
                RepoRoot);

            var stamp3 = FindStamp(SampleProject, cfg);
            Assert.True(!string.IsNullOrEmpty(stamp3) && File.Exists(stamp3!),
                $"Expected xml2doc.stamp after property change for {SampleProject}");
            var t3 = File.GetLastWriteTimeUtc(stamp3!);

            Assert.True(t3 > t2, $"Expected updated stamp time after property change. t2={t2:o}, t3={t3:o}");
        }
    }
}
