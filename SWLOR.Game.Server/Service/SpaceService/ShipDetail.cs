using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public class ShipDetail
    {
        public string Name { get; set; }
        public AppearanceType Appearance { get; set; }

        public int MaxShield { get; set; }
        public int MaxHull { get; set; }
        public int MaxCapacitor { get; set; }
        public float ShieldRechargeRate { get; set; }

        public int HighPowerNodes { get; set; }
        public int LowPowerNodes { get; set; }

        public bool HasDroidBay { get; set; }

        public ShipDetail()
        {
            Name = string.Empty;
        }
    }
}
