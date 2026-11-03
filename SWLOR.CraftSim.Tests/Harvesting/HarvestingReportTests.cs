using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.CraftSim.Cli;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Harvesting;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Scenario;

namespace SWLOR.CraftSim.Tests.Harvesting
{
    [TestFixture]
    public class HarvestingReportTests
    {
        private string _directory;

        [SetUp]
        public void CreateFolder() => _directory = Directory.CreateTempSubdirectory("craftsim-harvesting-").FullName;

        [TearDown]
        public void DeleteFolder() => Directory.Delete(_directory, true);

        private static GateContext Context(int runs) => new(null, null, runs, 1, AttributeSweep.Default);

        [Test]
        public void TheReportListsTheInputsEveryGateAndTheEvidence()
        {
            var parity = HarvestingFixture.Shipped;
            var context = Context(20000);
            var results = HarvestingReport.Run(context, parity, GateThresholds.Default);

            var report = HarvestingReport.Build(context, parity, GateThresholds.Default, results, "SWLOR.CraftSim harvesting --runs 20000");

            report.Should().StartWith("# Crafting revamp: Prospecting (harvesting) against today's harvests");
            report.Should().Contain("## Inputs").And.Contain("## Verdicts").And.Contain("## Limitations");
            report.Should().Contain("| Economy parity | Passed |");
            report.Should().Contain("| Length | Passed |");
            report.Should().Contain("| XP rate | Passed |");
            report.Should().Contain("| Quick Harvest XP rate cap | Passed |");
            report.Should().Contain("Evidence: refining XP").And.Contain("Evidence: charges, time and gems").And.Contain("Evidence: Gathering rank milestones");
            report.Should().Contain("Evidence: Heavy Strike plans").And.Contain("Evidence: collapse");
            report.Should().Contain("`HarvestSeconds` = 5").And.Contain("`RichSeamBonusPercent` =");
            report.Should().Contain("`veldite_vein`").And.Contain("`arkoxit_vein` (no gem)");
            report.Should().Contain("Ore activity level");
            results.Select(result => result.Gate).Should().Contain(new[] { GateNames.EconomyParity, GateNames.Length, GateNames.XpRate });
            results.Where(result => result.Gate is GateNames.EconomyParity or GateNames.Length or GateNames.XpRate)
                .Should().OnlyContain(result => result.Verdict == GateVerdict.Passed);
        }

        [Test]
        public void TheRefiningEvidenceStatesTheOreActivityLevelsAndTheXpBeforeAndNow()
        {
            var parity = HarvestingFixture.Parity(runs: 300);
            var results = HarvestingReport.Run(Context(300), parity, GateThresholds.Default);

            var refining = results.Single(result => result.Gate == "Evidence: refining XP");

            refining.Verdict.Should().Be(GateVerdict.NotApplicable);
            refining.Summary.Should().Contain("10 x (tier - 1) + 5").And.Contain("5, 15, 25, 35 and 45");
            var rows = refining.Tables.Single().Rows;
            rows.Should().HaveCount(12);
            var veldite = rows.Single(row => row[0] == "raw_veldite");
            veldite[2].Should().Be("5");
            veldite[3].Should().Be("25", "the flat XP refining always paid");
            veldite[4].Should().Be("25", "refining at the ore's level pays the flat XP");
            veldite[5].Should().Be("44", "five ranks below the level, 1.75x");
            veldite[6].Should().Be("19");
            veldite[7].Should().Be("6");
            veldite[8].Should().Be("0", "five ranks past the level");
            veldite[9].Should().Be("44");
            veldite[10].Should().Be("0");
            veldite[11].Should().Be("1.00x");
            var arkoxit = rows.Single(row => row[0] == "raw_arkoxit");
            arkoxit[2].Should().Be("45");
            arkoxit[3].Should().Be("150");
            arkoxit[4].Should().Be("150");
            arkoxit[5].Should().Be("263");
        }

        [Test]
        public void TheCommandWritesTheReportAndJson()
        {
            var report = Path.Combine(_directory, "harvesting.md");
            var json = Path.Combine(_directory, "harvesting.json");
            var output = new StringWriter();

            var code = HarvestingCommand.Run(
                new[] { "--runs", "300", "--seed", "5", "--out", report, "--json", json },
                output,
                new HarvestingParityOptions { Ranks = new[] { 0, 50 } });

            code.Should().Be(0);
            output.ToString().Should().Contain("Wrote").And.Contain("Economy parity:").And.Contain("Quick Harvest XP rate cap: Passed");
            File.ReadAllText(report).Should().Contain("# Crafting revamp: Prospecting (harvesting) against today's harvests").And.Contain("Seed 5; 300 runs per case.");
            JObject.Parse(File.ReadAllText(json))["seed"].Value<int>().Should().Be(5);
        }

        [Test]
        public void TheCommandIsStrictWhenAGateFails()
        {
            var code = HarvestingCommand.Run(
                new[] { "--runs", "300", "--out", Path.Combine(_directory, "strict.md"), "--strict" },
                new StringWriter(),
                new HarvestingParityOptions { Ranks = new[] { 0, 50 }, LeastActions = 7 });

            code.Should().Be(2);
        }

        [Test]
        public void TheCommandRejectsWhatItDoesNotTake()
        {
            var act = () => HarvestingCommand.Run(new[] { "--typo", "1" }, new StringWriter());

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void TheReportsSeedRepeatsExactly()
        {
            string Report()
            {
                var path = Path.Combine(_directory, "repeat.md");
                HarvestingCommand.Run(new[] { "--runs", "250", "--seed", "9", "--out", path }, new StringWriter(), new HarvestingParityOptions { Ranks = new[] { 0, 50 } });
                return File.ReadAllText(path);
            }

            Report().Should().Be(Report());
        }

        [Test]
        public void RefiningPaysTheFlatXpAtTheOresLevel_AndTheRankScalesItByTheCurve()
        {
            var veldite = RefiningModel.Ores().Single(ore => ore.Ore == "raw_veldite");

            RefiningModel.Xp(veldite, 9).Should().Be(225, "the flat table of the refinery");
            RefiningModel.CurrentXpPerUnit(veldite, 5).Should().Be(25, "at the ore's level it is today's flat XP");
            RefiningModel.CurrentXp(veldite, 5, 9).Should().Be(225);
            RefiningModel.CurrentXpPerUnit(veldite, 0).Should().Be(44);
            RefiningModel.CurrentXpPerUnit(veldite, 10).Should().Be(0);
            RefiningModel.CurrentXpPerUnit(veldite, 50).Should().Be(0);
            foreach (var ore in RefiningModel.Ores())
                RefiningModel.CurrentXpPerUnit(ore, 10 * (ore.RequiredLevel - 1) + 5).Should().Be(ore.XpPerUnit, ore.Ore);
        }
    }
}
