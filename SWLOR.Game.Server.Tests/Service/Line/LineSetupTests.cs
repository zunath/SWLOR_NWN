using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;
using static SWLOR.Game.Server.Tests.Service.Line.LineTestData;

namespace SWLOR.Game.Server.Tests.Service.Line;

/// <summary>Covers how a fight's numbers come from the fish's level and rarity, the angler's rank and rod, and the angler's perks.</summary>
public class LineSetupTests
{
    [Test]
    public void AFishAtTheAnglersRank_IsOnTheRungTheLadderStartsFrom()
    {
        LineSetup.Rung(25, 25).Should().Be(LineRules.RungAtRank);

        var setup = LineSetup.ForFish(25, FishRarity.Common, 25, 2);

        // Cap 20 + 2 + 25 / 12; reel 6 + (2 + 2) / 6 x 2; start 5 + 2 x 6; escape 17 + 2 + 2 x 6; rung 54 gives a 53.2% pull of 24.
        setup.TensionCap.Should().Be(24);
        setup.ReelDistance.Should().Be(6);
        setup.StartDistance.Should().Be(17);
        setup.DistanceMax.Should().Be(31);
        setup.Pull.Should().Be(13);
        setup.Stamina.Should().Be(21);
        setup.Difficulty.Should().Be(0);
        setup.Pattern.Weight(null, FishMove.Tire).Should().Be(21, "the Tire weight on rung 54 is 45 less 0.45 per rung");
    }

    [TestCase(25, 25, 54)]
    [TestCase(26, 25, 59)]
    [TestCase(30, 25, 79)]
    [TestCase(24, 25, 49)]
    [TestCase(20, 25, 29)]
    [TestCase(10, 25, 0)]
    [TestCase(40, 25, 100)]
    [TestCase(52, 50, 64)]
    public void TheRungRisesFiveForEachLevelTheFishIsAboveTheRank_WithinTheLadder(int level, int rank, int rung)
    {
        LineSetup.Rung(level, rank).Should().Be(rung);
    }

    [Test]
    public void EachLevelAboveTheRank_NeverMakesTheFishWeakerOrMoreRestful()
    {
        var last = LineSetup.ForFish(5, FishRarity.Common, 25, 2);
        for (var level = 6; level <= 45; level++)
        {
            var next = LineSetup.ForFish(level, FishRarity.Common, 25, 2);

            next.Pull.Should().BeGreaterThanOrEqualTo(last.Pull, $"level {level}");
            next.Stamina.Should().BeGreaterThanOrEqualTo(last.Stamina, $"level {level}");
            next.Pattern.Weight(null, FishMove.Tire).Should().BeLessThanOrEqualTo(last.Pattern.Weight(null, FishMove.Tire), $"level {level}");
            last = next;
        }

        LineSetup.ForFish(45, FishRarity.Common, 25, 2).Pull.Should().BeGreaterThan(LineSetup.ForFish(5, FishRarity.Common, 25, 2).Pull);
    }

    [Test]
    public void TheRodTierAndTheRank_SetTheTensionCapAndTheReelStrength()
    {
        var bare = LineSetup.ForFish(0, FishRarity.Common, 0, 0);
        var best = LineSetup.ForFish(0, FishRarity.Common, 50, 4);

        bare.TensionCap.Should().Be(20);
        bare.ReelDistance.Should().Be(6);
        best.TensionCap.Should().Be(20 + 4 + 4);
        best.ReelDistance.Should().Be(8);
        LineSetup.BaseTensionCap(4, 0).Should().BeGreaterThan(LineSetup.BaseTensionCap(0, 0));
        LineSetup.BaseTensionCap(0, 50).Should().BeGreaterThan(LineSetup.BaseTensionCap(0, 0));
        LineSetup.BaseReelDistance(4, 20).Should().BeGreaterThan(LineSetup.BaseReelDistance(0, 0));
    }

    [Test]
    public void AFartherReelHooksTheFishFartherOut_SoAFightTakesAboutAsManyReels()
    {
        LineSetup.BaseStartDistance(6).Should().Be(17);
        LineSetup.BaseDistanceMax(6).Should().Be(31);
        LineSetup.BaseStartDistance(8).Should().Be(21);
        LineSetup.BaseDistanceMax(8).Should().Be(39);
    }

    [Test]
    public void TheFishsPull_IsAShareOfTheLineBeforePerks_SoAPerkThatRaisesTheCapDoesNotWeakenTheFish()
    {
        var plain = LineSetup.ForFish(30, FishRarity.Common, 25, 2);
        var strong = LineSetup.ForFish(30, FishRarity.Common, 25, 2, new LineAnglerEffects(TensionCapBonus: 3));

        strong.TensionCap.Should().Be(plain.TensionCap + 3);
        strong.Pull.Should().Be(plain.Pull);
        strong.Stamina.Should().Be(plain.Stamina);
    }

    [Test]
    public void ARareFish_ChainsItsMoves_AndRestsLessThanACommonOne()
    {
        var common = LineSetup.ForFish(52, FishRarity.Common, 50, 4);
        var rare = LineSetup.ForFish(52, FishRarity.Rare, 50, 4);

        rare.Rarity.Should().Be(FishRarity.Rare);
        rare.Pull.Should().Be(common.Pull);
        rare.Stamina.Should().Be(common.Stamina);
        rare.Pattern.Weight(FishMove.Run, FishMove.Run).Should().BeGreaterThan(rare.Pattern.Weight(FishMove.Dive, FishMove.Run));
        common.Pattern.Weight(FishMove.Run, FishMove.Run).Should().Be(common.Pattern.Weight(FishMove.Dive, FishMove.Run));
        rare.Pattern.Weight(FishMove.Tire, FishMove.Tire).Should().BeLessThan(common.Pattern.Weight(FishMove.Tire, FishMove.Tire));
    }

    [Test]
    public void ThePerkAdjustments_EachChangeOnlyTheirOwnNumber()
    {
        var plain = LineSetup.ForFish(25, FishRarity.Common, 25, 2);
        var perked = LineSetup.ForFish(
            25,
            FishRarity.Common,
            25,
            2,
            new LineAnglerEffects(
                TensionCapBonus: 2,
                ReelDistanceBonus: 1,
                ReelTensionBonus: 1,
                GiveLineDistanceReduction: 1,
                SetHookDrainBonus: 2,
                HoldDrainBonus: 1,
                ForecastDepth: 2));

        perked.TensionCap.Should().Be(plain.TensionCap + 2);
        perked.ReelDistance.Should().Be(plain.ReelDistance + 1);
        perked.ReelTension.Should().Be(plain.ReelTension + 1);
        perked.GiveLineDistance.Should().Be(plain.GiveLineDistance - 1);
        perked.SetHookDrain.Should().Be(plain.SetHookDrain + 2);
        perked.HoldDrain.Should().Be(plain.HoldDrain + 1);
        perked.ForecastDepth.Should().Be(2);
        plain.ForecastDepth.Should().Be(1);
        perked.Pull.Should().Be(plain.Pull);
        perked.StartDistance.Should().Be(plain.StartDistance, "the fish is hooked where it always is");
    }

    [Test]
    public void TheRarityPerk_MovesOnlyRareFishDownTheLadder()
    {
        var perk = new LineAnglerEffects(RareFishRungReduction: 10);

        var common = LineSetup.ForFish(52, FishRarity.Common, 50, 4, perk);
        var plainCommon = LineSetup.ForFish(52, FishRarity.Common, 50, 4);
        var rare = LineSetup.ForFish(52, FishRarity.Rare, 50, 4, perk);
        var plainRare = LineSetup.ForFish(52, FishRarity.Rare, 50, 4);

        common.Pull.Should().Be(plainCommon.Pull);
        common.Stamina.Should().Be(plainCommon.Stamina);
        rare.Pull.Should().BeLessThan(plainRare.Pull);
        rare.Stamina.Should().BeLessThan(plainRare.Stamina);
    }

    [Test]
    public void ARareFish_IgnoresTheRestOfTheLine_SoOnlyTrophyCatchHelpsAgainstIt()
    {
        var wholeLine = new LineAnglerEffects(
            TensionCapBonus: 2,
            ReelDistanceBonus: 1,
            ReelTensionBonus: 1,
            GiveLineDistanceReduction: 1,
            SetHookDrainBonus: 2,
            HoldDrainBonus: 1,
            ForecastDepth: 2);
        var plain = LineSetup.ForFish(52, FishRarity.Rare, 50, 4);

        var withLine = LineSetup.ForFish(52, FishRarity.Rare, 50, 4, wholeLine);
        var withTrophy = LineSetup.ForFish(52, FishRarity.Rare, 50, 4, wholeLine with { RareFishRungReduction = 12 });
        var common = LineSetup.ForFish(52, FishRarity.Common, 50, 4, wholeLine);

        withLine.TensionCap.Should().Be(plain.TensionCap);
        withLine.ReelDistance.Should().Be(plain.ReelDistance);
        withLine.ReelTension.Should().Be(plain.ReelTension);
        withLine.GiveLineDistance.Should().Be(plain.GiveLineDistance);
        withLine.SetHookDrain.Should().Be(plain.SetHookDrain);
        withLine.HoldDrain.Should().Be(plain.HoldDrain);
        withLine.ForecastDepth.Should().Be(1, "Fish Sense does not read a rare fish's next two moves");
        withLine.Pull.Should().Be(plain.Pull);

        withTrophy.TensionCap.Should().Be(plain.TensionCap, "Trophy Catch is the one perk a rare fish lets through");
        withTrophy.ForecastDepth.Should().Be(1);
        withTrophy.Pull.Should().BeLessThan(plain.Pull);
        withTrophy.Stamina.Should().BeLessThan(plain.Stamina);

        common.TensionCap.Should().Be(plain.TensionCap + 2, "a common fish of the same level takes the whole line");
        common.ForecastDepth.Should().Be(2);
    }

    [Test]
    public void GiveLine_CannotLetOutLessThanNothing()
    {
        var setup = LineSetup.ForFish(25, FishRarity.Common, 25, 2, new LineAnglerEffects(GiveLineDistanceReduction: 9));

        setup.GiveLineDistance.Should().Be(0);
    }

    [Test]
    public void ASetupWithNumbersThatCannotMakeAFight_IsRefused()
    {
        var pattern = LineMovePattern.Common(20);

        ((Action)(() => new LineSetup(0, 3, 8, 15, 8, 2, pattern))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => new LineSetup(10, 0, 8, 15, 8, 2, pattern))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => new LineSetup(10, 3, 0, 15, 8, 2, pattern))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => new LineSetup(10, 3, 15, 15, 8, 2, pattern))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => new LineSetup(10, 3, 8, 15, 0, 2, pattern))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => new LineSetup(10, 3, 8, 15, 8, -1, pattern))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => new LineSetup(10, 3, 8, 15, 8, 2, null))).Should().Throw<ArgumentNullException>();
        ((Action)(() => new LineSetup(10, 3, 8, 15, 8, 2, pattern, new LineAnglerEffects(ForecastDepth: 3))))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void TheHighestRungs_AreWorthAFishNoOneLands()
    {
        var hopeless = LineSetup.ForFish(80, FishRarity.Common, 25, 2);

        hopeless.Pull.Should().BeGreaterThan(hopeless.ReelTension + hopeless.TensionCap / 2);
        hopeless.Stamina.Should().BeGreaterThan(28);
        hopeless.Pattern.Weight(null, FishMove.Tire).Should().BeLessThan(10);
    }

    [Test]
    public void AFightIsBuiltForTheRodsTier()
    {
        FishingRodTier.Tier(FishingRodType.Yew).Should().Be(3);
        var setup = LineSetup.ForFish(35, FishRarity.Common, 35, FishingRodTier.Tier(FishingRodType.Yew));

        setup.RodTier.Should().Be(3);
        setup.TensionCap.Should().Be(20 + 3 + 35 / 12);
    }
}
