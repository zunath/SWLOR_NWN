using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public class ShipStatus
    {
        public class ShipStatusModule
        {
            public string ItemInstanceId { get; set; }
            public string ItemTag { get; set; }
            public string SerializedItem { get; set; }
            public DateTime RecastTime { get; set; }
        }
        
        public string ItemTag { get; set; }
        public string Name { get; set; }
        public int Shield { get; set; }
        public int Hull { get; set; }
        public int Capacitor { get; set; }
        public int MaxShield{ get; set; }
        public int MaxHull { get; set; }
        public int MaxCapacitor { get; set; }
        public int ShieldCycle { get; set; }
        public int ShieldRechargeRate { get; set; }
        public int EMDamage { get; set; }
        public int ThermalDamage { get; set; }
        public int ExplosiveDamage { get; set; }
        public int Accuracy { get; set; }
        public int Evasion { get; set; }
        public int ThermalDefense { get; set; }
        public int ExplosiveDefense { get; set; }
        public int EMDefense { get; set; }
        public string SerializedHotBar { get; set; }

        public Dictionary<Feat, ShipStatusModule> HighPowerModules { get; set; }
        public Dictionary<Feat, ShipStatusModule> LowPowerModules { get; set; }

        public ShipStatus()
        {
            HighPowerModules = new Dictionary<Feat, ShipStatusModule>();
            LowPowerModules = new Dictionary<Feat, ShipStatusModule>();
        }
    }
}
