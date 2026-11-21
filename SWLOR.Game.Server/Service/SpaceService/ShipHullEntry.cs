using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// One of the 50 player hulls of the space revamp: slots, base stats and percent changes.
    /// Rows come from the generated RevampShipCatalog.
    /// </summary>
    public class ShipHullEntry
    {
        public string Tag { get; set; }
        public string Name { get; set; }
        public ShipSize Size { get; set; }
        public ShipRole Role { get; set; }
        public ShipManufacturer Manufacturer { get; set; }
        public bool Advanced { get; set; }
        public int PilotingRank { get; set; }
        public AppearanceType Appearance { get; set; }
        public PropertyLayoutType Layout { get; set; }

        public int High { get; set; }
        public int Mid { get; set; }
        public int Low { get; set; }

        /// <summary>How many slots at the end of each row are crew-station slots.</summary>
        public int StationHigh { get; set; }
        public int StationMid { get; set; }
        public int StationLow { get; set; }

        public int Hull { get; set; }
        public int Shield { get; set; }
        public int Capacitor { get; set; }
        public double CapacitorRegen { get; set; }
        public double Speed { get; set; } = 1.0;
        public int Attack { get; set; }
        public int Accuracy { get; set; }
        public int Defense { get; set; }
        public int Evasion { get; set; }
        public int Cargo { get; set; }
        public int Interior { get; set; }
        public double RepairMultiplier { get; set; } = 1.0;
        public int ResistAll { get; set; }

        public int HullPercent { get; set; }
        public int ShieldPercent { get; set; }
        public int CapacitorPercent { get; set; }
        public int ShieldRegenPercent { get; set; }
        public int DamagePercent { get; set; }
        public int ExplosivePercent { get; set; }
        public int CriticalBonus { get; set; }

        public string Signature { get; set; }
        public bool Store { get; set; }
        public RecipeType RecipeType { get; set; }
        public int RecipeLevel { get; set; }
        public (string Resref, int Quantity)[] Components { get; set; } = new (string Resref, int Quantity)[0];
    }
}
