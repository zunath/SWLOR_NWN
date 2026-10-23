using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Cli;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Search;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;

namespace SWLOR.CraftSim.Tests.Search
{
    [TestFixture]
    public class SearchReportTests
    {
        private static readonly SearchParityOptions Small = SearchParityOptions.Default with
        {
            Ranks = new[] { 0, 20, 40 },
            SiteLevels = new[] { 1, 3 },
            QuickSearchRuns = 20_000,
            Tolerance = 0.06,
            RareSupplyTables = new[] { "SCAVENGING_BUSH_1", "SUPPLY_CRATE_3" }
        };

        private static string WriteReport(out IReadOnlyList<GateResult> results)
        {
            var parity = new SearchParity(600, 5);
            var context = new GateContext(null, null, 600, 5, AttributeSweep.Default);
            results = SearchReport.Run(context, parity, Small, GateThresholds.Default);
            return SearchReport.Build(context, parity, Small, GateThresholds.Default, results, "SWLOR.CraftSim search --runs 600");
        }

        [Test]
        public void TheReportHoldsTheThreeGatesQuickSearchAndTheEvidenceTheDraftQuotes()
        {
            var report = WriteReport(out var results);

            results.Select(result => result.Gate).Should().Equal(
                GateNames.EconomyParity,
                GateNames.Length,
                GateNames.XpRate,
                SearchReport.QuickSearchXpGate,
                "Evidence: XP rate by player",
                "Evidence: rare supply",
                "Evidence: Treasure Seeker magnitudes");
            report.Should().StartWith("# " + SearchReport.Title);
            report.Should().Contain("## Inputs").And.Contain("## Verdicts").And.Contain("## Economy parity").And.Contain("## Length").And.Contain("## XP rate")
                .And.Contain("## Quick Search XP rate").And.Contain("## Evidence: rare supply").And.Contain("## Evidence: Treasure Seeker magnitudes").And.Contain("## Limitations");
            report.Should().Contain("Keen Eye").And.Contain("Steady Dig").And.Contain("Careful Hands");
        }

        [Test]
        public void TheReportRecordsEveryStartingValueOfTheBoardByName()
        {
            var values = SearchReport.StartingValues();

            values.Should().Contain(("OrdinaryChance", SearchRules.OrdinaryChance.ToString()));
            values.Should().Contain(("DigRiskBase", SearchRules.DigRiskBase.ToString()));
            values.Select(value => value.Name).Should().OnlyHaveUniqueItems();
            WriteReport(out _).Should().Contain($"`ActionSeconds` = {SearchRules.ActionSeconds}");
        }

        [Test]
        public void TheReportIsAsciiAndNamesThePlayersTheParityReferenceAndTheTimeItAssumes()
        {
            var report = WriteReport(out _);

            report.Where(character => character > 127).Should().BeEmpty();
            report.Should().Contain("Careless").And.Contain("Skilled").And.Contain("Oracle").And.Contain("Perception modifier");
            report.Should().Contain("30.0 s to reach and open each site");
        }

        [Test]
        public void TheRareSupplyEvidenceNamesTheRealTablesItWasAskedFor()
        {
            var report = WriteReport(out _);

            report.Should().Contain("SCAVENGING_BUSH_1").And.Contain("SUPPLY_CRATE_3").And.NotContain("KORRIBAN_SITH_CRATE_3");
        }

        [Test]
        public void TheCommandWritesTheReportAndTheJsonAndExitsCleanly()
        {
            var folder = Path.Combine(Path.GetTempPath(), "craftsim-search-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                var report = Path.Combine(folder, "report.md");
                var json = Path.Combine(folder, "results.json");
                var output = new StringWriter();

                var code = SearchCommand.Run(new[] { "--runs", "300", "--seed", "3", "--out", report, "--json", json }, output, Small);

                code.Should().Be(0);
                File.ReadAllText(report).Should().StartWith("# " + SearchReport.Title);
                File.ReadAllText(json).Should().Contain("\"seed\": 3").And.Contain("Economy parity");
                output.ToString().Should().Contain("Economy parity").And.Contain("XP rate");
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public void TheCommandRejectsAnOutputPathUsedTwice()
        {
            var path = Path.Combine(Path.GetTempPath(), "craftsim-search-collision.md");

            var run = () => SearchCommand.Run(new[] { "--out", path, "--json", path }, new StringWriter(), Small);

            run.Should().Throw<ArgumentException>();
        }
    }
}
