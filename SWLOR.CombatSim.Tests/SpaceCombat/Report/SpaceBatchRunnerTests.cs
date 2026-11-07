using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.SpaceCombat.Report;

namespace SWLOR.CombatSim.Tests.SpaceCombat.Report;

[TestFixture]
public class SpaceBatchRunnerTests
{
    private static SWLOR.CombatSim.SpaceCombat.Scenario.SpaceScenarioSpec Scenario() =>
        SpaceTestShips.Scenario(
            SpaceTestShips.Ship("Gunship", hull: 500, shield: 100, weapons: SpaceTestShips.Laser(dmg: 30)),
            SpaceTestShips.Ship("Target", hull: 400, shield: 200), durationSeconds: 300);

    [Test]
    public void Run_SameSeed_GivesTheSameSummary()
    {
        var first = SpaceBatchRunner.Run(Scenario(), runs: 25, seed: 7);
        var second = SpaceBatchRunner.Run(Scenario(), runs: 25, seed: 7);

        second.TimesToKill.Should().Equal(first.TimesToKill);
        second.DamagePerSecond.Should().Equal(first.DamagePerSecond);
        second.AttackerWins.Should().Be(first.AttackerWins);
    }

    [Test]
    public void Run_AgainstAPassiveDefender_WinsEveryRunWithAFullAttacker()
    {
        var summary = SpaceBatchRunner.Run(Scenario(), runs: 20, seed: 1);

        summary.AttackerWinRate.Should().Be(1);
        summary.RunsWithKill.Should().Be(20);
        summary.MedianAttackerHullRemaining.Should().Be(500);
        summary.MedianAttackerShieldRemaining.Should().Be(100);
        summary.P10TimeToKill.Should().BeLessThanOrEqualTo(summary.MedianTimeToKill.Value);
        summary.P90TimeToKill.Should().BeGreaterThanOrEqualTo(summary.MedianTimeToKill.Value);
        summary.MedianDamagePerSecond.Should().BeGreaterThan(0);
    }

    [Test]
    public void Run_WhenNothingIsDestroyed_ReportsNoTimeToKill()
    {
        var scenario = SpaceTestShips.Scenario(SpaceTestShips.Ship("Idle"), SpaceTestShips.Ship("Target"), durationSeconds: 5);

        var summary = SpaceBatchRunner.Run(scenario, runs: 3, seed: 1);

        summary.RunsWithKill.Should().Be(0);
        summary.MedianTimeToKill.Should().BeNull();
        summary.AttackerWinRate.Should().Be(0);
    }

    [Test]
    public void Report_NamesTheWinRateAndTheTimeToKill()
    {
        var scenario = Scenario();
        var summary = SpaceBatchRunner.Run(scenario, runs: 10, seed: 1);

        var report = SpaceMarkdownReport.Build(scenario, summary);

        report.Should().Contain("Time to kill: median (p10-p90)").And.Contain("100%").And.Contain("500 of 500");
    }
}
