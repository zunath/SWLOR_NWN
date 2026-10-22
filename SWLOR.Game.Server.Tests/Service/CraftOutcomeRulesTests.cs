using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Covers the outcome contract every item craft ends under. The legacy helpers below copy the original formulas from
/// <c>Feature/GuiDefinition/ViewModel/CraftViewModel.cs</c> line for line, so a drift in the shared rules shows up here.
/// </summary>
public class CraftOutcomeRulesTests
{
    private static readonly float[] QualityGrid = { 0f, 0.01f, 0.1f, 0.333f, 0.5f, 0.6789f, 0.85f, 0.9999f, 1f };
    private static readonly int[] BaseXpGrid = { 0, 1, 7, 100, 333, 1250 };

    // CraftViewModel.cs:1386-1401 (CalculateXP).
    private static int LegacyXp(int baseXp, int blueprintLevel, bool firstTime, float qualityPercent)
    {
        var xp = baseXp;
        // 20% bonus for the first time.
        if (firstTime)
            xp += (int)(xp * 0.20f);
        xp += (int)(xp * (blueprintLevel * 0.5f));
        xp += (int)(xp * qualityPercent);

        return xp;
    }

    // CraftViewModel.cs:1659-1667 (ProcessFailure): the failure call passes false and 0f, then takes 15%.
    private static int LegacyFailureXp(int baseXp, int blueprintLevel)
    {
        var xp = LegacyXp(baseXp, blueprintLevel, false, 0f);
        xp = (int)(xp * 0.15f);
        return xp;
    }

    // CraftViewModel.cs:1423-1432 (ProcessSuccess vendor value).
    private static int LegacyVendorValue(int recipeLevel, float qualityPercent)
    {
        const float LevelBucketMultiplier = 48f;
        const float LevelScalingPerRecipeLevel = 9f;
        var levelBonus = LevelBucketMultiplier * ((recipeLevel / 10f) + 1) + LevelScalingPerRecipeLevel * recipeLevel;
        var scaledByQuality = (int)Math.Round(levelBonus * qualityPercent);
        var minimumVendorBonus = Math.Max(25, (int)Math.Round(recipeLevel * 1.6f));
        const float CraftedVendorBonusMultiplier = 1.225f;
        return (int)Math.Round(Math.Max(scaledByQuality, minimumVendorBonus) * CraftedVendorBonusMultiplier);
    }

    [Test]
    public void Xp_MatchesTheOriginalFormulaAcrossTheGrid()
    {
        foreach (var baseXp in BaseXpGrid)
        foreach (var blueprintLevel in new[] { 0, 1, 2, 5, 10 })
        foreach (var firstTime in new[] { false, true })
        foreach (var quality in QualityGrid)
        {
            CraftOutcomeRules.CalculateXp(baseXp, blueprintLevel, firstTime, quality)
                .Should().Be(LegacyXp(baseXp, blueprintLevel, firstTime, quality),
                    $"base {baseXp}, blueprint {blueprintLevel}, first {firstTime}, quality {quality}");
        }
    }

    [TestCase(100, 1.0, 100)]
    [TestCase(100, 1.6, 160)]
    [TestCase(333, 1.6, 533)]
    [TestCase(0, 1.6, 0)]
    [TestCase(5, 1.5, 8)]
    public void ScaleBaseXp_RoundsTheScaledBaseToAWholeNumber(int baseXp, double multiplier, int expected)
    {
        CraftOutcomeRules.ScaleBaseXp(baseXp, multiplier).Should().Be(expected);
    }

    [Test]
    public void Xp_KnownValue()
    {
        // 100 -> +20% first craft = 120 -> +100% from a level 2 blueprint = 240 -> +50% quality = 360.
        CraftOutcomeRules.CalculateXp(100, 2, true, 0.5f).Should().Be(360);
    }

    [Test]
    public void FailureXp_IsFifteenPercentOfTheBaseWithTheBlueprintBonus()
    {
        foreach (var baseXp in BaseXpGrid)
        foreach (var blueprintLevel in new[] { 0, 1, 2, 5, 10 })
        {
            CraftOutcomeRules.CalculateFailureXp(baseXp, blueprintLevel)
                .Should().Be(LegacyFailureXp(baseXp, blueprintLevel));
        }

        CraftOutcomeRules.CalculateFailureXp(100, 0).Should().Be(15);
        CraftOutcomeRules.CalculateFailureXp(100, 2).Should().Be(30);
    }

    [Test]
    public void VendorValue_MatchesTheOriginalFormulaAcrossTheGrid()
    {
        for (var level = 0; level <= 60; level++)
        foreach (var quality in QualityGrid)
        {
            CraftOutcomeRules.CalculateVendorValueBonus(level, quality)
                .Should().Be(LegacyVendorValue(level, quality), $"level {level}, quality {quality}");
        }
    }

    [TestCase(10, 1f, 228)]
    [TestCase(10, 0f, 31)]
    [TestCase(50, 0.5f, 452)]
    [TestCase(0, 0f, 31)]
    public void VendorValue_KnownValues(int level, float quality, int expected)
    {
        CraftOutcomeRules.CalculateVendorValueBonus(level, quality).Should().Be(expected);
    }

    [Test]
    public void VendorValue_NeverFallsBelowTheFloorAndRisesWithQuality()
    {
        for (var level = 0; level <= 60; level += 5)
        {
            var floor = CraftOutcomeRules.CalculateVendorValueBonus(level, 0f);
            var previous = floor;
            foreach (var quality in QualityGrid)
            {
                var value = CraftOutcomeRules.CalculateVendorValueBonus(level, quality);
                value.Should().BeGreaterThanOrEqualTo(previous);
                previous = value;
            }
        }
    }

    [Test]
    public void EnhancementThresholds_StartAtFiftyAndEightyFive_AndAOneSlotItemUsesTheFirst()
    {
        CraftOutcomeRules.GetEnhancementThresholds(1).Should().Equal(50);
        CraftOutcomeRules.GetEnhancementThresholds(2).Should().Equal(50, 85);
        CraftOutcomeRules.GetEnhancementThresholds(3).Should().Equal(50, 85, 85);
        CraftOutcomeRules.GetEnhancementThresholds(0).Should().BeEmpty();
    }

    [Test]
    public void EnhancementThresholds_TheThirdSlotHasItsOwnNamedValue_StillEightyFiveUntilGateSevenSetsIt()
    {
        CraftOutcomeRules.ThirdSlotQualityThresholdPercent.Should().Be(85, "a starting value that reuses the second slot's until gate 7 sets it");
        CraftOutcomeRules.GetEnhancementThreshold(1).Should().Be(CraftOutcomeRules.FirstSlotQualityThresholdPercent);
        CraftOutcomeRules.GetEnhancementThreshold(2).Should().Be(CraftOutcomeRules.SecondSlotQualityThresholdPercent);
        CraftOutcomeRules.GetEnhancementThreshold(3).Should().Be(CraftOutcomeRules.ThirdSlotQualityThresholdPercent);
        CraftOutcomeRules.GetEnhancementThreshold(4).Should().Be(CraftOutcomeRules.ThirdSlotQualityThresholdPercent, "later slots use the third");
    }

    [TestCase(0, 100, false, false)]
    [TestCase(49, 100, false, false)]
    [TestCase(50, 100, true, false)]
    [TestCase(84, 100, true, false)]
    [TestCase(85, 100, true, true)]
    [TestCase(100, 100, true, true)]
    [TestCase(1, 2, true, false)]
    [TestCase(49, 98, true, false)]
    [TestCase(833, 1000, true, false)]
    [TestCase(850, 1000, true, true)]
    public void Enhancements_TransferExactlyWhenTheSlotThresholdIsMet(int value, int max, bool firstSlot, bool secondSlot)
    {
        var outcome = Success(new MinigameMeter(value, max), filledSlots: new[] { 1, 2 });

        outcome.Enhancements.Should().HaveCount(2);
        outcome.Enhancements[0].Should().Be(new EnhancementTransfer(1, 50, firstSlot));
        outcome.Enhancements[1].Should().Be(new EnhancementTransfer(2, 85, secondSlot));
    }

    [Test]
    public void Enhancements_AreDecidedPerSlotNotPerPosition()
    {
        // Only the second slot holds an enhancement, so it still needs the second threshold.
        var outcome = Success(new MinigameMeter(60, 100), filledSlots: new[] { 2 });

        outcome.Enhancements.Should().ContainSingle().Which.Should().Be(new EnhancementTransfer(2, 85, false));
        Success(new MinigameMeter(60, 100), filledSlots: Array.Empty<int>()).Enhancements.Should().BeEmpty();
    }

    [Test]
    public void Enhancements_RollNoChance()
    {
        // The old transfer chance was quality/max per property (CraftViewModel.cs:1420, 1455). The same outcome is returned every time.
        var first = Success(new MinigameMeter(70, 100), filledSlots: new[] { 1, 2 });
        var second = Success(new MinigameMeter(70, 100), filledSlots: new[] { 1, 2 });

        second.Enhancements.Should().Equal(first.Enhancements);
        first.Enhancements[0].Transfers.Should().BeTrue();
        first.Enhancements[1].Transfers.Should().BeFalse();
    }

    [Test]
    public void FoodDuration_FullQualityPaysTheOldHundredPercentBonus()
    {
        // CraftViewModel.cs:1471-1475: (int)qualityPercent is 1 only at 100%, and ConsumableItemDefinition pays 5 minutes per step.
        var oldFullQualityBonus = (int)1.0f;

        CraftOutcomeRules.CalculateFoodDurationBonusSeconds(1f).Should().Be(oldFullQualityBonus * 5 * 60);
    }

    [Test]
    public void FoodDuration_ScalesLinearlyAcrossTheWholeQualityRange()
    {
        CraftOutcomeRules.CalculateFoodDurationBonusSeconds(0f).Should().Be(0);
        CraftOutcomeRules.CalculateFoodDurationBonusSeconds(0.25f).Should().Be(75);
        CraftOutcomeRules.CalculateFoodDurationBonusSeconds(0.5f).Should().Be(150);
        CraftOutcomeRules.CalculateFoodDurationBonusSeconds(0.75f).Should().Be(225);

        // The old code paid nothing at any quality below 100%.
        for (var percent = 1; percent <= 99; percent++)
        {
            var seconds = CraftOutcomeRules.CalculateFoodDurationBonusSeconds(percent / 100f);
            seconds.Should().BeInRange((int)Math.Round(percent * 3.0) - 1, (int)Math.Round(percent * 3.0) + 1);
            seconds.Should().BeGreaterThan(0);
        }
    }

    [Test]
    public void FoodDuration_IsOnlyAppliedToFood()
    {
        var food = Success(new MinigameMeter(100, 100), isFood: true);
        var other = Success(new MinigameMeter(100, 100), isFood: false);

        food.FoodDurationBonusSeconds.Should().Be(300);
        other.FoodDurationBonusSeconds.Should().Be(0);
    }

    [Test]
    public void FoodCharges_KeepTheBlueprintLevelBonusAtFullQualityOnly()
    {
        // CraftViewModel.cs:1477-1482 adds the blueprint level to the charges inside the full-quality branch.
        Success(new MinigameMeter(100, 100), isFood: true, blueprintLevel: 3).FoodBonusCharges.Should().Be(3);
        Success(new MinigameMeter(99, 100), isFood: true, blueprintLevel: 3).FoodBonusCharges.Should().Be(0);
        Success(new MinigameMeter(100, 100), isFood: false, blueprintLevel: 3).FoodBonusCharges.Should().Be(0);
        Success(new MinigameMeter(100, 100), isFood: true, blueprintLevel: 0).FoodBonusCharges.Should().Be(0);
    }

    [Test]
    public void Success_CarriesQualityXpAndVendorValueFromTheSharedFormulas()
    {
        var outcome = CraftOutcomeRules.CalculateSuccess(
            new MinigameMeter(750, 1000), 30, 200, true, 2, false, new[] { 1 });

        outcome.QualityFraction.Should().Be(0.75f);
        outcome.QualityPercent.Should().Be(75);
        outcome.Xp.Should().Be(LegacyXp(200, 2, true, 0.75f));
        outcome.VendorValueBonus.Should().Be(LegacyVendorValue(30, 0.75f));
    }

    [Test]
    public void Failure_LosesAnItemOnARollOfSixtyFiveOrLower()
    {
        // Enhancements roll first, then component stacks (CraftViewModel.cs:1604-1641). Above 65 keeps the item.
        var random = new ScriptedMinigameRandom(66, 65, 1, 100);

        var failure = CraftOutcomeRules.RollFailure(random, filledEnhancementSlots: 2, componentStacks: 2, baseXp: 100, blueprintLevel: 0);

        failure.EnhancementsKept.Should().Equal(true, false);
        failure.ComponentsKept.Should().Equal(false, true);
        failure.Xp.Should().Be(15);
        random.Calls.Should().Be(4);
    }

    [Test]
    public void Failure_RollsNothingWhenNothingWasCommitted()
    {
        var random = new ScriptedMinigameRandom();

        var failure = CraftOutcomeRules.RollFailure(random, 0, 0, 100, 0);

        failure.EnhancementsKept.Should().BeEmpty();
        failure.ComponentsKept.Should().BeEmpty();
        random.Calls.Should().Be(0);
    }

    [Test]
    public void Failure_KeepsAboutThirtyFivePercent()
    {
        var kept = 0;
        const int rolls = 20000;
        var failure = CraftOutcomeRules.RollFailure(new SeededMinigameRandom(2026), 0, rolls, 100, 0);
        foreach (var component in failure.ComponentsKept)
            if (component)
                kept++;

        (kept / (double)rolls).Should().BeApproximately(0.35, 0.02);
    }

    [Test]
    public void Constants_MatchTheOriginalWindow()
    {
        CraftOutcomeRules.ChanceToLoseItemPercent.Should().Be(65);
        CraftOutcomeRules.FailureXpFraction.Should().Be(0.15f);
        CraftOutcomeRules.FirstCraftXpBonus.Should().Be(0.20f);
        CraftOutcomeRules.BlueprintLevelXpBonus.Should().Be(0.5f);
    }

    private static CraftOutcome Success(
        MinigameMeter quality,
        IReadOnlyList<int> filledSlots = null,
        bool isFood = false,
        int blueprintLevel = 0)
    {
        return CraftOutcomeRules.CalculateSuccess(
            quality, 20, 100, false, blueprintLevel, isFood, filledSlots ?? Array.Empty<int>());
    }
}
