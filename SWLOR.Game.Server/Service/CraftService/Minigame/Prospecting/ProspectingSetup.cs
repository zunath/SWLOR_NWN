using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

/// <summary>
/// Everything the Prospecting engine needs to know about one session besides chance: the node as the player finds it, the harvester's
/// charges, and what the player's Gathering rank earns. It is immutable. <see cref="ForNode"/> derives it from the node's content, the
/// rank and the charges; the constructor takes the finished numbers so a test can set them directly.
/// </summary>
public sealed class ProspectingSetup
{
    /// <param name="node">The node as the session begins: its pool, Stability and seams.</param>
    /// <param name="charges">The harvester's charges, which are the budget of the session. At least 1.</param>
    /// <param name="surveyReveals">The seams one Survey reveals. At least 1.</param>
    /// <param name="yieldTier">The tier of yield the Gathering rank earned, 0 to <see cref="CraftRankMilestones.MaxProspectingYieldTier"/>.</param>
    /// <param name="rank">The player's Gathering rank, for display and reports.</param>
    /// <param name="nodeTier">The node's tier, 1 to 5, for display and reports.</param>
    /// <param name="effects">The harvester's perks and style, or null for none.</param>
    public ProspectingSetup(
        ProspectingNode node,
        int charges,
        int surveyReveals = 1,
        int yieldTier = 0,
        int rank = 0,
        int nodeTier = 1,
        ProspectingEffects effects = null)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
        if (node.IsSpent)
            throw new ArgumentException("A spent node cannot be worked.", nameof(node));
        if (charges < 1)
            throw new ArgumentOutOfRangeException(nameof(charges), "A session needs at least one charge.");
        if (surveyReveals < 1 || surveyReveals > CraftRankMilestones.MaxProspectingSurveyReveals)
            throw new ArgumentOutOfRangeException(nameof(surveyReveals), "A Survey reveals at least one seam.");
        if (yieldTier < 0 || yieldTier > CraftRankMilestones.MaxProspectingYieldTier)
            throw new ArgumentOutOfRangeException(nameof(yieldTier), "The tier of yield is 0 to the most a rank can earn.");
        if (nodeTier < 1 || nodeTier > CraftRankMilestones.MaxTier)
            throw new ArgumentOutOfRangeException(nameof(nodeTier), "A node's tier is 1 to 5.");

        effects ??= ProspectingEffects.None;
        Charges = charges;
        SurveyReveals = surveyReveals;
        YieldTier = yieldTier;
        Rank = rank;
        NodeTier = nodeTier;
        Effects = effects;
        StrikeYield = ProspectingRules.StrikeYield(yieldTier, effects);
        HeavyStrikeYield = ProspectingRules.HeavyStrikeYield(yieldTier);
        SurveyStability = Math.Max(1, ProspectingRules.SurveyStability - effects.SurveyStabilityReduction);
        StrikeStability = Math.Max(1, ProspectingRules.StrikeStability - effects.StrikeStabilityReduction);
        HeavyStrikeStability = Math.Max(1, ProspectingRules.HeavyStrikeStability - effects.HeavyStrikeStabilityReduction);
        RichExtractCharges = Math.Max(1, ProspectingRules.ExtractCharges - effects.RichExtractChargeReduction);
        RichExtractStability = Math.Max(1, ProspectingRules.ExtractStability - effects.RichExtractStabilityReduction);
        ShatterPercent = Math.Max(0, ProspectingRules.ShatterPercent - effects.ShatterReduction);
    }

    /// <summary>The node as the session begins.</summary>
    public ProspectingNode Node { get; }

    /// <summary>The charges the session may spend.</summary>
    public int Charges { get; }

    /// <summary>The seams one Survey reveals.</summary>
    public int SurveyReveals { get; }

    /// <summary>The tier of yield the Gathering rank earned.</summary>
    public int YieldTier { get; }

    public int Rank { get; }

    public int NodeTier { get; }

    /// <summary>The harvester's perks and style.</summary>
    public ProspectingEffects Effects { get; }

    /// <summary>The Stability a Survey costs, perks included.</summary>
    public int SurveyStability { get; }

    /// <summary>The Stability a Strike costs, perks and style included.</summary>
    public int StrikeStability { get; }

    /// <summary>The Stability a Heavy Strike costs, perks included.</summary>
    public int HeavyStrikeStability { get; }

    /// <summary>The charges an Extract of a rich seam costs, perks included. A gem pocket costs <see cref="ProspectingRules.ExtractCharges"/>.</summary>
    public int RichExtractCharges { get; }

    /// <summary>The Stability an Extract of a rich seam costs, perks included. A gem pocket costs <see cref="ProspectingRules.ExtractStability"/>.</summary>
    public int RichExtractStability { get; }

    /// <summary>The chance, in percent, that a Heavy Strike shatters a gem pocket that has not been surveyed, perks included.</summary>
    public int ShatterPercent { get; }

    /// <summary>The chances of the units a Strike takes at this rank, before the pool caps them.</summary>
    public IReadOnlyList<(int Units, int Chance)> StrikeYield { get; }

    /// <summary>The chances of the units a Heavy Strike takes at this rank, before the pool caps them.</summary>
    public IReadOnlyList<(int Units, int Chance)> HeavyStrikeYield { get; }

    /// <summary>The level the node's units pay XP at: <see cref="GatheringLevels.ForTier"/> for the node's tier.</summary>
    public int VeinLevel => GatheringLevels.ForTier(NodeTier);

    /// <summary>
    /// The session of a Gathering rank at a node: the rank sets what a Survey reveals and how much a Strike yields, and the harvester's
    /// perks and style change the costs, the units and what a Survey reveals (never more than a node holds).
    /// </summary>
    public static ProspectingSetup ForNode(ProspectingNode node, int charges, int gatheringRank, int nodeTier, ProspectingEffects effects = null)
    {
        effects ??= ProspectingEffects.None;
        return new ProspectingSetup(
            node,
            charges,
            Math.Min(
                CraftRankMilestones.MaxProspectingSurveyReveals,
                CraftRankMilestones.ProspectingSurveyRevealsForRank(gatheringRank) + effects.SurveyReveals),
            CraftRankMilestones.ProspectingYieldTierForRank(gatheringRank),
            gatheringRank,
            nodeTier,
            effects);
    }
}
