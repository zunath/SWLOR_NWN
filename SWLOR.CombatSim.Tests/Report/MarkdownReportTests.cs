using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Report;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;

namespace SWLOR.CombatSim.Tests.Report
{
    [TestFixture]
    public class MarkdownReportTests
    {
        [Test]
        public void PerCombatantTable_SaysPlainlyWhatTheAffordabilityColumnsMean()
        {
            var report = MarkdownReport.Build(Scenario(), new[] { SummaryWithOneStarvedCombatant() }, includeCombatants: true);

            report.Should().Contain("| Couldn't afford a wanted ability | First time: median |");
            report.Should().NotContain("Runs out of resources");
            report.Should().NotContain("First runs out");
        }

        [Test]
        public void PerCombatantTable_ShowsTheShareOfRunsAndTheMedianFirstTime()
        {
            var report = MarkdownReport.Build(Scenario(), new[] { SummaryWithOneStarvedCombatant() }, includeCombatants: true);

            report.Should().Contain("| Caster | First | 0.00 |").And.Contain("1:30 |");
        }

        [Test]
        public void PerCombatantTable_ReportsNeverWhenNobodyCouldNotAfford()
        {
            var summary = SummaryWithOneStarvedCombatant();
            summary.Combatants[0].ShareOfRunsStarved = 0;
            summary.Combatants[0].MedianFirstStarvedAt = null;

            var report = MarkdownReport.Build(Scenario(), new[] { summary }, includeCombatants: true);

            report.Should().Contain("never |");
        }

        [Test]
        public void SummaryTable_ShowsHowOftenEachTeamWon()
        {
            var summary = SummaryWithOneStarvedCombatant();
            summary.WinsByTeam[0] = 6;
            summary.WinsByTeam[1] = 3;

            var report = MarkdownReport.Build(Scenario(), new[] { summary }, includeCombatants: false);

            report.Should().Contain("| Wins by team |").And.Contain("| First 60%, Second 30% |");
        }

        [Test]
        public void WithoutCombatantDetail_TheAffordabilityColumnsAreNotShown()
        {
            var report = MarkdownReport.Build(Scenario(), new[] { SummaryWithOneStarvedCombatant() }, includeCombatants: false);

            report.Should().NotContain("Couldn't afford");
        }

        private static ScenarioSpec Scenario() =>
            TestScenarios.TwoTeams(false, new[] { TestCombatants.Dummy("Caster") }, new[] { TestCombatants.Dummy("Enemy", true) });

        private static BatchSummary SummaryWithOneStarvedCombatant()
        {
            var summary = new BatchSummary { RuleSetName = "current", Runs = 10 };
            summary.Combatants.Add(new CombatantSummary
            {
                Name = "Caster",
                TeamIndex = 0,
                ShareOfRunsStarved = 0.4,
                MedianFirstStarvedAt = 90
            });
            return summary;
        }
    }
}
