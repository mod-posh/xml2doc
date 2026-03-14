using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Xml2Doc.Tests
{
    [CollectionDefinition("cli")]
    public class CliCollection : ICollectionFixture<BuildFixture> { }

    [Collection("cli")]
    public class CliTests
    {
        private readonly BuildFixture _fx;
        private static readonly Regex AnchorRx = new(@"<a id=""([^""]+)""", RegexOptions.Compiled);

        public CliTests(BuildFixture fx) => _fx = fx;

        private static string TempDir()
        {
            var path = Path.Combine(Path.GetTempPath(), "xml2doc_test_" + Guid.NewGuid().ToString("n"));
            Directory.CreateDirectory(path);
            return path;
        }

        [Fact]
        public void AnchorAlgorithm_changes_type_heading_ids_in_single_file_mode()
        {
            var outDir = TempDir();
            string One(string algo)
            {
                var dst = Path.Combine(outDir, $"api-{algo}.md");
                BuildFixture.Start(_fx.CliExe, $"--xml \"{_fx.SampleXml}\" --out \"{dst}\" --single --anchor-algorithm {algo} --file-names clean --basename-only", _fx.RepoRoot);
                return dst;
            }

            var fDefault = One("default");
            var fGithub = One("github");
            var fKramdown = One("kramdown");
            var fGfm = One("gfm");

            string[] Anchors(string f) =>
                File.ReadLines(f)
                    .SelectMany(l => AnchorRx.Matches(l).Cast<Match>())
                    .Select(m => m.Groups[1].Value)
                    .Distinct()
                    .OrderBy(s => s, StringComparer.Ordinal)
                    .ToArray();

            var a0 = Anchors(fDefault);
            var a1 = Anchors(fGithub);
            var a2 = Anchors(fKramdown);
            var a3 = Anchors(fGfm);

            Assert.True(!a0.SequenceEqual(a1) || !a0.SequenceEqual(a2) || !a0.SequenceEqual(a3),
                "Expected at least one difference in anchors across algorithms.");
        }

        [Fact]
        public void NamespaceIndex_trim_and_basename_affect_output()
        {
            var outDir = TempDir();

            BuildFixture.Start(_fx.CliExe,
                $"--xml \"{_fx.SampleXml}\" --out \"{outDir}\" --file-names clean " +
                $"--rootns \"Xml2Doc.Sample\" --trim-rootns-filenames --namespace-index --basename-only --toc",
                _fx.RepoRoot);

            Assert.True(File.Exists(Path.Combine(outDir, "namespaces.md")));
            Assert.True(Directory.Exists(Path.Combine(outDir, "namespaces")));

            var top = Directory.GetFiles(outDir, "*.md").Select(Path.GetFileName).ToArray();
            Assert.Contains("index.md", top);
            Assert.Contains("AliasingPlayground.md", top);
            Assert.DoesNotContain("Xml2Doc.Sample.AliasingPlayground.md", top);

            var nsPage = Path.Combine(outDir, "namespaces", "Xml2Doc.Sample.md");
            Assert.True(File.Exists(nsPage), "Expected per-namespace page.");
            var content = File.ReadAllText(nsPage).Replace("\r\n", "\n");
            Assert.Contains("../AliasingPlayground.md", content);
        }

        [Fact]
        public void DryRun_report_has_wouldWrite_and_wouldDelete_but_no_files()
        {
            var outDir = TempDir();
            var report = Path.Combine(outDir, "rep.json");

            // real run first to create some files
            BuildFixture.Start(_fx.CliExe,
                $"--xml \"{_fx.SampleXml}\" --out \"{outDir}\" --file-names clean --namespace-index --basename-only",
                _fx.RepoRoot);

            // dry-run with a different trim/root option
            BuildFixture.Start(_fx.CliExe,
                $"--xml \"{_fx.SampleXml}\" --out \"{outDir}\" --file-names clean --namespace-index --basename-only " +
                $"--rootns \"Xml2Doc.Sample\" --trim-rootns-filenames --dry-run --report \"{report}\"",
                _fx.RepoRoot);

            var json = File.ReadAllText(report);
            Assert.Contains("\"dryRun\": true", json);
            Assert.Contains("\"files\": []", json);
            Assert.Contains("\"wouldWrite\": [", json);
            Assert.Contains("\"wouldDelete\": [", json);
        }
    }
}
