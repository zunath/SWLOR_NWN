using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Service.Line;

/// <summary>
/// Covers what an Angler's perks change in the Line: the numbers the engine reads from <see cref="LineAnglerEffects"/>, Fish Sense and
/// how many turns it lasts, how a rare fish takes the line, and that a bite carries the angler's perks into the fight.
/// </summary>
public class LineAnglerPerkTests
{
    private static LineAnglerEffects FromTotals(Dictionary<StatType, int> totals) =>
        LineAnglerEffects.FromStatTotals(stat => totals.GetValueOrDefault(stat));

    [Test]
    public void AnAnglerWithNoPerks_HasTheDefaultEffects()
    {
        LineAnglerEffects.None.HasAny.Should().BeFalse();
        FromTotals(new Dictionary<StatType, int>()).Should().Be(LineAnglerEffects.None);
        LineAnglerEffects.None.ForecastDepth.Should().Be(1);
    }

    [Test]
    public void TheEffectsAreReadFromTheAnglersStatTotals()
    {
        var effects = FromTotals(new Dictionary<StatType, int>
        {
            [StatType.AnglerTensionCap] = 8,
            [StatType.AnglerSetHookDrain] = 3,
            [StatType.AnglerForecastTurns] = 3,
            [StatType.AnglerGiveLineDistanceReduction] = 1,
            [StatType.AnglerReelDistance] = 1,
            [StatType.AnglerRareFishRungs] = 4
        });

        effects.Should().Be(new LineAnglerEffects(
            TensionCapBonus: 8, ReelDistanceBonus: 1, GiveLineDistanceReduction: 1, SetHookDrainBonus: 3, ForecastDepth: 2,
            RareFishRungReduction: 4, ForecastTurns: 3));
        effects.HasAny.Should().BeTrue();
    }

    [Test]
    public void FishSenseIsOffWithoutTheStat_AndOnForAsManyTurnsAsItSays()
    {
        var off = FromTotals(new Dictionary<StatType, int>());
        var one = FromTotals(new Dictionary<StatType, int> { [StatType.AnglerForecastTurns] = 1 });
        var three = FromTotals(new Dictionary<StatType, int> { [StatType.AnglerForecastTurns] = 3 });

        off.ForecastDepth.Should().Be(1);
        one.ForecastDepth.Should().Be(2);
        one.ForecastTurns.Should().Be(1);
        three.ForecastTurns.Should().Be(3);

        var setup = LineSetup.ForFish(30, FishRarity.Common, 30, 3, three);
        new[] { 0, 1, 2 }.Should().OnlyContain(round => setup.SeesTwoMoves(round));
        setup.SeesTwoMoves(3).Should().BeFalse("it ends after the third turn");
        LineSetup.ForFish(30, FishRarity.Common, 30, 3, one).SeesTwoMoves(1).Should().BeFalse();
        LineSetup.ForFish(30, FishRarity.Common, 30, 3).SeesTwoMoves(0).Should().BeFalse();
    }

    [Test]
    public void ANegativeStatTotalIsNeverAPenalty()
    {
        var effects = FromTotals(new Dictionary<StatType, int> { [StatType.AnglerTensionCap] = -3, [StatType.AnglerSetHookDrain] = -1 });

        effects.Should().Be(LineAnglerEffects.None);
    }

    [Test]
    public void TheWholeLineAtRankFifty_OnARareFish_IsAHalfStrengthLine()
    {
        var line = new LineAnglerEffects(
            TensionCapBonus: 8, SetHookDrainBonus: 3, GiveLineDistanceReduction: 1, ForecastDepth: 2, ForecastTurns: 3);
        var plain = LineSetup.ForFish(52, FishRarity.Rare, 50, 4);

        var rare = LineSetup.ForFish(52, FishRarity.Rare, 50, 4, line);

        rare.TensionCap.Should().Be(plain.TensionCap + 4, "half of 8");
        rare.SetHookDrain.Should().Be(plain.SetHookDrain + 1, "half of 3, rounded down");
        rare.GiveLineDistance.Should().Be(plain.GiveLineDistance, "a style's single point does nothing against a rare fish");
        rare.ForecastDepth.Should().Be(1, "Fish Sense does not work on a rare fish");
    }

    [Test]
    public void TrophyCatchIsTheOnlyPerkAtFullStrengthOnARareFish()
    {
        var plain = LineSetup.ForFish(52, FishRarity.Rare, 50, 4);
        var trophy = LineSetup.ForFish(52, FishRarity.Rare, 50, 4, new LineAnglerEffects(RareFishRungReduction: 4));

        trophy.Pull.Should().BeLessThan(plain.Pull);
        trophy.Stamina.Should().BeLessThan(plain.Stamina);
        LineSetup.Rung(52, 50).Should().Be(LineRules.RungAtRank + 2 * LineRules.RungsPerLevelAbove);
    }

    [Test]
    public void ABiteCarriesTheAnglersPerksIntoTheFight()
    {
        Skill.CacheXPChartData();
        var world = new FakeFishingFightWorld();
        var lifecycle = new FishingFightLifecycle(world, new ScriptedMinigameRandom(99, 99, 99, 99, 99, 99, 99, 99));
        var perks = new LineAnglerEffects(TensionCapBonus: 5, SetHookDrainBonus: 2, GiveLineDistanceReduction: 1);
        FishingBite BiteWith(LineAnglerEffects effects) => new(
            11, 22, 33, new FishAttribute("Test Fish", "test_fish", 20, true), FishingRodType.GlassFiber, 20, 5, "Little Worm", 7,
            new Vector3(10f, 20f, 0f), effects);

        var plain = lifecycle.Bite(BiteWith(null));
        var perked = new FishingFightLifecycle(new FakeFishingFightWorld(), new ScriptedMinigameRandom(99, 99, 99, 99, 99, 99, 99, 99)).Bite(BiteWith(perks));

        perked.Engine.Setup.TensionCap.Should().Be(plain.Engine.Setup.TensionCap + 5);
        perked.Engine.Setup.SetHookDrain.Should().Be(plain.Engine.Setup.SetHookDrain + 2);
        perked.Engine.Setup.GiveLineDistance.Should().Be(plain.Engine.Setup.GiveLineDistance - 1);
        plain.Engine.Setup.Effects.Should().Be(LineAnglerEffects.None, "a bite with no perks fights with none");
    }

    [Test]
    public void QuickReelNeverReadsThePerks()
    {
        var perks = new LineAnglerEffects(TensionCapBonus: 8, SetHookDrainBonus: 3, ForecastDepth: 2, RareFishRungReduction: 4);

        FishingQuickReel.CatchChancePercent(30, 30).Should().Be(60);
        FishingQuickReel.CatchChancePercent(30, 30, FishingQuickReel.ReferenceWillpowerScore).Should().Be(60);
        perks.HasAny.Should().BeTrue("the perks exist, and the roll is a function of rank, fish level and Willpower only");
    }
}
