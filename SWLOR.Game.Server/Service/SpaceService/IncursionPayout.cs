using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// What a cleared incursion pays one player. Kept in the incursion record until a player who was offline at the clear
    /// logs in.
    /// </summary>
    public class IncursionPayout
    {
        public int Credits { get; set; }

        /// <summary>Loot table name to the number of pulls from it, rolled when the payout is delivered.</summary>
        public Dictionary<string, int> Pulls { get; set; } = new();

        /// <summary>Items given as they are (a Ship Schematic).</summary>
        public List<string> Items { get; set; } = new();

        public double Points { get; set; }
        public string RegionName { get; set; } = string.Empty;
    }
}
