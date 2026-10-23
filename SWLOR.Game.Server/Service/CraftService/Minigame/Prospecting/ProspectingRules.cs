using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

/// <summary>
/// The numbers of Prospecting, the node session a harvester opens. Every value here is a starting value for the simulator to tune; the
/// engine, the setup and the node read them and nothing else, so a tuning pass changes this file only (CraftSim's harvesting study,
/// <c>design/testing/crafting-revamp/harvesting-prospecting-parity.md</c>, is the check).
/// <para>
/// A node holds a pool of units (today's d4 harvests), a Stability of 100 and one to three hidden seams. Each seam is a unit of the
/// pool that is sealed in rock: a Strike takes the loose units first and breaks into a seam only when nothing else is left, so a
/// player who never looks for seams still gets the whole pool. Survey finds a seam and Extract opens it: a rich seam gives its unit and,
/// sometimes, an extra ore roll, and a gem pocket gives a gem in place of its unit. A harvest of today took one charge and 5 seconds
/// for one unit; Quick Harvest still does. A session spends charges for each action instead, and the harvester's remaining charges
/// are its budget.
/// </para>
/// </summary>
public static class ProspectingRules
{
    /// <summary>The node's pool is the die today's harvests came from: 1 to this many units, set when the node is first worked.</summary>
    public const int PoolDie = 4;

    /// <summary>The Stability every node starts with. A node that reaches 0 collapses.</summary>
    public const int MaxStability = 100;

    // Seams. A node holds one to three hidden seams, never more than its pool has units (each seals one); the weights are the chance
    // of each count. A seam is a gem pocket with GemPocketPercent chance when the node's loot table has gem entries, and a rich seam
    // otherwise.
    public const int OneSeamWeight = 30;
    public const int TwoSeamsWeight = 40;
    public const int ThreeSeamsWeight = 30;
    public const int GemPocketPercent = 25;

    /// <summary>The seams every node can hold at most.</summary>
    public const int MaxSeams = 3;

    // Actions: harvester charges, Stability and seconds. Strike and Heavy Strike take the same time as the harvests they replace: five
    // seconds a unit is today's pace, and a Strike takes 1.5 units on average.
    public const int SurveyCharges = 1;
    public const int SurveyStability = 12;
    public const double SurveySeconds = 2.5;

    public const int StrikeCharges = 1;
    public const int StrikeStability = 20;
    public const double StrikeSeconds = 5.0;

    public const int HeavyStrikeCharges = 2;
    public const int HeavyStrikeStability = 36;
    public const double HeavyStrikeSeconds = 8.0;

    public const int ExtractCharges = 2;
    public const int ExtractStability = 24;
    public const double ExtractSeconds = 3.0;

    /// <summary>The chance, in percent, that a Heavy Strike shatters one gem pocket that has not been surveyed. Keep it a multiple of 20, so every outcome of a Heavy Strike stays a whole percentage (the engine refuses otherwise).</summary>
    public const int ShatterPercent = 20;

    /// <summary>The chance, in percent, that a claimed rich seam gives an extra ore roll beyond its own unit. This is the one place a session yields more than the pool.</summary>
    public const int RichSeamBonusPercent = 25;

    /// <summary>The ore rolls a rich seam's bonus gives.</summary>
    public const int RichSeamBonusRolls = 1;

    /// <summary>The gems a claimed gem pocket gives, in place of its unit.</summary>
    public const int GemPocketGems = 1;

    /// <summary>
    /// The seconds one harvest took before Prospecting (<c>HarvesterItemDefinition</c>'s delay) and still takes in Quick Harvest, once for
    /// every unit: Quick Harvest never trains Gathering faster than today.
    /// </summary>
    public const double HarvestSeconds = 5.0;

    /// <summary>The seconds an action takes to resolve, and so the least time between two actions. Leave is immediate.</summary>
    public static double SecondsFor(string actionId) => actionId switch
    {
        ProspectingActionIds.Survey => SurveySeconds,
        ProspectingActionIds.Strike => StrikeSeconds,
        ProspectingActionIds.HeavyStrike => HeavyStrikeSeconds,
        ProspectingActionIds.Extract => ExtractSeconds,
        _ => 0.0
    };

    // Gathering rank milestones (CraftRankMilestones). A tier of yield moves percentage points of a Strike's and a Heavy Strike's chance
    // from the lowest outcome to the highest, and a Survey reveals one more seam at each of the survey milestones. They replace the
    // Might and Perception bonuses Gathering used to take from attributes. Yield per action never raises the pool: the pool still
    // ends the node.
    public const int StrikeYieldShiftPercent = 5;
    public const int HeavyStrikeYieldShiftPercent = 5;

    /// <summary>The chances of the units a Strike takes, at the tier of yield the Gathering rank earned (0 to 3). 1 or 2 units.</summary>
    public static IReadOnlyList<(int Units, int Chance)> StrikeYield(int yieldTier)
    {
        var shift = StrikeYieldShiftPercent * ClampTier(yieldTier);
        return new[] { (1, 70 - shift), (2, 30 + shift) };
    }

    /// <summary>The chances of the units a Heavy Strike takes, at the tier of yield the Gathering rank earned (0 to 3). 2 to 4 units.</summary>
    public static IReadOnlyList<(int Units, int Chance)> HeavyStrikeYield(int yieldTier)
    {
        var shift = HeavyStrikeYieldShiftPercent * ClampTier(yieldTier);
        return new[] { (2, 25 - shift), (3, 50), (4, 25 + shift) };
    }

    /// <summary>The expected units of a Strike or a Heavy Strike, before the pool caps it.</summary>
    public static double ExpectedUnits(IReadOnlyList<(int Units, int Chance)> yield)
    {
        var total = 0.0;
        foreach (var (units, chance) in yield)
            total += units * chance / 100.0;

        return total;
    }

    // The multiplier on the XP of a session (see CraftMinigameXp), by the vein's level: 1 pays today's XP for each unit gathered.
    // Quick Harvest never takes it. A tuning pass raises a band here when the XP-rate gate (gate 6) needs it.
    public const double XpMultiplierVeinLevel5 = 1.0;
    public const double XpMultiplierVeinLevel15 = 1.0;
    public const double XpMultiplierVeinLevel25 = 1.0;
    public const double XpMultiplierVeinLevel35 = 1.0;
    public const double XpMultiplierVeinLevel45 = 1.0;

    /// <summary>The multiplier on the XP of a session at a vein of the level. A level of 0 or less takes the lowest band's.</summary>
    public static double XpMultiplierFor(int veinLevel) => veinLevel switch
    {
        <= 5 => XpMultiplierVeinLevel5,
        <= 15 => XpMultiplierVeinLevel15,
        <= 25 => XpMultiplierVeinLevel25,
        <= 35 => XpMultiplierVeinLevel35,
        _ => XpMultiplierVeinLevel45
    };

    private static int ClampTier(int tier) => Math.Clamp(tier, 0, CraftRankMilestones.MaxProspectingYieldTier);
}
