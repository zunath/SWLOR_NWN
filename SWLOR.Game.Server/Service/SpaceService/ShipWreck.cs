using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>A destroyed NPC ship's wreck that a Salvager can work (plan section 12, "Salvage").</summary>
    public class ShipWreck
    {
        /// <summary>The faction tier of the ship (1-6).</summary>
        public int Tier { get; init; }

        public WreckDifficulty Difficulty { get; init; }

        /// <summary>The destroyed ship's hull size. A Capital wreck needs a Large or Capital salvager.</summary>
        public ShipSize Size { get; init; }

        public DateTime DestroyedAt { get; init; }

        /// <summary>The players of the killing party, who have the wreck to themselves for the claim time.</summary>
        public HashSet<string> Claimants { get; init; } = new();

        /// <summary>
        /// Whether the wreck still carries a data core to slice (elite, ace and boss wrecks; plan section 12). The
        /// claimants' first claim covers it as it covers salvage.
        /// </summary>
        public bool HasDataCore { get; set; }

        /// <summary>
        /// Whether the wreck lies in Wild Space: its rare finds draw on the Wild Space tables and schematics, and its
        /// salvaged components roll rare traits more often (plan section 23).
        /// </summary>
        public bool WildSpace { get; init; }

        /// <summary>Salvage cycles left before the wreck is stripped.</summary>
        public int RemainingCycles { get; set; }

        /// <summary>The maker whose manufacturer component the wreck's salvage can yield; Invalid for none.</summary>
        public ShipManufacturer Manufacturer { get; set; }
    }
}
