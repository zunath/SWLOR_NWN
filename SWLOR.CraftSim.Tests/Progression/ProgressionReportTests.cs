using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.CraftSim.Cli;
using SWLOR.CraftSim.Progression;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Progression
{
    [TestFixture]
    public class ProgressionReportTests
    {
        private static readonly ProgressionAssumptions Assumptions = new();
        private string _directory;

        [SetUp]
        public void CreateFolder() => _directory = Directory.CreateTempSubdirectory("craftsim-progression-").FullName;

        [TearDown]
        public void DeleteFolder() => Directory.Delete(_directory, true);

        [Test]
        public void TheReportHasEverySectionTheOwnerAsksFor()
        {
            var report = ProgressionReport.Build(ProgressionFixture.RealRecipes, Assumptions, "SWLOR.CraftSim progression");

            report.Should().Contain("## Hours to rank 50 today")
                .And.Contain("## Hours per 10-rank band")
                .And.Contain("## Gathering rank 50")
                .And.Contain("**Quick Harvest**")
                .And.Contain("**Quick Search**")
                .And.Contain("## What supplies the components")
                .And.Contain("## Sensitivity")
                .And.Contain("## Guild task cohort")
                .And.Contain("### The guild rank chore")
                .And.Contain("### Catch tasks (Agriculture)")
                .And.Contain("`agr_tsk_017`")
                .And.Contain("## Research cohort")
                .And.Contain("## Proposals for the owner")
                .And.Contain("### XP target")
                .And.Contain("### Guild task XP")
                .And.Contain("### Research XP ceiling")
                .And.Contain("## Limitations");
            foreach (var skill in new[] { "Smithery", "Engineering", "Fabrication", "Agriculture" })
                report.Should().Contain($"### {skill}");
        }

        [Test]
        public void TheReportQuotesTheServersRankFiftyCostAndRpXpPace()
        {
            var report = ProgressionReport.Build(ProgressionFixture.RealRecipes, Assumptions, "cmd");

            report.Should().Contain("411,125 XP").And.Contain("3,000 XP per hour").And.Contain("137 hours");
        }

        [Test]
        public void TheReportIsPlainAscii()
        {
            var report = ProgressionReport.Build(ProgressionFixture.RealRecipes, Assumptions, "cmd");

            report.Where(c => c > 127).Distinct().Should().BeEmpty();
        }

        [Test]
        public void ARunOfOneSkillSkipsTheSectionsItCannotFill()
        {
            var model = ProgressionFixture.Model(
                SharedData.Recipes, ProgressionFixture.Supply, ProgressionFixture.Guilds, 0.95, 0.3, 15, new[] { SkillType.Engineering });

            var report = ProgressionReport.Build(model, Assumptions, "cmd");

            report.Should().Contain("### Engineering").And.NotContain("### Smithery").And.NotContain("## Research cohort");
        }

        [Test]
        public void TheProgressionCommandWritesTheReportAndJsonFromASeed()
        {
            var report = Path.Combine(_directory, "report.md");
            var json = Path.Combine(_directory, "results.json");
            var output = new StringWriter();

            var code = ProgressionCommand.Run(new[] { "--runs", "3", "--seed", "7", "--skills", "Smithery", "--out", report, "--json", json }, output);

            code.Should().Be(0);
            output.ToString().Should().Contain("Wrote");
            File.ReadAllText(report).Should().Contain("# Crafting revamp: progression pace baseline (workstream 11)").And.Contain("--seed 7");
            var results = JObject.Parse(File.ReadAllText(json));
            results["Seed"].Value<int>().Should().Be(7);
            results["Runs"].Value<int>().Should().Be(3);
            results["Crafting"].Select(p => p["Skill"].Value<string>()).Distinct().Should().BeEquivalentTo("Smithery");
            results["Gathering"].Should().HaveCount(3);
        }

        [Test]
        public void ASeedRepeatsTheResultsExactly()
        {
            string Results(int seed)
            {
                var path = Path.Combine(_directory, "results.json");
                ProgressionCommand.Run(new[] { "--runs", "3", "--seed", seed.ToString(), "--skills", "Agriculture", "--json", path, "--out", Path.Combine(_directory, "r.md") }, new StringWriter());
                return File.ReadAllText(path);
            }

            Results(4).Should().Be(Results(4));
            Results(4).Should().NotBe(Results(5));
        }

        [TestCase("--runs", "0", "--runs needs a whole number")]
        [TestCase("--skills", "Armor", "--skills names a crafting skill")]
        [TestCase("--skills", "", "--skills needs at least one crafting skill")]
        [TestCase("--skills", ",", "--skills needs at least one crafting skill")]
        [TestCase("--skills", " , ", "--skills needs at least one crafting skill")]
        [TestCase("--node-seconds", "0", "--node-seconds needs a number above zero")]
        [TestCase("--might", "9", "--might needs a whole number")]
        [TestCase("--bogus", "1", "Unknown option '--bogus'")]
        public void BadOptionsAreRejected(string option, string value, string message)
        {
            var act = () => ProgressionCommand.Run(new[] { option, value }, new StringWriter());

            act.Should().Throw<ArgumentException>().WithMessage($"*{message}*");
        }

        [Test]
        public void TheNodeTimeOptionChangesTheGatheringHours()
        {
            string Json(string seconds)
            {
                var path = Path.Combine(_directory, $"nodes-{seconds}.json");
                ProgressionCommand.Run(new[] { "--runs", "2", "--skills", "Fabrication", "--node-seconds", seconds, "--json", path, "--out", Path.Combine(_directory, "n.md") }, new StringWriter());
                return File.ReadAllText(path);
            }

            var near = JObject.Parse(Json("15"))["Gathering"].Last["TotalHours"].Value<double>();
            var far = JObject.Parse(Json("120"))["Gathering"].Last["TotalHours"].Value<double>();

            far.Should().BeGreaterThan(near * 3);
        }
    }
}
