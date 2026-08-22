#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// Tier-specific content for one difficulty tier of a dungeon theme: ambient spawn pool,
    /// per-room spawn counts, boss, and treasure. Consumed by DungeonContentPlacer.Populate.
    /// </summary>
    public class DungeonTierDetail
    {
        public int Tier { get; set; }
        public List<DungeonCreatureEntry> Creatures { get; set; } = new();
        public int MinCreaturesPerRoom { get; set; } = 1;
        public int MaxCreaturesPerRoom { get; set; } = 2;
        public string BossResref { get; set; } = string.Empty;
        public string TreasureLootTableId { get; set; } = string.Empty;
        public int TreasureItemCount { get; set; } = 1;
        /// <summary>Free-text balance note for programmer/DM reference. Not used by the system.</summary>
        public string LevelNote { get; set; } = string.Empty;
    }
}
