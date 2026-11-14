using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A Hull Design: another look for one ship model, unlocked by its Design Schematic and applied by a starport Refit
    /// (plan section 7). Rows come from the generated RevampRewardCatalog.
    /// </summary>
    public class ShipHullDesign
    {
        /// <summary>The deed tag of the ship the design belongs to.</summary>
        public string ShipTag { get; set; }
        public string ShipName { get; set; }
        public AppearanceType Appearance { get; set; }

        /// <summary>The design's own name, for example "Pattern A".</summary>
        public string Name { get; set; }

        /// <summary>The Design Schematic item that unlocks it.</summary>
        public string Resref { get; set; }

        /// <summary>The ship's tier, from its Piloting gate (1-5).</summary>
        public int Tier { get; set; }

        /// <summary>The schematic only drops in Wild Space (Rift Raider ships).</summary>
        public bool WildSpaceOnly { get; set; }
    }
}
