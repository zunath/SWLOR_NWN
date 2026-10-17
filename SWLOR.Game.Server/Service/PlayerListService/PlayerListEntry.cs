using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PlayerListService
{
    /// <summary>
    /// One row of the player list, already resolved for the player viewing it.
    /// </summary>
    public class PlayerListEntry
    {
        public uint Player { get; set; }
        // Empty when a non-DM viewer has not named this player.
        public string Name { get; set; }
        public string Descriptor { get; set; }
        public bool IsDisguised { get; set; }
        public bool IsAfk { get; set; }
        public IReadOnlyCollection<PlayerListFlagType> Flags { get; set; }
        public PlayerListRelation Relation { get; set; }
        public PlayerListStatus Status { get; set; }
    }
}
