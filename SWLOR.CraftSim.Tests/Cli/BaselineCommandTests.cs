using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.CraftSim.Cli;

namespace SWLOR.CraftSim.Tests.Cli
{
    [TestFixture]
    public class BaselineCommandTests
    {
        private string _directory;

        [SetUp]
        public void CreateFolder() => _directory = Directory.CreateTempSubdirectory("craftsim-cli-").FullName;

        [TearDown]
        public void DeleteFolder() => Directory.Delete(_directory, true);

        [Test]
        public void TheGatheringSectionWritesTheReportAndJson()
        {
            var report = Path.Combine(_directory, "report.md");
            var json = Path.Combine(_directory, "results.json");
            var output = new StringWriter();

            var code = BaselineCommand.Run(
                new[] { "--section", "gathering", "--gathering-runs", "200", "--seed", "5", "--out", report, "--json", json }, output);

            code.Should().Be(0);
            output.ToString().Should().Contain("Wrote");
            File.ReadAllText(report).Should().Contain("# Crafting revamp: legacy baseline of gathering, refining and fishing");
            var results = JObject.Parse(File.ReadAllText(json));
            results["gathering"]["Seed"].Value<int>().Should().Be(5);
            results["gathering"]["Trials"].Value<int>().Should().Be(200);
            results.ContainsKey("crafting").Should().BeFalse();
        }

        [Test]
        public void ASeedRepeatsTheResultsExactly()
        {
            string Results(int seed)
            {
                var path = Path.Combine(_directory, "results.json");
                BaselineCommand.Run(new[] { "--section", "gathering", "--gathering-runs", "300", "--seed", seed.ToString(), "--json", path, "--out", Path.Combine(_directory, "r.md") }, new StringWriter());
                return File.ReadAllText(path);
            }

            Results(4).Should().Be(Results(4));
            Results(4).Should().NotBe(Results(5));
        }

        [Test]
        public void TheCraftingSectionPlaysTheRequestedSkillsOnly()
        {
            var json = Path.Combine(_directory, "crafting.json");

            BaselineCommand.Run(new[] { "--section", "crafting", "--runs", "3", "--skills", "Smithery, espionage", "--json", json, "--out", Path.Combine(_directory, "c.md") }, new StringWriter())
                .Should().Be(0);

            var cells = JObject.Parse(File.ReadAllText(json))["crafting"]["Cells"];
            cells.Select(c => c["Skill"].Value<string>()).Distinct().Should().BeEquivalentTo("Smithery", "Espionage");
            cells.Select(c => c["Policy"].Value<string>()).Distinct().Should().BeEquivalentTo("Dominant rotation", "Lookahead");
            cells.Select(c => c["Runs"].Value<int>()).Distinct().Should().Equal(3);
        }

        [TestCase("--section", "fishing", "--section must be")]
        [TestCase("--runs", "0", "--runs needs a whole number")]
        [TestCase("--gathering-runs", "x", "--gathering-runs needs a whole number")]
        [TestCase("--skills", "Armor", "--skills names a crafting skill")]
        [TestCase("--bogus", "1", "Unknown option '--bogus'")]
        public void BadOptionsAreRejected(string option, string value, string message)
        {
            var args = new[] { "--section", "gathering", option, value };
            if (option == "--section")
                args = new[] { option, value };

            var act = () => BaselineCommand.Run(args, new StringWriter());

            act.Should().Throw<ArgumentException>().WithMessage($"*{message}*");
        }

        [Test]
        public void TwoOutputsCannotShareAFile()
        {
            var path = Path.Combine(_directory, "same.md");

            var act = () => BaselineCommand.Run(new[] { "--section", "gathering", "--out", path, "--json", path }, new StringWriter());

            act.Should().Throw<ArgumentException>().WithMessage("*name the same file*");
        }

        [Test]
        public void WithoutAnOutFileTheReportGoesToTheOutput()
        {
            var output = new StringWriter();

            BaselineCommand.Run(new[] { "--section", "gathering", "--gathering-runs", "100" }, output).Should().Be(0);

            output.ToString().Should().Contain("## Harvesting");
        }

        [Test]
        public void TheGearCommandPrintsEverySkillsTiers()
        {
            var output = new StringWriter();

            GearCommand.Run(Array.Empty<string>(), output).Should().Be(0);

            output.ToString().Should().Contain("Smithery: 5 tiers").And.Contain("tier 5: Control 29, Craftsmanship 30, CP 37");
            output.ToString().Should().Contain("Agriculture: 5 tiers");
        }
    }
}
