namespace SWLOR.Game.Server.Service.ProspectingService
{
    /// <summary>
    /// A player using a harvester on a vein or a tree: who, with which harvester, on which node, where they stand and what the node and
    /// their Gathering rank are. The session that follows is built from it.
    /// </summary>
    /// <param name="Player">The harvester.</param>
    /// <param name="Harvester">The harvester item. Its charges are the session's budget.</param>
    /// <param name="Node">The vein or tree.</param>
    /// <param name="LootTable">The name of the node's harvesting loot table.</param>
    /// <param name="NodeTier">The node's tier, 1 to 5: its <c>HARVESTER_REQUIRED_LEVEL</c>.</param>
    /// <param name="Rank">The player's Gathering rank when they began.</param>
    /// <param name="Position">Where the player stood, which they must not leave during the session.</param>
    public sealed record ProspectingRequest(
        uint Player,
        uint Harvester,
        uint Node,
        string LootTable,
        int NodeTier,
        int Rank,
        System.Numerics.Vector3 Position);
}
