using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;

namespace SWLOR.Game.Server.Tests.Service.Research;

/// <summary>
/// Covers what Fabrication gear and the research tier change in the research Experiment: Control raises Test chance (more slowly above
/// the best unmodded gear), Craftsmanship adds trials, and each research tier above tier 2 lowers the base Test chance.
/// </summary>
public class ExperimentGearTests
{
    private const int FixedLevel = 1;

    [TestCase(0, 0)]
    [TestCase(1, 0)]
    [TestCase(2, 1)]
    [TestCase(13, 6)]
    [TestCase(29, 14)]
    [TestCase(30, 15)]
    [TestCase(33, 15)]
    [TestCase(34, 16)]
    [TestCase(119, 37)]
    [TestCase(-5, 0)]
    public void ControlAddsOnePointPerTwoUpToThirty_ThenOnePointPerFour(int control, int expected)
    {
        ExperimentSetup.ControlChance(control).Should().Be(expected);
    }

    [TestCase(0, 0)]
    [TestCase(30, 0)]
    [TestCase(119, 0)]
    [TestCase(120, 1)]
    [TestCase(240, 2)]
    [TestCase(-5, 0)]
    public void EachHundredAndTwentyCraftsmanshipAddsATrial(int craftsmanship, int expected)
    {
        ExperimentSetup.TrialsFromCraftsmanship(craftsmanship).Should().Be(expected);
        new ExperimentSetup(FixedLevel, 10, 10, 0, false, craftsmanship: craftsmanship).Trials.Should().Be(ExperimentRules.Trials + expected);
    }

    [TestCase(10, 0)]
    [TestCase(20, 0)]
    [TestCase(30, 5)]
    [TestCase(40, 10)]
    [TestCase(50, 15)]
    public void EachResearchTierAboveTierTwoTakesFivePointsOfTestChance(int requiredRank, int penalty)
    {
        var setup = new ExperimentSetup(FixedLevel, requiredRank, requiredRank, 0, false);

        setup.TierChancePenalty.Should().Be(penalty);
        setup.BaseTestChancePercent.Should().Be(ExperimentRules.BaseTestChancePercent - penalty);
    }

    [Test]
    public void ControlStacksWithTheRankAndThePerksUnderTheCap()
    {
        // Tier 5 at rank 50: 60 - 15 for the tier, + 15 for best unmodded gear's 30 Control, + 4 perk points.
        var setup = new ExperimentSetup(FixedLevel, 50, 50, 0, false, ExperimentTuning.Default with { TestChanceBonusPercent = 4 }, control: 30);
        setup.ControlChanceBonus.Should().Be(15);
        setup.BaseTestChancePercent.Should().Be(64);

        new ExperimentSetup(FixedLevel, 50, 10, 0, false, control: 119).BaseTestChancePercent
            .Should().Be(ExperimentRules.MaximumTestChancePercent, "60 + 15 rank + 37 Control is over the cap");
    }

    [Test]
    public void TheEngineGivesTheCraftsmanshipTrialsAndTheControlChance()
    {
        var setup = new ExperimentSetup(FixedLevel, 40, 40, 0, false, control: 20, craftsmanship: 120);
        var engine = new ExperimentEngine(new ScriptedMinigameRandom(), setup);

        engine.Meters.Progress.Max.Should().Be(ExperimentRules.Trials + 1);
        engine.TestChancePercent.Should().Be(60 - 10 + 10);
        engine.Preview(ExperimentActionIds.TestFixed).Outcomes[0].ChancePercent.Should().Be(60);
    }
}
