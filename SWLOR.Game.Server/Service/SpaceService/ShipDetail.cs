using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public class ShipDetail
    {
        public string Name { get; set; }
        public AppearanceType Appearance { get; set; }
        public PropertyLayoutType Layout { get; set; }
        public string ItemResref { get; set; }
        public int MaxShield { get; set; }
        public int MaxHull { get; set; }
        public int MaxCapacitor { get; set; }
        public int ShieldRechargeRate { get; set; }
        public int HighPowerNodes { get; set; }
        public int LowPowerNodes { get; set; }
        public int ConfigurationNodes { get; set; }
        public int Accuracy { get; set; }
        public int Evasion { get; set; }
        public int ExplosiveDefense { get; set; }
        public int ThermalDefense { get; set; }
        public int EMDefense { get; set; }
        public bool HasDroidBay { get; set; }

        public Dictionary<PerkType, int> RequiredPerks { get; set; }

        public ShipDetail()
        {
            Name = string.Empty;
            RequiredPerks = new Dictionary<PerkType, int>();
        }
    }
}
