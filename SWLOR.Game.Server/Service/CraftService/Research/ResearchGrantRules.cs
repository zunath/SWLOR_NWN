using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Service.CraftService.Research;

/// <summary>
/// What each research level gives a blueprint, and how a completed job grants it. A job that ran an experiment grants exactly the
/// outcome stored on it; a job created before experiments existed has none and grants under the original random rolls, which this
/// class keeps unchanged: the weighted pick among licensed runs, credit reduction and time reduction, their dice, and the small
/// chance of an extra guaranteed bonus at every level.
/// </summary>
public static class ResearchGrantRules
{
    public const int MaxLevel = 10;

    /// <summary>Today's licensed-run die (d3), and the most an experiment can pay.</summary>
    public const int LicensedRunsMaximum = 3;

    /// <summary>Today's reduction die (d10, percent), and the most an experiment can pay.</summary>
    public const int ReductionMaximum = 10;

    private const int ExtraBonusSides = 1000;

    // The extra guaranteed bonus appears on a roll of 0 to 999 at or below these, by research level 1 to 10: 0.2% rising to 12.1%.
    private static readonly int[] ExtraBonusCeilings = { 1, 2, 3, 10, 15, 20, 50, 80, 100, 120 };

    // Today's weights for licensed runs, credit reduction and time reduction; the second set applies to a blueprint with an enhancement slot.
    private static readonly int[] StandardWeights = { 300, 400, 400 };
    private static readonly int[] EnhancedWeights = { 310, 420, 420 };

    public static bool IsRandomLevel(int level) => level is 3 or 5 or 8;

    public static ResearchLevelGrant GrantFor(int level) => level switch
    {
        1 or 6 or 9 => ResearchLevelGrant.ItemBonus,
        2 or 4 => ResearchLevelGrant.LicensedRun,
        3 or 5 or 8 => ResearchLevelGrant.RandomBlueprintBonus,
        7 => ResearchLevelGrant.GuaranteedTierThreeBonus,
        10 => ResearchLevelGrant.EnhancementSlot,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Research levels run 1 to 10.")
    };

    /// <summary>The words the research window uses for what a level gives.</summary>
    public static string Describe(int level)
    {
        if (level < 1 || level > MaxLevel)
            return "MAX";

        return GrantFor(level) switch
        {
            ResearchLevelGrant.ItemBonus => "Item Bonus +1",
            ResearchLevelGrant.LicensedRun => "Licensed Run +1",
            ResearchLevelGrant.RandomBlueprintBonus => "Blueprint Bonus",
            ResearchLevelGrant.GuaranteedTierThreeBonus => "Guaranteed Item Bonus",
            _ => "Enhancement Slot +1"
        };
    }

    /// <summary>The candidates an experiment at this level gathers evidence toward.</summary>
    public static IReadOnlyList<ResearchCandidate> CandidatesFor(int level) => IsRandomLevel(level)
        ? new[] { ResearchCandidate.LicensedRuns, ResearchCandidate.CreditReduction, ResearchCandidate.TimeReduction }
        : new[] { ResearchCandidate.FixedGrant };

    public static string CandidateName(ResearchCandidate candidate, int level) => candidate switch
    {
        ResearchCandidate.LicensedRuns => "Licensed Runs",
        ResearchCandidate.CreditReduction => "Credit Reduction",
        ResearchCandidate.TimeReduction => "Time Reduction",
        ResearchCandidate.FixedGrant => Describe(level),
        _ => "Nothing"
    };

    /// <summary>The most a candidate can pay: today's dice maximum.</summary>
    public static int MaximumAmount(ResearchCandidate candidate) => candidate switch
    {
        ResearchCandidate.LicensedRuns => LicensedRunsMaximum,
        ResearchCandidate.CreditReduction or ResearchCandidate.TimeReduction => ReductionMaximum,
        _ => 0
    };

    /// <summary>The average of today's die for a candidate: 2 licensed runs, or 5.5 percent.</summary>
    public static double AverageDice(ResearchCandidate candidate) => (1 + MaximumAmount(candidate)) / 2.0;

    /// <summary>The share of today's random-level rolls that land on a candidate, as a fraction.</summary>
    public static double TodayWeightShare(ResearchCandidate candidate, bool blueprintHasEnhancementSlot)
    {
        var weights = blueprintHasEnhancementSlot ? EnhancedWeights : StandardWeights;
        var index = candidate switch
        {
            ResearchCandidate.LicensedRuns => 0,
            ResearchCandidate.CreditReduction => 1,
            ResearchCandidate.TimeReduction => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(candidate))
        };
        return weights[index] / (double)weights.Sum();
    }

    /// <summary>The chance of the extra guaranteed bonus at a level, in percent.</summary>
    public static double ExtraBonusChancePercent(int level) => (ExtraBonusCeilings[CeilingIndex(level)] + 1) * 100.0 / ExtraBonusSides;

    /// <summary>The tier of the extra guaranteed bonus: 1, except 2 at level 9 and 3 at level 10.</summary>
    public static int ExtraBonusTier(int level) => level switch
    {
        9 => 2,
        10 => 3,
        _ => 1
    };

    /// <summary>Rolls whether the extra guaranteed bonus appears, at today's chance for the level.</summary>
    public static bool RollExtraBonus(int level, IMinigameRandom random) =>
        random.Next(0, ExtraBonusSides) <= ExtraBonusCeilings[CeilingIndex(level)];

    /// <summary>
    /// Today's random blueprint bonus: a weighted pick among licensed runs +d3, credit reduction +d10 percent and time reduction
    /// +d10 percent, applied to the blueprint. The weights shift slightly for a blueprint that has an enhancement slot.
    /// </summary>
    public static (ResearchCandidate Candidate, int Amount) RollRandomBonus(BlueprintDetail blueprint, IMinigameRandom random)
    {
        var hasEnhancementBonus = blueprint.EnhancementSlots > 0 && blueprint.Level < MaxLevel;
        var weights = hasEnhancementBonus ? EnhancedWeights : StandardWeights;

        var roll = random.Next(0, weights.Sum());
        var index = 0;
        var cumulative = weights[0];
        while (roll >= cumulative && index < weights.Length - 1)
            cumulative += weights[++index];

        switch (index)
        {
            case 0:
                var runs = random.Next(1, LicensedRunsMaximum + 1);
                blueprint.LicensedRuns += runs;
                return (ResearchCandidate.LicensedRuns, runs);
            case 1:
                var credit = random.Next(1, ReductionMaximum + 1);
                blueprint.CreditReduction += credit;
                return (ResearchCandidate.CreditReduction, credit);
            default:
                var time = random.Next(1, ReductionMaximum + 1);
                blueprint.TimeReduction += time;
                return (ResearchCandidate.TimeReduction, time);
        }
    }

    /// <summary>
    /// Raises the blueprint one research level and grants what the new level gives. A settled experiment decides a random level's
    /// bonus and whether the extra guaranteed bonus appears; with none (a job created before experiments), today's rolls decide both.
    /// A random level whose experiment gathered no evidence falls back to today's weighted roll and dice.
    /// </summary>
    /// <param name="blueprint">The blueprint, changed in place.</param>
    /// <param name="experiment">The job's stored experiment, or null for a job created before experiments.</param>
    /// <param name="random">The source of chance for the rolls that remain.</param>
    /// <param name="addGuaranteedBonus">Adds a guaranteed bonus of the given tier to the blueprint's item.</param>
    public static ResearchCompletion Complete(
        BlueprintDetail blueprint,
        ResearchExperimentRecord experiment,
        IMinigameRandom random,
        Action<int> addGuaranteedBonus)
    {
        if (blueprint == null)
            throw new ArgumentNullException(nameof(blueprint));
        if (random == null)
            throw new ArgumentNullException(nameof(random));
        if (addGuaranteedBonus == null)
            throw new ArgumentNullException(nameof(addGuaranteedBonus));

        var stored = experiment is { Status: ResearchExperimentStatus.Settled };

        blueprint.Level++;
        if (blueprint.Level > MaxLevel)
            blueprint.Level = MaxLevel;

        var level = blueprint.Level;
        var grant = GrantFor(level);
        var candidate = ResearchCandidate.FixedGrant;
        var amount = 0;
        var fromExperiment = stored;

        switch (grant)
        {
            case ResearchLevelGrant.ItemBonus:
                blueprint.ItemBonuses++;
                break;
            case ResearchLevelGrant.LicensedRun:
                blueprint.LicensedRuns++;
                break;
            case ResearchLevelGrant.RandomBlueprintBonus:
                if (stored && experiment.Winner != ResearchCandidate.None)
                {
                    candidate = experiment.Winner;
                    amount = experiment.Magnitude;
                    ApplyBonus(blueprint, candidate, amount);
                }
                else
                {
                    (candidate, amount) = RollRandomBonus(blueprint, random);
                    fromExperiment = false;
                }
                break;
            case ResearchLevelGrant.GuaranteedTierThreeBonus:
                addGuaranteedBonus(3);
                break;
            default:
                blueprint.EnhancementSlots++;
                break;
        }

        var extra = stored ? experiment.ExtraBonus : RollExtraBonus(level, random);
        var extraTier = 0;
        if (extra)
        {
            extraTier = ExtraBonusTier(level);
            addGuaranteedBonus(extraTier);
        }

        return new ResearchCompletion(level, grant, candidate, amount, extraTier, fromExperiment);
    }

    private static void ApplyBonus(BlueprintDetail blueprint, ResearchCandidate candidate, int amount)
    {
        switch (candidate)
        {
            case ResearchCandidate.LicensedRuns:
                blueprint.LicensedRuns += amount;
                break;
            case ResearchCandidate.CreditReduction:
                blueprint.CreditReduction += amount;
                break;
            case ResearchCandidate.TimeReduction:
                blueprint.TimeReduction += amount;
                break;
        }
    }

    private static int CeilingIndex(int level)
    {
        if (level < 1 || level > MaxLevel)
            throw new ArgumentOutOfRangeException(nameof(level), level, "Research levels run 1 to 10.");

        return level - 1;
    }
}
