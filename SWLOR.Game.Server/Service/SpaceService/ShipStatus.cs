using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;

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
            public DateTime RecastStartedAt { get; set; }
            public int ModuleBonus { get; set; }

            /// <summary>
            /// RequiresSkill ranks on the module item when it was installed. Null when it had none.
            /// </summary>
            public Dictionary<SkillType, int> RequiredSkills { get; set; }
        }

        /// <summary>
        /// A toggled module that is on.
        /// </summary>
        public class ShipActiveModule
        {
            /// <summary>The hotbar feat slot number (1-30): high 1-10, low 11-20, mid 21-30.</summary>
            public int Slot { get; set; }

            /// <summary>When the module's current cycle ends and it fires again.</summary>
            public DateTime NextCycleTime { get; set; }

            /// <summary>The object a support module locked onto when it was turned on.</summary>
            public uint TargetId { get; set; } = OBJECT_INVALID;
        }

        public string ItemTag { get; set; }

        /// <summary>
        /// RequiresSkill ranks on the ship's deed item when it was registered. Null when it had none.
        /// </summary>
        public Dictionary<SkillType, int> RequiredSkills { get; set; }
        public int Shield { get; set; }
        public int Hull { get; set; }
        public int Capacitor { get; set; }
        public int MaxShield{ get; set; }
        public int MaxHull { get; set; }
        public int MaxCapacitor { get; set; }
        public int ShieldCycle { get; set; }

        /// <summary>
        /// Superseded by the WIL-based shield regen (SpaceCombatMath.ShieldRegenPerSecond). Kept because saved data
        /// carries it; nothing reads it.
        /// </summary>
        public int ShieldRechargeRate { get; set; }

        // The six per-type damage and defense fields are superseded by Attack and Defense. Saved data still carries
        // them (a later migration removes them); combat no longer reads them.
        public int EMDamage { get; set; }
        public int ThermalDamage { get; set; }
        public int ExplosiveDamage { get; set; }
        public int Accuracy { get; set; }
        public int Evasion { get; set; }
        public int ThermalDefense { get; set; }
        public int ExplosiveDefense { get; set; }
        public int EMDefense { get; set; }

        /// <summary>The ship's Attack, added to every weapon module's own Attack.</summary>
        public int Attack { get; set; }

        /// <summary>The ship's Defense, added to the pilot's VIT.</summary>
        public int Defense { get; set; }

        /// <summary>The hull size. Saved data without it reads as Small.</summary>
        public ShipSize Size { get; set; } = ShipSize.Small;

        /// <summary>Percent bonus to the ship's damage by type (Thermal, EM, Explosive).</summary>
        public Dictionary<CombatDamageType, int> DamageBonusPercent { get; set; } = new();

        /// <summary>Percent bonus to shield regen.</summary>
        public int ShieldRegenBonusPercent { get; set; }

        /// <summary>The hull's capacitor regen per second, before the pilot's WIL is added.</summary>
        public double CapacitorRegenPerSecond { get; set; } = 1.0;

        /// <summary>
        /// Fractional shield and capacitor regen carried between ticks. Persisted, because a player's status is
        /// reloaded from the database every tick.
        /// </summary>
        public double ShieldRegenRemainder { get; set; }
        public double CapacitorRegenRemainder { get; set; }

        public int Industrial { get; set; }
        public bool CapitalShip { get; set; }
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
        /// Equipped mid-powered modules
        /// </summary>
        public Dictionary<int, ShipStatusModule> MidPowerModules { get; set; }

        /// <summary>
        /// Equipped configuration module
        /// </summary>
        public Dictionary<int, ShipStatusModule> ConfigurationModules { get; set; }

        /// <summary>
        /// The toggled modules that are currently on, for players and ships alike (plan section 8).
        /// Saved data that has an empty list still loads.
        /// </summary>
        public List<ShipActiveModule> ActiveModules { get; set; }

        /// <summary>
        /// Resistance percentages by damage type.
        /// </summary>
        public Dictionary<ResistanceType, int> Resistances { get; set; }

        /// <summary>
        /// Crew station tags keyed by hotbar feat slot number (1-30). Used for crew stations (plan section 10).
        /// </summary>
        public Dictionary<int, ShipSlotTag> SlotTags { get; set; }

        public ShipStatus()
        {
            HighPowerModules = new Dictionary<int, ShipStatusModule>();
            LowPowerModules = new Dictionary<int, ShipStatusModule>();
            MidPowerModules = new Dictionary<int, ShipStatusModule>();
            ConfigurationModules = new Dictionary<int, ShipStatusModule>();
            ActiveModules = new List<ShipActiveModule>();
            Resistances = new Dictionary<ResistanceType, int>();
            SlotTags = new Dictionary<int, ShipSlotTag>();
        }
    }
}
