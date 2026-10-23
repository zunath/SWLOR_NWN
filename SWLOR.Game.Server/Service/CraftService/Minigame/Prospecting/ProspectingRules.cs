using System.Collections.Generic;
using System.Linq;

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
/// for one unit; Quick Harvest still does. A session spends charges on its Strikes and Extracts instead (a Survey costs Stability and
/// seconds only), and the harvester's remaining charges are its budget.
/// </para>
/// </summary>
public static class ProspectingRules
{
    /// <summary>The node's pool is the die today's harvests came from: 1 to this many units, set when the node is first worked.</summary>
    public const int PoolDie = 4;

    /// <summary>The Stability every node starts with. A node that reaches 0 collapses.</summary>
    public const int MaxStability = 100;

    // Seams. A node holds one to three hidden seams, never more than its pool has units (each seals one); the weights are the chance
    // of each count. A seam is a gem pocket with GemPocketShare times the chance that a roll of the node's loot table is a gem (so none
    // where the table has no gem), and a rich seam otherwise. Gems are part of the bounded upside: pockets are rare enough that a skilled
    // session's expected gems per node, the gems its ordinary rolls give included, stay within 15% of today's at every tier.
    public const int OneSeamWeight = 30;
    public const int TwoSeamsWeight = 40;
    public const int ThreeSeamsWeight = 30;
    public const double GemPocketShare = 0.15;

    /// <summary>The seams every node can hold at most.</summary>
    public const int MaxSeams = 3;

    /// <summary>The chance, in hundredths of a percent, that a seam of a node whose table gives a gem on <paramref name="gemChance"/> of its rolls is a gem pocket.</summary>
    public static int GemPocketBasisPoints(double gemChance) =>
        (int)Math.Clamp(Math.Round(10000 * GemPocketShare * gemChance, MidpointRounding.AwayFromZero), 0, 10000);

    // Actions: harvester charges, Stability and seconds. Strike and Heavy Strike take the same time as the harvests they replace: five
    // seconds a unit is today's pace, and a Strike takes 1.5 units on average. A Survey costs no charge, so looking for seams spends
    // Stability and seconds only; an Extract costs the charge of the unit it takes, which a harvest of today costs too, and one more.
    public const int SurveyCharges = 0;
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

    /// <summary>The chance, in percent, that a Heavy Strike shatters one gem pocket that has not been surveyed.</summary>
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

    /// <summary>
    /// The chances of the units a Strike takes, at the tier of yield the Gathering rank earned (0 to 3). 1 or 2 units, each more with a
    /// style that adds units to a Strike, and a larger share of the bigger result with a perk or style that moves it.
    /// </summary>
    public static IReadOnlyList<(int Units, int Chance)> StrikeYield(int yieldTier, ProspectingEffects effects = null)
    {
        effects ??= ProspectingEffects.None;
        var shift = StrikeYieldShiftPercent * ClampTier(yieldTier);
        var big = Math.Clamp(30 + shift + effects.StrikeBigChancePoints, 0, 100);
        var yield = new List<(int Units, int Chance)>(2);
        if (big < 100)
            yield.Add((1 + effects.StrikeUnits, 100 - big));
        if (big > 0)
            yield.Add((2 + effects.StrikeUnits, big));

        return yield;
    }

    /// <summary>The chances of the units a Heavy Strike takes, at the tier of yield the Gathering rank earned (0 to 3). 2 to 4 units.</summary>
    public static IReadOnlyList<(int Units, int Chance)> HeavyStrikeYield(int yieldTier)
    {
        var shift = HeavyStrikeYieldShiftPercent * ClampTier(yieldTier);
        return new[] { (2, 25 - shift), (3, 50), (4, 25 + shift) };
    }

    /// <summary>
    /// Turns the weights of an action's outcomes into whole percentages that add up to 100: each share is rounded down, the points left
    /// over go to the outcomes with the largest remainders, and an outcome with any weight keeps at least 1 percent. Outcomes of equal
    /// weight are served in order, so the result is the same on every machine.
    /// </summary>
    public static IReadOnlyList<int> WholePercentages(IReadOnlyList<long> weights)
    {
        var total = 0L;
        foreach (var weight in weights)
            total += Math.Max(0, weight);

        if (total <= 0)
            throw new ArgumentException("An action's outcomes need some weight.", nameof(weights));

        var percentages = new int[weights.Count];
        var remainders = new long[weights.Count];
        var assigned = 0;
        for (var index = 0; index < weights.Count; index++)
        {
            var scaled = Math.Max(0, weights[index]) * 100;
            percentages[index] = (int)(scaled / total);
            remainders[index] = scaled % total;
            assigned += percentages[index];
        }

        var order = Enumerable.Range(0, weights.Count).OrderByDescending(index => remainders[index]).ThenBy(index => index).ToList();
        for (var left = 100 - assigned; left > 0; left--)
            percentages[order[(100 - assigned - left) % order.Count]]++;

        for (var index = 0; index < weights.Count; index++)
        {
            if (weights[index] <= 0 || percentages[index] > 0)
                continue;

            var largest = 0;
            for (var other = 1; other < percentages.Length; other++)
            {
                if (percentages[other] > percentages[largest])
                    largest = other;
            }

            percentages[largest]--;
            percentages[index] = 1;
        }

        return percentages;
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
