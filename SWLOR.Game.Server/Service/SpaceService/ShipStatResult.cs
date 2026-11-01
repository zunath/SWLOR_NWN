using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The stats of a revamp ship worked out from its hull, fitted modules and pilot.
    /// </summary>
    public class ShipStatResult
    {
        public ShipSize Size { get; set; }
        public bool CapitalShip { get; set; }
        public int MaxHull { get; set; }
        public int MaxShield { get; set; }
        public int MaxCapacitor { get; set; }

        /// <summary>Capacitor regen per second before the pilot's WIL is added.</summary>
        public double CapacitorRegenPerSecond { get; set; }

        public int Attack { get; set; }
        public int Accuracy { get; set; }
        public int Defense { get; set; }
        public int Evasion { get; set; }
        public int ShieldRegenBonusPercent { get; set; }
        public int CriticalBonus { get; set; }

        /// <summary>The movement multiplier (1.0 is today's speed).</summary>
        public double Speed { get; set; } = 1.0;

        public Dictionary<ResistanceType, int> Resistances { get; set; } = new();
        public Dictionary<CombatDamageType, int> DamageBonusPercent { get; set; } = new();

        /// <summary>The crew tag of every slot by hotbar feat slot number (1-30).</summary>
        public Dictionary<int, ShipSlotTag> SlotTags { get; set; } = new();
    }
}
