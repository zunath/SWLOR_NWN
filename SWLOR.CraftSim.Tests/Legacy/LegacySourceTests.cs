using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Legacy;
using SWLOR.CraftSim.Tests.Support;

namespace SWLOR.CraftSim.Tests.Legacy
{
    [TestFixture]
    public class LegacySourceTests
    {
        // Parallel work may add or remove a few lines above a rule; a changed rule still fails.
        private const int Tolerance = 40;

        private static readonly Regex Citation = new(@"\b(?<file>[A-Za-z]+\.cs):(?<first>\d+)(?:-(?<last>\d+))?", RegexOptions.Compiled);

        public static IEnumerable<TestCaseData> References =>
            LegacySource.All.Select(reference => new TestCaseData(reference).SetArgDisplayNames(reference.Name));

        [TestCaseSource(nameof(References))]
        public void EveryAnchorIsStillInTheCitedSource(SourceReference reference)
        {
            var path = Path.Combine(SharedData.Root, reference.File.Replace('/', Path.DirectorySeparatorChar));
            File.Exists(path).Should().BeTrue($"{reference.File} holds the rule '{reference.Name}'");

            var lines = File.ReadAllLines(path);
            var from = Math.Max(0, reference.FirstLine - 1 - Tolerance);
            var to = Math.Min(lines.Length - 1, reference.LastLine - 1 + Tolerance);
            var window = lines[from..(to + 1)].Select(l => l.Trim()).ToList();
            var outside = new List<string>();

            foreach (var anchor in reference.Anchors)
            {
                window.Any(line => line.Contains(anchor, StringComparison.Ordinal))
                    .Should().BeTrue($"'{anchor}' must appear within {Tolerance} lines of {reference.Citation} ({reference.Name}); the rule changed or moved");

                var exact = lines[Math.Max(0, reference.FirstLine - 1)..Math.Min(lines.Length, reference.LastLine)];
                if (!exact.Any(line => line.Contains(anchor, StringComparison.Ordinal)))
                    outside.Add(anchor);
            }

            if (outside.Count > 0)
                Assert.Warn($"{reference.Citation} ({reference.Name}) has drifted from its cited lines; update LegacySource. Anchors elsewhere: {string.Join(" | ", outside)}");
        }

        private static IEnumerable<string> PortedCode() =>
            new[] { "Legacy", "Data" }
                .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(SharedData.Root, "SWLOR.CraftSim", folder), "*.cs", SearchOption.AllDirectories))
                .Where(file => Path.GetFileName(file) != "LegacySource.cs");

        [Test]
        public void TheRegistryListsEveryReference()
        {
            var declared = typeof(LegacySource)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(SourceReference))
                .Select(f => (SourceReference)f.GetValue(null))
                .ToList();

            LegacySource.All.Should().BeEquivalentTo(declared, options => options.WithoutStrictOrdering());
            LegacySource.All.Select(r => r.Name).Should().OnlyHaveUniqueItems();
        }

        [Test]
        public void EveryCitationInTheLegacyCodeFallsInsideARegisteredReference()
        {
            var problems = new List<string>();

            foreach (var file in PortedCode())
            {
                foreach (var (line, number) in File.ReadAllLines(file).Select((l, i) => (l, i + 1)))
                {
                    foreach (Match match in Citation.Matches(line))
                    {
                        var name = match.Groups["file"].Value;
                        var first = int.Parse(match.Groups["first"].Value);
                        var last = match.Groups["last"].Success ? int.Parse(match.Groups["last"].Value) : first;
                        var covered = LegacySource.All.Any(r =>
                            Path.GetFileName(r.File) == name && first >= r.FirstLine && last <= r.LastLine);
                        if (!covered)
                            problems.Add($"{Path.GetFileName(file)}:{number} cites {match.Value}, which no registered reference covers");
                    }
                }
            }

            problems.Should().BeEmpty();
        }

        [Test]
        public void EveryRegisteredFileIsCitedBySomeLegacyCode()
        {
            var text = string.Concat(PortedCode().Select(File.ReadAllText));

            foreach (var file in LegacySource.All.Select(r => Path.GetFileName(r.File)).Distinct())
                text.Contains(file + ":", StringComparison.Ordinal).Should().BeTrue($"the ported code must cite {file}");
        }
    }
}
