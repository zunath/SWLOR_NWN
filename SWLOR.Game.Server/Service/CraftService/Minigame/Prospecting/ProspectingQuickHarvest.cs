using System;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

/// <summary>
/// Quick Harvest, the way to work a node without a session: it resolves the node with no seams, as today's harvests did. It takes the
/// units the node has left, one harvest at a time, each spending one charge and taking one harvest delay with the harvest animation, and
/// pays the XP of a unit at the player's rank for each. It reads no attribute and no rank milestone (the parity reference is a character
/// with no Might), so it yields exactly what the harvester gave before Prospecting existed, and it takes as long, so it never trains
/// Gathering faster than before.
/// </summary>
public static class ProspectingQuickHarvest
{
    /// <summary>The units a Quick Harvest takes: everything the node has left, as far as the harvester's charges go.</summary>
    public static int Units(ProspectingNode node, int charges)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        return Math.Min(node.Pool, Math.Max(0, charges));
    }

    /// <summary>The charges a Quick Harvest of the units spends: one for each, as each of today's harvests did.</summary>
    public static int Charges(int units) => Math.Max(0, units);

    /// <summary>The seconds a Quick Harvest of the units takes: one harvest delay for each.</summary>
    public static double Seconds(int units) => Math.Max(0, units) * ProspectingRules.HarvestSeconds;

    /// <summary>The XP one unit pays a Gathering rank at a node of the tier: <c>GetDeltaXP(vein level - rank)</c>, as before.</summary>
    public static int XpPerUnit(int nodeTier, int gatheringRank) => GatheringLevels.XpPerUnit(nodeTier, gatheringRank);

    /// <summary>The XP a Quick Harvest of the units pays.</summary>
    public static int Xp(int units, int nodeTier, int gatheringRank) => Math.Max(0, units) * XpPerUnit(nodeTier, gatheringRank);

    /// <summary>
    /// The units a Quick Harvest takes from a fresh node, on average, with plenty of charges: the mean of today's d4 harvests per node.
    /// </summary>
    public static double ExpectedUnits => (ProspectingRules.PoolDie + 1) / 2.0;
}
