using System;
using System.Collections.Generic;

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
            public int ModuleBonus { get; set; }
        }
        
        public string ItemTag { get; set; }
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
        public DateTime GlobalRecast { get; set; }

        /// <summary>
        /// Equipped high-powered modules
        /// </summary>
        public Dictionary<int, ShipStatusModule> HighPowerModules { get; set; }

        /// <summary>
        /// Equipped low-powered modules
        /// </summary>
        public Dictionary<int, ShipStatusModule> LowPowerModules { get; set; }

        /// <summary>
        /// A collection of ship modules, by feat, which can be activated.
        /// This is primarily used by ship AI but is also available for player ships.
        /// </summary>
        public HashSet<int> ActiveModules { get; set; }

        public ShipStatus()
        {
            HighPowerModules = new Dictionary<int, ShipStatusModule>();
            LowPowerModules = new Dictionary<int, ShipStatusModule>();
            ActiveModules = new HashSet<int>();
        }
    }
}
