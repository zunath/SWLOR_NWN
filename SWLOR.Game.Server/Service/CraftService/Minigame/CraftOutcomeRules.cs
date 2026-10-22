using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// The rules every item craft ends under, whichever minigame produced the quality. XP, vendor value and the loss chance copy
/// the formulas the original crafting window used (<c>CraftViewModel.ProcessSuccess</c>, <c>CalculateXP</c> and
/// <c>ProcessFailure</c>). Enhancements transfer by quality threshold instead of by chance, and food duration scales with quality
/// instead of paying only at full quality.
/// </summary>
public static class CraftOutcomeRules
{
    /// <summary>Each component stack and each enhancement is lost on a failed craft on a D100 of this value or lower.</summary>
    public const int ChanceToLoseItemPercent = 65;

    /// <summary>A failed craft gives this share of the XP a plain success would.</summary>
    public const float FailureXpFraction = 0.15f;

    /// <summary>The XP bonus on a recipe's first successful craft.</summary>
    public const float FirstCraftXpBonus = 0.20f;

    /// <summary>The XP bonus for each level of the blueprint used.</summary>
    public const float BlueprintLevelXpBonus = 0.5f;

    /// <summary>The quality an enhancement in the first slot needs to be guaranteed.</summary>
    public const int FirstSlotQualityThresholdPercent = 50;

    /// <summary>The quality an enhancement in the second slot, and any later slot, needs to be guaranteed.</summary>
    public const int SecondSlotQualityThresholdPercent = 85;

    /// <summary>
    /// The item local that holds a crafted food's extra duration in seconds. The food item reads it when eaten, because an item
    /// property can only store whole 5-minute steps.
    /// </summary>
    public const string FoodDurationBonusSecondsVariable = "FOOD_DURATION_BONUS_SECONDS";

    /// <summary>One step of a food duration bonus in seconds (<c>ConsumableItemDefinition</c>: 5 minutes per bonus).</summary>
    public const int FoodDurationSecondsPerUnit = 5 * 60;

    /// <summary>The duration bonus, in steps, of a food crafted at 100% quality: the old code's <c>(int)qualityPercent</c>.</summary>
    public const int FullQualityFoodDurationUnits = 1;

    /// <summary>
    /// The quality percentage each enhancement slot needs, in slot order. An item with one slot uses the first threshold,
    /// and slots past the second use the second.
    /// </summary>
    public static IReadOnlyList<int> GetEnhancementThresholds(int slotCount)
    {
        var thresholds = new List<int>();
        for (var slot = 1; slot <= slotCount; slot++)
            thresholds.Add(GetEnhancementThreshold(slot));

        return thresholds;
    }

    /// <summary>The quality percentage the enhancement in a slot (counting from 1) needs for a guaranteed transfer.</summary>
    public static int GetEnhancementThreshold(int slot) =>
        slot <= 1 ? FirstSlotQualityThresholdPercent : SecondSlotQualityThresholdPercent;

    /// <summary>The recipe's base XP after its minigame's XP multiplier, which scales a failed craft's XP with it.</summary>
    public static int ScaleBaseXp(int baseXp, double xpMultiplier) =>
        (int)Math.Round(baseXp * xpMultiplier, MidpointRounding.AwayFromZero);

    /// <summary>XP for a successful craft: first-craft bonus, blueprint bonus, then a bonus equal to the quality share.</summary>
    public static int CalculateXp(int baseXp, int blueprintLevel, bool firstTime, float qualityFraction)
    {
        var xp = baseXp;
        if (firstTime)
            xp += (int)(xp * FirstCraftXpBonus);
        xp += (int)(xp * (blueprintLevel * BlueprintLevelXpBonus));
        xp += (int)(xp * qualityFraction);

        return xp;
    }

    /// <summary>XP for a failed craft: 15% of what a success would give at zero quality and without the first-craft bonus.</summary>
    public static int CalculateFailureXp(int baseXp, int blueprintLevel)
    {
        var xp = CalculateXp(baseXp, blueprintLevel, false, 0f);
        return (int)(xp * FailureXpFraction);
    }

    /// <summary>The credits added to a crafted item's vendor value.</summary>
    public static int CalculateVendorValueBonus(int recipeLevel, float qualityFraction)
    {
        const float LevelBucketMultiplier = 48f;
        const float LevelScalingPerRecipeLevel = 9f;
        const float CraftedVendorBonusMultiplier = 1.225f;

        var levelBonus = LevelBucketMultiplier * ((recipeLevel / 10f) + 1) + LevelScalingPerRecipeLevel * recipeLevel;
        var scaledByQuality = (int)Math.Round(levelBonus * qualityFraction);
        var minimumVendorBonus = Math.Max(25, (int)Math.Round(recipeLevel * 1.6f));

        return (int)Math.Round(Math.Max(scaledByQuality, minimumVendorBonus) * CraftedVendorBonusMultiplier);
    }

    /// <summary>The food duration bonus in seconds. It rises evenly from nothing at 0% to the old full-quality bonus at 100%.</summary>
    public static int CalculateFoodDurationBonusSeconds(float qualityFraction) =>
        (int)Math.Round(Math.Clamp(qualityFraction, 0f, 1f) * FullQualityFoodDurationUnits * FoodDurationSecondsPerUnit);

    /// <summary>
    /// Everything a successful craft decides from its quality.
    /// </summary>
    /// <param name="quality">The quality meter when the progress meter completed.</param>
    /// <param name="recipeLevel">The recipe's level.</param>
    /// <param name="baseXp">XP before bonuses, from <c>Craft.GetBaseRecipeXP</c>.</param>
    /// <param name="firstTime">True when the recipe was never crafted before.</param>
    /// <param name="blueprintLevel">The level of the blueprint used, or 0 when none was.</param>
    /// <param name="isFood">True for food recipes.</param>
    /// <param name="filledEnhancementSlots">The slots (counting from 1) that hold an enhancement.</param>
    public static CraftOutcome CalculateSuccess(
        MinigameMeter quality,
        int recipeLevel,
        int baseXp,
        bool firstTime,
        int blueprintLevel,
        bool isFood,
        IReadOnlyList<int> filledEnhancementSlots)
    {
        var fraction = quality.Fraction;
        var transfers = filledEnhancementSlots
            .Select(slot =>
            {
                var threshold = GetEnhancementThreshold(slot);
                return new EnhancementTransfer(slot, threshold, quality.Percent >= threshold);
            })
            .ToList();

        return new CraftOutcome(
            fraction,
            quality.Percent,
            CalculateXp(baseXp, blueprintLevel, firstTime, fraction),
            CalculateVendorValueBonus(recipeLevel, fraction),
            transfers,
            isFood ? CalculateFoodDurationBonusSeconds(fraction) : 0,
            isFood && quality.IsFull ? blueprintLevel : 0);
    }

    /// <summary>
    /// Decides what a failed craft costs: each enhancement slot that holds one, then each component stack, is kept on a D100
    /// roll above <see cref="ChanceToLoseItemPercent"/>. Rolls run in that order.
    /// </summary>
    public static CraftFailure RollFailure(
        IMinigameRandom random,
        int filledEnhancementSlots,
        int componentStacks,
        int baseXp,
        int blueprintLevel)
    {
        var enhancementsKept = RollKept(random, filledEnhancementSlots);
        var componentsKept = RollKept(random, componentStacks);

        return new CraftFailure(enhancementsKept, componentsKept, CalculateFailureXp(baseXp, blueprintLevel));
    }

    private static IReadOnlyList<bool> RollKept(IMinigameRandom random, int count)
    {
        var kept = new List<bool>(count);
        for (var index = 0; index < count; index++)
            kept.Add(random.D100() > ChanceToLoseItemPercent);

        return kept;
    }
}
