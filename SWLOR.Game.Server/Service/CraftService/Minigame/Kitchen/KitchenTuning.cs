namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>
/// Every number the Kitchen rules use. The plan fixes the actions and meters, not these magnitudes: phase 2 tuned the ones marked
/// below against the CraftSim balance gates (<c>design/testing/crafting-revamp/kitchen-gates.md</c>) and the rest stay at their
/// phase 1 starting values until the client evidence says otherwise.
/// </summary>
public static class KitchenTuning
{
    /// <summary>Each flavor axis runs from 0 to this value.</summary>
    public const int AxisMax = 100;

    /// <summary>
    /// Doneness (the progress meter) needed to finish the dish. Tuned: needing only 100, crafts at the lower levels with gear took fewer actions
    /// than the 10 action floor of the length gate.
    /// </summary>
    public const int DonenessMax = 120;

    /// <summary>The quality meter is a percentage.</summary>
    public const int QualityMax = 100;

    /// <summary>A farthest-apart pair of points in the flavor cube is this far apart (100 x the square root of 3, rounded up).</summary>
    public const int MaxDistance = 174;

    // Adding ingredients

    /// <summary>One ingredient line is added in at most this many portions, so a stack of ten is not ten clicks.</summary>
    public const int MaxPortionsPerLine = 4;

    /// <summary>A free pantry seasoning pushes its axis by this much.</summary>
    public const int PantryPush = 4;

    /// <summary>Each unit of a diluting ingredient (water) shrinks every axis by this percentage.</summary>
    public const int DilutePercentPerUnit = 25;

    /// <summary>
    /// With no Control, a push lands anywhere from this percentage below to this percentage above its listed strength. Tuned: at 40 a
    /// single rotation of actions could match adaptive play across a band of low-level dishes (gate 1).
    /// </summary>
    public const int BasePushSpreadPercent = 50;

    /// <summary>Control narrows the spread: spread = base x 100 / (100 + this x Control).</summary>
    public const int ControlPerSpreadStep = 3;

    /// <summary>The chance of the light and of the heavy push; the steady push takes the rest.</summary>
    public const int ExtremePushChancePercent = 25;

    /// <summary>The share of an Add Peak Aroma doubles.</summary>
    public const int PeakAromaMultiplier = 2;

    // Cooking

    public const int SimmerLowDoneness = 5;
    public const int SimmerMediumDoneness = 12;
    public const int SearDoneness = 36;

    /// <summary>
    /// Doneness gained by each action grows by this share per point of Craftsmanship. Tuned: at 0.015 gear shortened crafts below the
    /// length gate's floor of 10 actions.
    /// </summary>
    public const double CraftsmanshipDonenessPerPoint = 0.008;

    /// <summary>
    /// An enhancement's progress penalty (written for the old crafting window's much larger progress scale) adds this share of
    /// itself to the Doneness the dish needs. Tuned: at 0.5 dishes with two or three enhancements failed or fell
    /// short of quality often enough that enhancement transfers dropped below today's (gate 7).
    /// </summary>
    public const double EnhancementDonenessPerPenalty = 0.25;

    /// <summary>The Doneness a set of enhancements adds to what the dish needs, from their total progress penalty.</summary>
    public static int EnhancementDoneness(int progressPenalty) =>
        (int)Math.Round(Math.Max(0, progressPenalty) * EnhancementDonenessPerPenalty, MidpointRounding.AwayFromZero);

    /// <summary>The Doneness multiplier moves by this much per skill rank above (or below) the recipe's level.</summary>
    public const double LevelFactorPerRank = 0.05;

    public const double LevelFactorMin = 0.75;
    public const double LevelFactorMax = 1.5;

    // Freshness

    /// <summary>Freshness lost at the end of every turn.</summary>
    public const int FreshnessDecayPerTurn = 4;

    /// <summary>Searing costs this much Freshness on top of the turn's decay.</summary>
    public const int SearFreshnessCost = 6;

    /// <summary>Cover halves the Freshness loss for this many turns, counting the turn it is used.</summary>
    public const int CoverTurns = 3;

    // Heat and drift

    /// <summary>How far the next drift moves the flavor on one axis at High heat.</summary>
    public const int DriftStrength = 8;

    public const int DriftPercentAtLow = 0;
    public const int DriftPercentAtMedium = 50;
    public const int DriftPercentAtHigh = 100;

    // Craft points

    /// <summary>The CP a Stir costs. Tuned: at 8 a cook with gear could stir every turn without reading the drift (gate 1).</summary>
    public const int StirCp = 12;
    public const int CoverCp = 6;

    /// <summary>The CP Reduce costs, an action only a cook with the Reduce perk has.</summary>
    public const int ReduceCp = 12;

    /// <summary>Gear CP plus this much per skill rank, as every crafting skill's CP.</summary>
    public const double CpPerRank = 0.75;

    /// <summary>The rank where the CP passive begins; kept from the old crafting window until the simulator re-tunes it.</summary>
    public const int CpPassiveRank = 25;

    public const int CpPassiveBonus = 31;

    // XP

    // The multiplier on the base XP of every Kitchen craft in a recipe-level band (see CraftMinigameXp), each the least that keeps the
    // CraftSim XP-rate gate (gate 6) passing in its band at every gear tier and enhancement count. The Kitchen's 10-20 action loop at
    // an estimated 8 seconds an action pays less XP per hour than the old window's 4-28 action loop at 6 seconds, and the old window is
    // fastest at the lowest levels. Starting values until client timing replaces the seconds per action.
    public const double XpMultiplierLevels1To10 = 2.15;
    public const double XpMultiplierLevels11To20 = 1.15;
    public const double XpMultiplierLevels21To30 = 1.1;
    public const double XpMultiplierLevels31To40 = 1.2;
    public const double XpMultiplierLevels41Up = 1.05;

    /// <summary>The multiplier on the base XP of a Kitchen craft at a recipe level. A level of 0 or less takes the lowest band's.</summary>
    public static double XpMultiplierForLevel(int recipeLevel) => recipeLevel switch
    {
        <= 10 => XpMultiplierLevels1To10,
        <= 20 => XpMultiplierLevels11To20,
        <= 30 => XpMultiplierLevels21To30,
        <= 40 => XpMultiplierLevels31To40,
        _ => XpMultiplierLevels41Up
    };

    // Telegraphed opportunity

    public const int PeakAromaChancePercent = 25;
    public const string PeakAromaId = "peak-aroma";
    public const string PeakAromaName = "Peak Aroma";

    public static int DriftPercent(KitchenHeat heat) => heat switch
    {
        KitchenHeat.Low => DriftPercentAtLow,
        KitchenHeat.Medium => DriftPercentAtMedium,
        _ => DriftPercentAtHigh
    };
}
