using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService;

/// <summary>
/// The skill ranks at which gathering, research and droid assembly capabilities unlock for every character.
/// Perks used to sell these milestones; each one is now available at the rank the matching perk rank required.
/// </summary>
public static class CraftRankMilestones
{
    public const int RanksPerTier = 10;
    public const int MaxTier = 5;
    public const int MaxResearchTier = 5;
    public const int MaxRefineryManagementLevel = 6;

    // Gathering ranks that each add one item per power core, after the first at rank 0.
    private static readonly int[] RefineryManagementRanks = { 10, 20, 30, 40, 45 };

    // Fabrication ranks that each add a concurrent research job and a licensed run on new blueprints.
    private static readonly int[] ResearchMilestoneRanks = { 25, 50 };

    // The Search board's schedule (tuned by CraftSim, see scavenging-search-parity.md): the Gathering rank that first gives each value.
    // The searches and the promising spots are the same at every rank, because a step in either is worth more than a rank's step in
    // Quick Search's expected rolls (which rise 0.065 a tier), so a count that steps cannot keep the board at the same share of Quick
    // Search at every rank. Gathering rank improves the board through the chance a Search finds on promising ground, which replaced
    // Perception, and which steps at the same ranks Hard Look did.
    private static readonly (int Rank, int Value)[] SearchesSteps = { (0, 6) };
    private static readonly (int Rank, int Value)[] PromisingSpotSteps = { (0, 2) };
    private static readonly (int Rank, int Value)[] PromisingChanceSteps = { (0, 29), (10, 33), (20, 36), (30, 39), (40, 42) };

    // Gathering ranks at which a Strike and a Heavy Strike in a Prospecting session yield more (replacing Might), and at which one
    // Survey reveals one more seam (replacing Perception).
    private static readonly int[] ProspectingYieldRanks = { 15, 30, 45 };
    private static readonly int[] ProspectingSurveyRanks = { 25, 50 };

    /// <summary>The most tiers of Prospecting yield a Gathering rank can earn.</summary>
    public const int MaxProspectingYieldTier = 3;

    /// <summary>The most seams one Survey can reveal.</summary>
    public const int MaxProspectingSurveyReveals = 3;

    /// <summary>
    /// Tier 1 to 5 for a Gathering rank (harvesters, ores, scavenge sites, Hard Look level) or an Engineering rank
    /// (droids): tier 1 at rank 0, then one more every 10 ranks.
    /// </summary>
    public static int TierForRank(int rank) => Math.Min(MaxTier, Math.Max(0, rank) / RanksPerTier + 1);

    /// <summary>The lowest skill rank that reaches the tier.</summary>
    public static int RequiredRankForTier(int tier) => Math.Max(0, tier - 1) * RanksPerTier;

    /// <summary>The items a power core refines beyond its base of three: 1 at Gathering 0, up to 6 at Gathering 45.</summary>
    public static int RefineryManagementLevelForRank(int gatheringRank) =>
        1 + CountReached(RefineryManagementRanks, gatheringRank);

    /// <summary>The highest recipe tier a Fabrication rank can research: none below rank 10, then one more every 10 ranks.</summary>
    public static int ResearchTierForRank(int fabricationRank) =>
        Math.Min(MaxResearchTier, Math.Max(0, fabricationRank) / RanksPerTier);

    /// <summary>The lowest Fabrication rank that can research recipes of the tier.</summary>
    public static int RequiredFabricationRankForResearchTier(int tier) => Math.Max(1, tier) * RanksPerTier;

    /// <summary>Research jobs a character may run at once: one, plus one at Fabrication 25 and another at 50.</summary>
    public static int MaxConcurrentResearchJobsForRank(int fabricationRank) =>
        1 + CountReached(ResearchMilestoneRanks, fabricationRank);

    /// <summary>Licensed runs added to new blueprints: one at Fabrication 25 and another at 50.</summary>
    public static int BlueprintLicensedRunBonusForRank(int fabricationRank) =>
        CountReached(ResearchMilestoneRanks, fabricationRank);

    /// <summary>The tier of Prospecting yield a Gathering rank earns: 0 below rank 15, then one more at ranks 15, 30 and 45.</summary>
    public static int ProspectingYieldTierForRank(int gatheringRank) => CountReached(ProspectingYieldRanks, gatheringRank);

    /// <summary>The seams one Survey reveals at a Gathering rank: 1, then one more at ranks 25 and 50.</summary>
    public static int ProspectingSurveyRevealsForRank(int gatheringRank) => 1 + CountReached(ProspectingSurveyRanks, gatheringRank);

    /// <summary>The lowest Gathering rank of each tier of Prospecting yield, for text.</summary>
    public static IReadOnlyList<int> ProspectingYieldMilestoneRanks => ProspectingYieldRanks;

    /// <summary>The Gathering ranks at which a Survey reveals one more seam, for text.</summary>
    public static IReadOnlyList<int> ProspectingSurveyMilestoneRanks => ProspectingSurveyRanks;

    public static int GetHarvesterTier(uint creature) => TierForRank(GetGatheringRank(creature));

    public static int GetRefiningTier(uint creature) => TierForRank(GetGatheringRank(creature));

    public static int GetRefineryManagementLevel(uint creature) => RefineryManagementLevelForRank(GetGatheringRank(creature));

    /// <summary>
    /// The searches a Gathering rank gets on a scavenge site's board. They replaced Hard Look, which only gave a chance of a second
    /// attempt; Quick Search still reads <see cref="HardLookLevelForRank"/> for that chance.
    /// </summary>
    public static int SearchesForRank(int gatheringRank) => ValueAt(SearchesSteps, gatheringRank);

    /// <summary>The spots of a scavenge site's board that are promising at a Gathering rank. They replaced Perception.</summary>
    public static int PromisingSpotsForRank(int gatheringRank) => ValueAt(PromisingSpotSteps, gatheringRank);

    /// <summary>
    /// The chance, in percent, that a Search of promising ground finds something at a Gathering rank. It rises with rank, which is how
    /// Gathering rank improves the board in place of Perception.
    /// </summary>
    public static int PromisingChanceForRank(int gatheringRank) => ValueAt(PromisingChanceSteps, gatheringRank);

    /// <summary>The Hard Look level a Gathering rank is worth, which sets Quick Search's chance of a second attempt: the rank's tier.</summary>
    public static int HardLookLevelForRank(int gatheringRank) => TierForRank(gatheringRank);

    public static int GetScavengingTier(uint creature) => TierForRank(GetGatheringRank(creature));

    /// <summary>The Hard Look level the Gathering rank grants, which sets Quick Search's chance of a second search.</summary>
    public static int GetHardLookLevel(uint creature) => HardLookLevelForRank(GetGatheringRank(creature));

    public static int GetSearches(uint creature) => SearchesForRank(GetGatheringRank(creature));

    public static int GetPromisingSpots(uint creature) => PromisingSpotsForRank(GetGatheringRank(creature));

    public static int GetPromisingChance(uint creature) => PromisingChanceForRank(GetGatheringRank(creature));

    /// <summary>The Gathering ranks at which anything on the Search board's schedule changes, with the board's numbers from there, for reports.</summary>
    public static IReadOnlyList<(int Rank, int Searches, int PromisingSpots, int PromisingChance)> SearchSchedule() =>
        SearchesSteps.Concat(PromisingSpotSteps).Concat(PromisingChanceSteps)
            .Select(step => step.Rank)
            .Distinct()
            .OrderBy(rank => rank)
            .Select(rank => (rank, SearchesForRank(rank), PromisingSpotsForRank(rank), PromisingChanceForRank(rank)))
            .ToList();

    public static int GetDroidAssemblyTier(uint creature) =>
        TierForRank(Skill.GetCreatureSkillRank(creature, SkillType.Engineering));

    public static int GetResearchTier(uint creature) =>
        ResearchTierForRank(Skill.GetCreatureSkillRank(creature, SkillType.Fabrication));

    public static int GetMaxConcurrentResearchJobs(uint creature) =>
        MaxConcurrentResearchJobsForRank(Skill.GetCreatureSkillRank(creature, SkillType.Fabrication));

    public static int GetBlueprintLicensedRunBonus(uint creature) =>
        BlueprintLicensedRunBonusForRank(Skill.GetCreatureSkillRank(creature, SkillType.Fabrication));

    private static int GetGatheringRank(uint creature) => Skill.GetCreatureSkillRank(creature, SkillType.Gathering);

    private static int ValueAt((int Rank, int Value)[] steps, int rank)
    {
        var value = steps[0].Value;
        foreach (var step in steps)
        {
            if (rank >= step.Rank)
                value = step.Value;
        }

        return value;
    }

    private static int CountReached(int[] ranks, int rank)
    {
        var reached = 0;
        foreach (var milestone in ranks)
        {
            if (rank >= milestone)
                reached++;
        }

        return reached;
    }
}
