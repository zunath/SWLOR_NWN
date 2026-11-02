using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Service.ScavengeService
{
    /// <summary>
    /// A scavenge site as the scavenging code knows it once a player has opened it and the checks have passed: who searches what, the
    /// loot table and the character's rank, perks and place. The session that follows is built from it.
    /// </summary>
    /// <param name="Player">The scavenger.</param>
    /// <param name="Site">The scavenge site placeable.</param>
    /// <param name="SiteLevel">The site's level, from its <c>SCAVENGE_POINT_LEVEL</c>.</param>
    /// <param name="LootTableName">The site's loot table, for messages and reports.</param>
    /// <param name="Table">The site's loot table.</param>
    /// <param name="GatheringRank">The scavenger's Gathering rank when they opened the site.</param>
    /// <param name="DeltaXp">The XP a successful attempt pays before the board's multiplier: <c>Skill.GetDeltaXP(10 x site level - rank)</c>.</param>
    /// <param name="TreasureHunterLevel">The scavenger's Treasure Hunter level, which weights rare-flagged entries.</param>
    /// <param name="CreditFinderLevel">The scavenger's Creditfinder level, which raises gold finds.</param>
    /// <param name="Effects">The scavenger's perks for the board, or null for none.</param>
    public sealed record ScavengeRequest(
        uint Player,
        uint Site,
        int SiteLevel,
        string LootTableName,
        LootTable Table,
        int GatheringRank,
        int DeltaXp,
        int TreasureHunterLevel,
        int CreditFinderLevel,
        SearchEffects Effects = null);
}
