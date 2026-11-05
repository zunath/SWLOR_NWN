using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.CraftSim.Cli;
using SWLOR.CraftSim.Tests.Support;

namespace SWLOR.CraftSim.Tests.Cli
{
    [TestFixture]
    public class LegacyEnhancementsCommandTests
    {
        private string _directory;

        [SetUp]
        public void CreateFolder() => _directory = Directory.CreateTempSubdirectory("craftsim-legacy-enh-").FullName;

        [TearDown]
        public void DeleteFolder() => Directory.Delete(_directory, true);

        private string Path(string name) => System.IO.Path.Combine(_directory, name);

        private int Run(IEnumerable<string> args, out string output)
        {
            var writer = new StringWriter();
            var code = LegacyEnhancementsCommand.Run(args.ToArray(), writer, SharedData.Recipes, SharedData.Gear, SharedData.Enhancements);
            output = writer.ToString();
            return code;
        }

        private string[] Small(params string[] more) =>
            new[] { "--skills", "Agriculture", "--runs", "6", "--recipes-per-band", "1", "--out", Path("report.md") }.Concat(more).ToArray();

        [Test]
        public void WritesTheReportWithEverySectionTheStandardsAskFor()
        {
            Run(Small("--seed", "3"), out var output).Should().Be(0);

            output.Should().Contain("Wrote");
            var report = File.ReadAllText(Path("report.md"));
            report.Should().Contain("# Crafting revamp: legacy baseline of enhancement transfer");
            report.Should().Contain("## Inputs").And.Contain("## Representative enhancements").And.Contain("## Gate 7 cohort: rank 50, best gear, at level");
            report.Should().Contain("## By band, enhancement tier and slot count").And.Contain("## Limitations");
            report.Should().Contain("Seed 3; 6 crafts per recipe level, skill, gear level, policy, enhancement penalty and slot count");
            report.Should().Contain("Command: `SWLOR.CraftSim legacy-enhancements --skills Agriculture");
            report.Should().Contain("`CraftViewModel.cs:695-754`").And.Contain("`CraftViewModel.cs:1420-1468`");
            report.Should().Contain("### Agriculture").And.Contain("Check against player reports");
            report.Should().Contain("Lookahead: made").And.Contain("Dominant: made").And.Contain("enhancements transferred");
        }

        [Test]
        public void TheGateCohortHasABandAndSlotRowForEachBandAndNumberOfEnhancements()
        {
            Run(Small(), out _);

            var report = File.ReadAllText(Path("report.md"));
            var cohort = report[report.IndexOf("## Gate 7 cohort", StringComparison.Ordinal)..report.IndexOf("## By band", StringComparison.Ordinal)];
            var rows = cohort.Split('\n').Where(line => line.StartsWith("| 4") || line.StartsWith("| 5")).ToList();
            rows.Select(row => row.Split('|')[1].Trim() + "/" + row.Split('|')[4].Trim()).Should().Equal(
                "41-45/1", "41-45/2", "41-45/3", "46-50/1", "46-50/2", "46-50/3", "51-53/1", "51-53/2", "51-53/3");
        }

        [Test]
        public void TheSweepCoversEveryGearLevelUnlessTheBestIsAskedFor()
        {
            Run(Small(), out _);
            var all = File.ReadAllText(Path("report.md"));
            Run(Small("--gear", "best"), out _);
            var best = File.ReadAllText(Path("report.md"));

            all.Should().Contain("### Agriculture, no gear").And.Contain("### Agriculture, mid gear").And.Contain("### Agriculture, best gear");
            best.Should().Contain("### Agriculture, best gear").And.NotContain("### Agriculture, no gear").And.NotContain("### Agriculture, mid gear");
            best.Should().Contain("the lookahead policy");
        }

        [Test]
        public void TheSweepGivesEachBandTheTiersItsRecipesAccept()
        {
            Run(Small("--gear", "best"), out _);

            var report = File.ReadAllText(Path("report.md"));
            var sweep = report[report.IndexOf("### Agriculture, best gear", StringComparison.Ordinal)..report.IndexOf("## Limitations", StringComparison.Ordinal)];
            var rows = sweep.Split('\n').Where(line => line.StartsWith("| 1-10") || line.StartsWith("| 41-53")).ToList();
            rows.Should().Contain(row => row.StartsWith("| 1-10 | I (5) |"), "a level 1-10 recipe accepts the level 5 enhancements");
            rows.Should().NotContain(row => row.StartsWith("| 1-10 | V (45) |"), "level 45 is more than 5 above a level 1-10 recipe");
            rows.Should().Contain(row => row.StartsWith("| 41-53 | V (45) |"));
        }

        [Test]
        public void TheJsonHasOneSampleForEachRecipeEnhancementSlotCountGearAndPolicy()
        {
            Run(Small("--json", Path("results.json"), "--gear", "best", "--seed", "9"), out _);

            var json = JObject.Parse(File.ReadAllText(Path("results.json")));
            json["seed"].Value<int>().Should().Be(9);
            json["runs"].Value<int>().Should().Be(6);
            var samples = json["samples"].ToList();
            samples.Should().NotBeEmpty();
            samples.Select(s => s["skill"].Value<string>()).Distinct().Should().Equal("Agriculture");
            samples.Where(s => s["gateCohort"].Value<bool>()).Select(s => s["policy"].Value<string>()).Distinct().Should().BeEquivalentTo("Dominant rotation", "Lookahead");
            samples.Where(s => !s["gateCohort"].Value<bool>()).Select(s => s["policy"].Value<string>()).Distinct().Should().Equal("Lookahead");
            samples.Select(s => s["slots"].Value<int>()).Distinct().OrderBy(v => v).Should().Equal(1, 2, 3);
            samples.Where(s => s["gateCohort"].Value<bool>()).Select(s => s["rank"].Value<int>()).Distinct().Should().Equal(50);
            samples.Should().OnlyContain(s => s["gear"].Value<string>() == "best gear");
            samples.Should().OnlyContain(s => s["expectedTransfersPerAttempt"].Value<double>() <= s["slots"].Value<int>());
        }

        [Test]
        public void ASeedRepeatsTheReportExactlyAndAnotherSeedChangesIt()
        {
            string Report(string seed)
            {
                Run(Small("--seed", seed, "--gear", "best"), out _);
                return File.ReadAllText(Path("report.md"));
            }

            var first = Report("2");

            Report("2").Should().Be(first);
            Report("3").Should().NotBe(first);
        }

        [Test]
        public void ThePlayedSkillsCanBeChosenAndEverySkillWithEnhancementsIsTheDefault()
        {
            Run(new[] { "--skills", "Smithery, engineering", "--runs", "3", "--recipes-per-band", "1", "--gear", "best", "--out", Path("report.md") }, out _);
            var some = File.ReadAllText(Path("report.md"));

            some.Should().Contain("### Smithery").And.Contain("### Engineering").And.NotContain("### Agriculture");
            SWLOR.CraftSim.Legacy.Crafting.LegacyEnhancementOptions.DefaultSkills.Select(s => s.ToString())
                .Should().Equal("Smithery", "Engineering", "Fabrication", "Agriculture");
        }

        [TestCase("--skills", "Espionage", "--skills names a crafting skill with enhancement slots")]
        [TestCase("--skills", "Armor", "--skills names a crafting skill with enhancement slots")]
        [TestCase("--gear", "mid", "--gear must be all or best")]
        [TestCase("--runs", "0", "--runs needs a whole number")]
        [TestCase("--seed", "x", "--seed needs a whole number")]
        [TestCase("--recipes-per-band", "0", "--recipes-per-band needs a whole number")]
        [TestCase("--bogus", "1", "Unknown option '--bogus'")]
        public void BadOptionsAreRejectedBeforeAnythingIsPlayed(string option, string value, string message)
        {
            var args = new List<string> { "--runs", "2", "--out", Path("x.md") };
            var index = args.IndexOf(option);
            if (index >= 0)
                args[index + 1] = value;
            else
                args.AddRange(new[] { option, value });

            var act = () => Run(args, out _);

            act.Should().Throw<ArgumentException>().WithMessage($"*{message}*");
            File.Exists(Path("x.md")).Should().BeFalse();
        }

        [Test]
        public void TheReportAndTheJsonCannotBeTheSameFile()
        {
            var act = () => Run(Small("--json", Path("report.md")), out _);

            act.Should().Throw<ArgumentException>().WithMessage("*name the same file*");
        }

        [Test]
        public void WithoutOutTheReportIsPrinted()
        {
            Run(new[] { "--skills", "Agriculture", "--runs", "2", "--recipes-per-band", "1", "--gear", "best" }, out var output).Should().Be(0);

            output.Should().Contain("# Crafting revamp: legacy baseline of enhancement transfer");
        }
    }
}
