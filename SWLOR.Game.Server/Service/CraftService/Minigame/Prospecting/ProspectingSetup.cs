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
        StrikeYield = ProspectingRules.StrikeYield(yieldTier);
        HeavyStrikeYield = ProspectingRules.HeavyStrikeYield(yieldTier);
        StrikeStability = Math.Max(1, ProspectingRules.StrikeStability - effects.StrikeStabilityReduction);
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

    /// <summary>The Stability a Strike costs, style included.</summary>
    public int StrikeStability { get; }

    /// <summary>
    /// The seconds an action takes to resolve, and so the least time between two actions, perks and style included. <paramref name="motherLode"/>
    /// is true for the first Strike or Heavy Strike of a node when the harvester has Mother Lode, which takes a share of its time, and
    /// <paramref name="seam"/> is the kind of seam an Extract would claim.
    /// </summary>
    public double SecondsFor(string actionId, bool motherLode = false, ProspectingSeamKind? seam = null)
    {
        var seconds = ProspectingRules.SecondsFor(actionId);
        switch (actionId)
        {
            case ProspectingActionIds.Survey:
                return Math.Max(0.5, seconds - Effects.SeamWorkTimeReductionTenths / 10.0);

            case ProspectingActionIds.Extract:
                var rich = seam == ProspectingSeamKind.Rich ? Effects.RichExtractTimeReductionTenths : 0;
                return Math.Max(0.5, seconds - (rich + Effects.SeamWorkTimeReductionTenths) / 10.0);

            case ProspectingActionIds.Strike:
                seconds = Math.Max(0.5, seconds - Effects.StrikeTimeReductionTenths / 10.0);
                return motherLode ? seconds * ProspectingPerkRules.MotherLodeTimePercent / 100.0 : seconds;

            case ProspectingActionIds.HeavyStrike:
                seconds = Math.Max(0.5, seconds - Effects.HeavyStrikeTimeReductionTenths / 10.0);
                return motherLode ? seconds * ProspectingPerkRules.MotherLodeTimePercent / 100.0 : seconds;

            default:
                return seconds;
        }
    }

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
